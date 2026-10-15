using System.Collections.Generic;
using RogueLibsCore;
using UnityEngine;

namespace SoRCharacters.Railgunner
{
    // While charging or armed he holds a railgun on the game's own held-gun object (Gun.gunSprite, an Items-collection
    // tk2dSprite: Gun.cs:155, its material is SpawnerMain.itemsMaterial, Gun.cs:184/197, and ShowGun picks the sprite by
    // item name, Gun.cs:1591). Purely visual: inventory, ammo and attacks are untouched.
    internal static class HeldRailgun
    {
        public const string SpriteName = "RailgunHeld";
        private const string FallbackSpriteName = "LaserGun";
        private const float KeepVisibleSeconds = 4f;

        private sealed class Held
        {
            public Agent Agent = null!;
            public Gun Gun = null!;
            public int SpriteId;
            public bool Custom;
            public int SavedSpriteId;
            public Material? SavedMaterial;
            public bool HiddenByGame;
        }

        private static readonly Dictionary<int, Held> Holding = new Dictionary<int, Held>();
        private static bool _spriteRegistered;
        private static bool _loggedCollection;
        private static bool _loggedNoSprite;
        private static bool _loggedAimFailed;

        public static bool Any => Holding.Count != 0;

        public static void Register()
        {
            try
            {
                RogueLibs.CreateCustomSprite(SpriteName, SpriteScope.Items, Plugin.LoadResource("railgun_held.png"));
                _spriteRegistered = true;
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("[Railgunner] could not register the held railgun sprite, the Laser Gun sprite is used instead: " + e.Message);
            }
        }

        // Mirrors the normal-gun branch of Gun.ShowGun (Gun.cs:1570-1584): gun and both gun arms shown, his body arms
        // hidden, so he holds it in both hands. The sprite is then swapped for the railgun.
        public static void Show(Agent agent)
        {
            if (Holding.ContainsKey(agent.agentID)) return;

            RailshotFire.Guard("held railgun show", () =>
            {
                var gun = agent.gun;
                if (gun == null || gun.gunSprite == null || gun.meshRenderer == null) return;

                var collection = gun.gunSprite.Collection;
                if (!_loggedCollection)
                {
                    _loggedCollection = true;
                    Plugin.Log.LogInfo($"[Railgunner] held gun sprite collection: {(collection != null ? collection.name : "none")}");
                }
                if (collection == null) return;

                var custom = false;
                var id = -1;
                if (_spriteRegistered)
                {
                    id = collection.GetSpriteIdByName(SpriteName, -1);
                    custom = id >= 0;
                }
                if (id < 0) id = collection.GetSpriteIdByName(FallbackSpriteName, -1);
                if (id < 0)
                {
                    if (!_loggedNoSprite)
                    {
                        _loggedNoSprite = true;
                        Plugin.Log.LogWarning("[Railgunner] no held railgun sprite in the gun's sprite collection; he charges without a held gun");
                    }
                    return;
                }

                var held = new Held
                {
                    Agent = agent,
                    Gun = gun,
                    SpriteId = id,
                    Custom = custom,
                    SavedSpriteId = gun.gunSprite.spriteId,
                    SavedMaterial = gun.gunSpriteRenderer != null ? gun.gunSpriteRenderer.sharedMaterial : null,
                };

                // Registered before any renderer is touched, so a failure below cannot leave the gun shown with no entry.
                Holding[agent.agentID] = held;
                KeepVisible(held);

                try
                {
                    if (!gun.meshRenderer.enabled)
                    {
                        gun.meshRenderer.enabled = true;
                        gun.gunAnim.enabled = true;
                        gun.gunContainerAnim.enabled = true;
                    }
                    gun.meshRendererArm1.enabled = true;
                    gun.meshRendererArm2.enabled = true;
                    gun.usingSpecialGun = false;

                    var hitbox = agent.agentHitboxScript;
                    Color32 arm1 = hitbox.arm1.color;
                    hitbox.arm1.color = new Color32(arm1.r, arm1.g, arm1.b, 0);
                    Color32 arm2 = hitbox.arm2.color;
                    hitbox.arm2.color = new Color32(arm2.r, arm2.g, arm2.b, 0);

                    ApplySprite(held);
                }
                catch
                {
                    Holding.Remove(agent.agentID);
                    Restore(held);
                    throw;
                }

                Plugin.Log.LogInfo($"[Railgunner] holding the {(custom ? "railgun" : "Laser Gun")} sprite");
            });
        }

        // Called from the Gun.GunUpdate prefix, before its visibleTime countdown (Gun.cs:1971-1977).
        public static void KeepAlive(Gun gun)
        {
            if (!TryGet(gun, out var held)) return;
            if (held.HiddenByGame) return;

            // The game hides the gun itself on death, knock-out, interactions and operating (Agent.cs:8519,
            // AgentHitbox.cs:2296, InteractionHelper.cs:1135, PlayfieldObject.cs:5030, ...); that is left alone.
            if (!gun.meshRenderer.enabled)
            {
                held.HiddenByGame = true;
                Plugin.Log.LogInfo("[Railgunner] held railgun hidden by the game");
                return;
            }
            KeepVisible(held);
        }

        // Replaces Gun.AimGun (Gun.cs:706-826) while he holds the railgun: the gun points along the charge's aim (the
        // turn-limited aim once armed), his body faces it like vanilla's RotateToAngle (Gun.cs:740), the sprite flips
        // when aiming left (Gun.cs:813-825). Returns false to let the game aim as usual.
        public static bool Aim(Gun gun)
        {
            if (!TryGet(gun, out var held) || held.HiddenByGame) return false;

            try
            {
                var agent = held.Agent;
                var dir = RailshotState.FireDirection(agent);
                var angle = Mathf.Repeat(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 360f);

                if (agent.isPlayer > 0 && agent.localPlayer && !agent.outOfControl)
                {
                    agent.agentSpriteContainer.rotation = Quaternion.identity;
                    agent.agentSpriteTransform.rotation = Quaternion.identity;
                    agent.movement.RotateToAngle(angle);
                }

                if (!agent.disappeared) gun.gunAnim.SetInteger("Dir", gun.GetAimAngleString(angle));
                gun.gunContainerTr.rotation = Quaternion.Euler(0f, 0f, angle);

                var scale = angle > 91f && angle < 271f ? new Vector3(1f, -1f, 1f) : Vector3.one;
                var body = agent.agentSpriteTransform.localScale.x;
                if (body > 0.65f && body < 0.67f)
                {
                    scale = new Vector3(scale.x * agent.melee.diminutiveScale.x, scale.y * agent.melee.diminutiveScale.y, 1f);
                }
                gun.gunContainerTr.localScale = scale;

                // Melee.MeleeLateUpdate (run just before, Updater.cs:513-514) re-shows a melee or thrown weapon in his
                // hand every frame (Melee.cs:1331, 1371); it would float beside the railgun, so it is hidden this frame.
                var hitbox = agent.agentHitboxScript;
                if (agent.inventory.equippedWeapon != agent.inventory.fist)
                {
                    hitbox.heldItemRenderer.enabled = false;
                    hitbox.heldItem2Renderer.enabled = false;
                    if (hitbox.heldItemWBRenderer != null) hitbox.heldItemWBRenderer.enabled = false;
                }

                ApplySprite(held);
                return true;
            }
            catch (System.Exception e)
            {
                if (!_loggedAimFailed)
                {
                    _loggedAimFailed = true;
                    Plugin.Log.LogError($"[Railgunner] held railgun aim failed: {e}");
                }
                return false;
            }
        }

        public static void Release(int agentId)
        {
            if (!Holding.TryGetValue(agentId, out var held)) return;
            Holding.Remove(agentId);
            Restore(held);
        }

        public static void ReleaseAll()
        {
            if (Holding.Count == 0) return;
            var all = new List<Held>(Holding.Values);
            Holding.Clear();
            foreach (var held in all) Restore(held);
        }

        // Puts his gun sprite back and hides the gun the way the game does when its visible time runs out (HideGun,
        // Gun.cs:1598-1630: body arms back, facing kept). His own gun reappears on his next shot via ShowGun.
        private static void Restore(Held held)
        {
            RailshotFire.Guard("held railgun restore", () =>
            {
                var gun = held.Gun;
                if (gun == null || gun.gunSprite == null) return;

                if (gun.gunSprite.spriteId == held.SpriteId) gun.gunSprite.SetSprite(held.SavedSpriteId);

                // The custom sprite's own material is put back on its own condition: the sprite id may already have been
                // changed by the game while our material is still on the renderer.
                var renderer = gun.gunSpriteRenderer;
                if (held.Custom && renderer != null)
                {
                    var definitions = gun.gunSprite.Collection != null ? gun.gunSprite.Collection.spriteDefinitions : null;
                    var custom = definitions != null && held.SpriteId >= 0 && held.SpriteId < definitions.Length ? definitions[held.SpriteId].material : null;
                    if (custom != null && renderer.sharedMaterial == custom) renderer.sharedMaterial = held.SavedMaterial ?? gun.gunOff;
                }

                if (held.Agent == null) return;
                gun.HideGun();
            });
        }

        private static void ApplySprite(Held held)
        {
            var sprite = held.Gun.gunSprite;
            if (sprite.spriteId != held.SpriteId) sprite.SetSprite(held.SpriteId);
            if (!held.Custom) return;

            // A custom sprite has its own texture; the game's lighting refresh puts the shared items atlas material
            // back (ObjectSprite.cs:1408), which would draw the wrong part of the atlas.
            var renderer = held.Gun.gunSpriteRenderer;
            if (renderer != null && renderer.sharedMaterial == held.Gun.gunOff)
            {
                var material = sprite.CurrentSprite?.material;
                if (material != null) renderer.sharedMaterial = material;
            }
        }

        private static void KeepVisible(Held held)
        {
            held.Gun.visibleTime = KeepVisibleSeconds;
            held.Gun.visibleGun = held.Agent.inventory.equippedWeapon;
        }

        private static bool TryGet(Gun gun, out Held held)
        {
            held = null!;
            if (Holding.Count == 0 || gun == null || gun.agent == null) return false;
            if (!Holding.TryGetValue(gun.agent.agentID, out var found) || found.Gun != gun) return false;
            held = found;
            return true;
        }
    }
}
