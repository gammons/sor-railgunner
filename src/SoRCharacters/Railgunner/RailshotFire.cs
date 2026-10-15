using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using SoRCharacters.Core;
using SoRCharacters.Core.Railgunner;
using UnityEngine;

namespace SoRCharacters.Railgunner
{
    internal sealed class ShotResult
    {
        public int NpcKills { get; set; }
        public int PlayerKills { get; set; }
        public int Objects { get; set; }
        public int WallsCut { get; set; }
        public int SteelPassed { get; set; }
        public float StopDistance { get; set; }
        public List<Vector2> KillPositions { get; } = new List<Vector2>();
        public List<Vector2> WallCutPositions { get; } = new List<Vector2>();
    }

    internal static class RailshotFire
    {
        private const float TileUnits = 0.64f;
        private const float DefaultAgentRadius = 0.24f;

        private static readonly HashSet<string> LoggedFailures = new HashSet<string>();

        public static ShotResult Fire(Agent shooter, Vector2 dir)
        {
            var result = new ShotResult();
            var gc = GameController.gameController;
            if (gc == null || !gc.serverPlayer || gc.multiplayerMode) return result;

            Door.freerAgent = shooter;
            try
            {
                var origin = (Vector2)shooter.tr.position;
                var breaching = GameCompat.HasBreachingSlug(shooter);
                var plan = Plan(origin, dir, breaching);
                result.StopDistance = plan.StopDistance;
                result.SteelPassed = plan.SteelPassed.Count;
                var end = origin + dir * plan.StopDistance;
                var halfWidth = HalfWidth();

                KillAgents(gc, shooter, origin, dir, plan.StopDistance, halfWidth, result);
                DestroyObjects(gc, shooter, origin, dir, plan.StopDistance, halfWidth, result);
                CutWalls(gc, shooter, plan.TilesToCut, breaching, result);
                if (result.WallsCut > 0) Guard("path update", () => RailshotEffects.QueuePathUpdate(origin, end));
                RailshotEffects.Play(shooter, origin, end, dir, result, RailgunnerModule.Settings);
            }
            catch (Exception e)
            {
                LogOnce("shot", e);
            }

            Plugin.Log.LogInfo($"[Railgunner] shot: {result.NpcKills} npcs, {result.PlayerKills} players, {result.Objects} objects, {result.WallsCut} walls, {result.SteelPassed} steel passed, length {result.StopDistance:0.00}");
            return result;
        }

        public static BeamPlan Plan(Vector2 origin, Vector2 dir, bool breaching)
        {
            var settings = RailgunnerModule.Settings;
            return BeamGeometry.Plan(new Vec2(origin.x, origin.y), new Vec2(dir.x, dir.y),
                Tiles.ToUnits(settings.BeamRangeTiles), HalfWidth(), t => Classify(t, breaching));
        }

        // Breaching Slug turns steel into a breakable wall; lockdown barriers and the border keep their kind.
        public static TileKind Classify(TileCoord t, bool breaching)
        {
            var gc = GameController.gameController;
            TileData data;
            if (gc.streamingWorld)
            {
                data = gc.tileInfo.GetTileData(new Vector3(t.X * TileUnits, t.Y * TileUnits, 0f));
            }
            else
            {
                var size = gc.loadLevel.levelSizePixels;
                var cx = t.X * TileUnits;
                var cy = t.Y * TileUnits;
                if (t.X < 0 || t.Y < 0 || t.X > 159 || t.Y > 159 || cx >= size || cy >= size) return TileKind.Border;
                data = gc.tileInfo.GetTileDataInt(t.X, t.Y);
            }
            if (data == null) return TileKind.Empty;

            switch (data.wallMaterial)
            {
                case wallMaterialType.Border:
                    return TileKind.Border;
                case wallMaterialType.Steel:
                    return breaching && !data.destroyedWall ? TileKind.Breakable : TileKind.Steel;
                case wallMaterialType.LockdownWall:
                    return TileKind.Steel;
                case wallMaterialType.Normal:
                case wallMaterialType.Wood:
                case wallMaterialType.Glass:
                    return data.destroyedWall ? TileKind.Empty : TileKind.Breakable;
                default:
                    return TileKind.Empty;
            }
        }

        private static float HalfWidth()
        {
            return Tiles.ToUnits(RailgunnerModule.Settings.BeamWidthTiles) / 2f;
        }

        private static void KillAgents(GameController gc, Agent shooter, Vector2 origin, Vector2 dir, float length, float halfWidth, ShotResult result)
        {
            var o = new Vec2(origin.x, origin.y);
            var d = new Vec2(dir.x, dir.y);
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            foreach (var victim in gc.agentList.ToList())
            {
                Guard("agent", () =>
                {
                    if (victim == null || victim == shooter || Skip(victim)) return;
                    var pos = victim.tr.position;
                    if ((pos.x - origin.x) * dir.x + (pos.y - origin.y) * dir.y < 0f) return;
                    var radius = victim.agentCollider != null ? victim.agentCollider.radius : DefaultAgentRadius;
                    if (!BeamGeometry.CircleHit(o, d, length, halfWidth, new Vec2(pos.x, pos.y), radius)) return;

                    var isPlayer = victim.isPlayer > 0;
                    var wasMech = victim.mechFilled;
                    var wasKnockedOut = victim.dead;
                    RailKill(victim, shooter);
                    if (wasMech && !victim.dead && !victim.mechFilled)
                    {
                        Plugin.Log.LogInfo("[Railgunner] mech destroyed; killing the ejected pilot");
                        RailKill(victim, shooter);
                    }

                    // A knocked-out agent is already dead; the -200 hit takes the "Death Type 2" route
                    // (StatusEffects.cs:1479-1505), which clears knockedOut, so that is the success signal.
                    var killed = wasKnockedOut ? !victim.KnockedOut() : victim.dead;
                    if (!killed) return;
                    if (isPlayer) result.PlayerKills++;
                    else result.NpcKills++;
                    result.KillPositions.Add(pos);
                    gc.spawnerMain.SpawnParticleEffect("BulletHitLaser", pos, angle);
                });
            }
        }

        private static bool Skip(Agent a)
        {
            return (a.dead && !a.KnockedOut()) || a.ghost || a.disappeared || a.hologram || a.butlerBot || a.mechEmpty || a.objectAgent
                || a.isDummy || a.isBackup;
        }

        // Research Q5. For a player victim justHitByAgent2 stays null: with it set, ChangeHealth zeroes
        // player-vs-player damage when PvP is off (StatusEffects.cs:324-328), so a co-op partner would survive.
        private static void RailKill(Agent victim, Agent killer)
        {
            var se = victim.statusEffects;
            victim.lastHitByAgent = killer;
            victim.justHitByAgent2 = victim.isPlayer > 0 ? null : killer;
            victim.deathMethod = "Railshot";
            victim.deathKiller = killer.agentName;
            victim.bloodyMessed = true;
            var hp = victim.health;
            var wasMech = victim.mechFilled;
            if (victim.health > 1f) victim.health = 1f;
            se.ignoreInvincible = true;
            try
            {
                se.ChangeHealth(-200f);
            }
            finally
            {
                se.ignoreInvincible = false;
            }
            if (victim.dead && victim.inhuman && !victim.gibbed) se.NormalGib();

            // ChangeHealth can refuse the hit (StatusEffects.cs:259) or zero it (:345-360); don't leave the victim at 1 HP.
            if (!victim.dead && !victim.KnockedOut() && !(wasMech && !victim.mechFilled))
            {
                victim.health = hp;
                Plugin.Log.LogInfo($"[Railgunner] {victim.agentRealName} survived the railshot (hit refused)");
            }
        }

        private static void DestroyObjects(GameController gc, Agent shooter, Vector2 origin, Vector2 dir, float length, float halfWidth, ShotResult result)
        {
            var o = new Vec2(origin.x, origin.y);
            var d = new Vec2(dir.x, dir.y);

            foreach (var obj in gc.objectRealList.ToList())
            {
                Guard("object", () =>
                {
                    if (obj == null || obj.notRealObject || obj.cantBeDestroyed || obj.destroying || obj.destroyed || obj.OnFloor) return;
                    if (obj.objectAgent != null || obj.hasObjectAgent) return;
                    if (!TryBounds(obj, out var bounds)) return;
                    if (!BeamGeometry.BoxHit(o, d, length, halfWidth, new Vec2(bounds.min.x, bounds.min.y), new Vec2(bounds.max.x, bounds.max.y))) return;

                    if (!obj.noOwnCheck) gc.OwnCheck(shooter, obj.gameObject, "Normal", 0);
                    obj.lastHitByAgent = shooter;
                    obj.DestroyMe(shooter);
                    result.Objects++;
                });
            }
        }

        private static bool TryBounds(ObjectReal obj, out Bounds bounds)
        {
            Collider2D? collider = obj.objectHitbox;
            if (collider == null || !collider.enabled) collider = obj.objectCollider;
            if (collider == null || !collider.enabled)
            {
                bounds = default;
                return false;
            }
            bounds = collider.bounds;
            return bounds.size.x > 0f || bounds.size.y > 0f;
        }

        private static void CutWalls(GameController gc, Agent shooter, IReadOnlyList<TileCoord> tiles, bool breaching, ShotResult result)
        {
            foreach (var tile in tiles)
            {
                Guard("wall", () =>
                {
                    if (Classify(tile, breaching) != TileKind.Breakable) return;
                    var x = tile.X * TileUnits;
                    var y = tile.Y * TileUnits;
                    var data = gc.streamingWorld
                        ? gc.tileInfo.GetTileData(new Vector3(x, y, 0f))
                        : gc.tileInfo.GetTileDataInt(tile.X, tile.Y);

                    GameObject? wall = data.wallCollider != null ? data.wallCollider.gameObject : null;
                    if (wall == null) wall = gc.tileInfo.IsOverlapping(new Vector2(x, y + 0.12f), "Wall");

                    if (wall != null) gc.OwnCheck(shooter, wall, "Normal", 0);
                    gc.tileInfo.DestroyWallTileAtPosition(x, y, true, shooter);
                    result.WallsCut++;
                    result.WallCutPositions.Add(new Vector2(x, y));

                    if (wall != null)
                    {
                        wall.layer = 1;
                        var graphUpdate = wall.GetComponent<GraphUpdateScene>();
                        if (graphUpdate != null) graphUpdate.Apply();
                    }
                    gc.stats.AddDestructionQuestPoints();
                    if (shooter.isPlayer > 0 && shooter.localPlayer) gc.stats.AddToStat(shooter, "Destruction", 1);
                });
            }
        }

        public static void Guard(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                LogOnce(what, e);
            }
        }

        private static void LogOnce(string what, Exception e)
        {
            if (LoggedFailures.Add(what + "|" + e.GetType().FullName + "|" + e.Message))
            {
                Plugin.Log.LogError($"[Railgunner] {what} failed: {e}");
            }
        }
    }
}
