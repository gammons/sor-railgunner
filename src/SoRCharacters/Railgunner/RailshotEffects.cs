using System.Collections.Generic;
using SoRCharacters.Core.Railgunner;
using UnityEngine;

namespace SoRCharacters.Railgunner
{
    // The beam and sparks use gc.normalShader ("tk2d/BlendVertexColor": alpha blend, texture × vertex colour, no _Color).
    // gc.lightUpShader is the additive white hit-flash shader and can't draw blue. Colour lives in the generated
    // textures; the fade sets the vertex alpha and also narrows the lines and shrinks the sparks, so the effect still
    // fades out even if vertex colour were ignored, and everything is destroyed when its time is up.
    internal static class RailshotEffects
    {
        private const float Z = -8f;
        private const int EffectLayer = 1;
        private const float TileUnits = 0.64f;
        private const float LightSpacing = 3f * 0.64f;
        private const float PathMargin = 1.28f;

        private const float GlowWidth = 0.18f;
        private const float CoreWidth = 0.05f;
        private const float AfterglowWidth = 0.3f;
        private const float FlashWidth = 0.6f;
        private static readonly Color GlowColor = new Color(0.35f, 0.7f, 1f, 0.8f);
        private static readonly Color CoreColor = new Color(0.88f, 0.95f, 1f, 1f);
        private static readonly Color AfterglowColor = new Color(0.3f, 0.6f, 1f, 0.3f);
        private static readonly Color FlashColor = Color.white;

        private const int MaxSparksPerShot = 1500;
        private const int BurstSparks = 30;
        private const int MuzzleSparks = 40;
        private const float MuzzleMinLifetime = 0.1f;
        private const float MuzzleMaxLifetime = 0.15f;
        private const float MuzzleMinSize = 0.15f;
        private const float MuzzleMaxSize = 0.3f;
        private const float SparkMinLifetime = 0.4f;
        private const float SparkMaxLifetime = 0.8f;
        private const float SparkMinSize = 0.06f;
        private const float SparkMaxSize = 0.12f;
        private const float SparkDampen = 0.04f;
        private const float SparksMaxSeconds = 3f;

        private sealed class BeamLine
        {
            public GameObject Object = null!;
            public LineRenderer Renderer = null!;
            public Material Material = null!;
            public float Width;
            public float StartedAt;
            public float Seconds;
        }

        private sealed class Sparks
        {
            public GameObject Object = null!;
            public ParticleSystem System = null!;
            public Material Material = null!;
            public float StartedAt;
        }

        private sealed class PathUpdate
        {
            public Bounds Bounds;
            public int Frame;
        }

        private static readonly List<BeamLine> Lines = new List<BeamLine>();
        private static readonly List<Sparks> SparkSystems = new List<Sparks>();
        private static readonly List<PathUpdate> PathUpdates = new List<PathUpdate>();
        private static Texture2D? _glowTexture;
        private static Texture2D? _coreTexture;
        private static Texture2D? _afterglowTexture;
        private static Texture2D? _flashTexture;
        private static Texture2D? _sparkTexture;
        private static bool _loggedLineFailed;
        private static bool _loggedSparksFailed;
        private static bool _loggedCreated;

        public static bool Any => Lines.Count != 0 || SparkSystems.Count != 0 || PathUpdates.Count != 0;

        public static void Play(Agent shooter, Vector2 origin, Vector2 end, Vector2 dir, ShotResult result, RailgunnerSettings settings)
        {
            var gc = GameController.gameController;
            var length = Vector2.Distance(origin, end);
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            RailshotFire.Guard("noise", () =>
            {
                gc.spawnerMain.SpawnNoise(origin, 4f, null, null, shooter);
                gc.spawnerMain.SpawnNoise(end, 4f, null, null, shooter);
            });
            RailshotFire.Guard("shake", () =>
            {
                gc.ScreenShake(settings.ShakeSeconds, settings.ShakeOffset, origin, null);
                gc.ScreenBump(2f, 30, shooter);
            });
            RailshotFire.Guard("beam", () => StartBeam(gc, origin, end, settings));
            RailshotFire.Guard("muzzle burst", () => StartMuzzleBurst(gc, origin));
            RailshotFire.Guard("sparks", () => StartSparks(gc, origin, end, dir, result, settings));
            RailshotFire.Guard("muzzle light", () => gc.spawnerMain.SpawnLightTemp(origin, null, "Explosion"));
            RailshotFire.Guard("lights", () =>
            {
                for (var d = 0f; d <= length; d += LightSpacing)
                {
                    gc.spawnerMain.SpawnLightTemp(origin + dir * d, null, "GunFlash");
                }
            });
            RailshotFire.Guard("end spark", () => gc.spawnerMain.SpawnParticleEffect("BulletHitLaser", end, angle));
            RailshotFire.Guard("fire sound", () => gc.audioHandler.Play(shooter, RailgunnerModule.ActiveFireSound));
            RailshotFire.Guard("fire sound 2", () => gc.audioHandler.Play(shooter, settings.FireSound2));
            if (result.WallsCut > 0) RailshotFire.Guard("wall sound", () => gc.audioHandler.Play(shooter, "WallDestroy"));
        }

        // Explosion.Testt (Explosion.cs:1557-1573) updates the A* graph a frame after the walls go, once their
        // objects are deactivated; this does the same over the beam's bounding box.
        public static void QueuePathUpdate(Vector2 origin, Vector2 end)
        {
            var bounds = new Bounds(origin, Vector3.zero);
            bounds.Encapsulate(end);
            bounds.Expand(new Vector3(PathMargin * 2f, PathMargin * 2f, 0f));
            bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
            PathUpdates.Add(new PathUpdate { Bounds = bounds, Frame = Time.frameCount });
        }

        private static void StartBeam(GameController gc, Vector2 origin, Vector2 end, RailgunnerSettings settings)
        {
            AddLine(gc, "RailshotFlash", origin, end, FlashWidth, settings.FlashSeconds, 1004,
                ref _flashTexture, FlashColor, 0.3f);
            AddLine(gc, "RailshotAfterglow", origin, end, AfterglowWidth, settings.AfterglowSeconds, 1000,
                ref _afterglowTexture, AfterglowColor, 0.1f);
            AddLine(gc, "RailshotGlow", origin, end, GlowWidth, settings.BeamFadeSeconds, 1001,
                ref _glowTexture, GlowColor, 0.35f);
            AddLine(gc, "RailshotCore", origin, end, CoreWidth, settings.BeamFadeSeconds, 1002,
                ref _coreTexture, CoreColor, 0.5f);
        }

        private static void AddLine(GameController gc, string name, Vector2 origin, Vector2 end, float width, float seconds,
            int sortingOrder, ref Texture2D? texture, Color color, float solidCore)
        {
            var go = new GameObject(name);
            go.layer = EffectLayer;
            var line = new BeamLine { Object = go, Width = width, StartedAt = Time.unscaledTime, Seconds = seconds };
            Lines.Add(line);

            try
            {
                line.Renderer = go.AddComponent<LineRenderer>();
                line.Material = new Material(gc.normalShader);
                if (texture == null) texture = CrossSectionTexture(name, color, solidCore);
                line.Material.mainTexture = texture;
            }
            catch (System.Exception e)
            {
                Lines.Remove(line);
                Destroy(line);
                if (!_loggedLineFailed)
                {
                    _loggedLineFailed = true;
                    Plugin.Log.LogError($"[Railgunner] beam line '{name}' skipped: {e}");
                }
                return;
            }
            if (line.Material.HasProperty("_Color")) line.Material.color = Color.white;

            var renderer = line.Renderer;
            renderer.positionCount = 2;
            renderer.useWorldSpace = true;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.material = line.Material;
            renderer.sortingOrder = sortingOrder;
            renderer.SetPosition(0, new Vector3(origin.x, origin.y, Z));
            renderer.SetPosition(1, new Vector3(end.x, end.y, Z));
            SetStrength(line, 1f);

            if (!_loggedCreated)
            {
                _loggedCreated = true;
                Plugin.Log.LogInfo($"[Railgunner] beam created (layer {go.layer}, shader {line.Material.shader.name}, " +
                                   $"_MainTex {line.Material.HasProperty("_MainTex")}, _Color {line.Material.HasProperty("_Color")})");
            }
        }

        private static void SetStrength(BeamLine line, float strength)
        {
            var colour = new Color(1f, 1f, 1f, strength);
            line.Renderer.startColor = colour;
            line.Renderer.endColor = colour;
            var width = line.Width * Mathf.Sqrt(strength);
            line.Renderer.startWidth = width;
            line.Renderer.endWidth = width;
        }

        private static void StartSparks(GameController gc, Vector2 origin, Vector2 end, Vector2 dir, ShotResult result, RailgunnerSettings settings)
        {
            var bursts = new List<Vector2>(result.KillPositions.Count + result.WallCutPositions.Count + 1) { end };
            bursts.AddRange(result.KillPositions);
            bursts.AddRange(result.WallCutPositions);

            var length = Vector2.Distance(origin, end);
            var budget = MaxSparksPerShot - MuzzleSparks;
            var burstEach = Mathf.Min(BurstSparks, budget / bursts.Count);
            var lineSparks = Mathf.RoundToInt(Mathf.Max(0f, Mathf.Min(length / TileUnits * settings.SparksPerTile, budget - burstEach * bursts.Count)));
            if (burstEach + lineSparks <= 0) return;

            var sparks = CreateSparks(gc, origin, "RailshotSparks", SparkGradient(), SparkMinLifetime, SparkMaxLifetime,
                SparkMinSize, SparkMaxSize, SparkDampen, MaxSparksPerShot, 1003);
            var streaks = sparks.Object.GetComponent<ParticleSystemRenderer>();
            streaks.renderMode = ParticleSystemRenderMode.Stretch;
            streaks.velocityScale = 0.06f;
            streaks.lengthScale = 1.5f;
            var system = sparks.System;
            var normal = new Vector2(-dir.y, dir.x);
            var emit = new ParticleSystem.EmitParams();

            for (var i = 0; i < lineSparks; i++)
            {
                var position = origin + dir * (length * Random.value);
                var side = Random.value < 0.5f ? -1f : 1f;
                var velocity = normal * (side * Random.Range(3f, 8f)) + dir * Random.Range(-1.5f, 1.5f);
                Emit(system, ref emit, position, velocity);
            }

            foreach (var burst in bursts)
            {
                for (var i = 0; i < burstEach; i++)
                {
                    var velocity = Random.insideUnitCircle.normalized * Random.Range(4f, 10f);
                    Emit(system, ref emit, burst, velocity);
                }
            }
        }

        private static void StartMuzzleBurst(GameController gc, Vector2 origin)
        {
            var sparks = CreateSparks(gc, origin, "RailshotMuzzleBurst", MuzzleGradient(), MuzzleMinLifetime, MuzzleMaxLifetime,
                MuzzleMinSize, MuzzleMaxSize, SparkDampen, MuzzleSparks, 1005);
            var emit = new ParticleSystem.EmitParams();
            for (var i = 0; i < MuzzleSparks; i++)
            {
                var velocity = Random.insideUnitCircle.normalized * Random.Range(3f, 9f);
                Emit(sparks.System, ref emit, origin, velocity);
            }
        }

        private static void Emit(ParticleSystem system, ref ParticleSystem.EmitParams emit, Vector2 position, Vector2 velocity)
        {
            emit.position = new Vector3(position.x, position.y, Z);
            emit.velocity = new Vector3(velocity.x, velocity.y, 0f);
            system.Emit(emit, 1);
        }

        private static Gradient SparkGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.1f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Gradient MuzzleGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.6f), new GradientColorKey(new Color(0.8f, 0.92f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Sparks CreateSparks(GameController gc, Vector2 origin, string name, Gradient gradient, float minLifetime, float maxLifetime,
            float minSize, float maxSize, float dampen, int maxParticles, int sortingOrder)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.layer = EffectLayer;
            go.transform.position = new Vector3(origin.x, origin.y, Z);
            var sparks = new Sparks { Object = go, StartedAt = Time.unscaledTime };
            SparkSystems.Add(sparks);

            var system = go.AddComponent<ParticleSystem>();
            sparks.System = system;
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.useUnscaledTime = true;
            main.duration = maxLifetime;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startSpeed = 0f;
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.maxParticles = maxParticles;

            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;

            var drag = system.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = 0f;
            drag.dampen = dampen;

            var colour = system.colorOverLifetime;
            colour.enabled = true;
            colour.color = new ParticleSystem.MinMaxGradient(gradient);

            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            sparks.Material = new Material(gc.normalShader);
            sparks.Material.mainTexture = SparkTexture();
            if (sparks.Material.HasProperty("_Color")) sparks.Material.color = Color.white;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = sparks.Material;
            renderer.sortingOrder = sortingOrder;

            go.SetActive(true);
            system.Play();
            return sparks;
        }

        public static void Tick()
        {
            if (Lines.Count != 0) TickLines();
            if (SparkSystems.Count != 0) TickSparks();
            if (PathUpdates.Count != 0) TickPathUpdates();
        }

        private static void TickLines()
        {
            var now = Time.unscaledTime;
            for (var i = Lines.Count - 1; i >= 0; i--)
            {
                var line = Lines[i];
                try
                {
                    var t = (now - line.StartedAt) / line.Seconds;
                    if (t >= 1f || line.Object == null || line.Renderer == null)
                    {
                        Lines.RemoveAt(i);
                        Destroy(line);
                        continue;
                    }
                    SetStrength(line, 1f - t);
                }
                catch (System.Exception e)
                {
                    if (!_loggedLineFailed)
                    {
                        _loggedLineFailed = true;
                        Plugin.Log.LogError($"[Railgunner] beam tick failed: {e}");
                    }
                    if (i < Lines.Count && Lines[i] == line) Lines.RemoveAt(i);
                    Destroy(line);
                }
            }
        }

        private static void TickSparks()
        {
            var now = Time.unscaledTime;
            for (var i = SparkSystems.Count - 1; i >= 0; i--)
            {
                var sparks = SparkSystems[i];
                try
                {
                    var age = now - sparks.StartedAt;
                    var done = sparks.Object == null || sparks.System == null || age >= SparksMaxSeconds
                               || (age > SparkMaxLifetime && !sparks.System.IsAlive(true));
                    if (!done) continue;
                    SparkSystems.RemoveAt(i);
                    Destroy(sparks);
                }
                catch (System.Exception e)
                {
                    if (!_loggedSparksFailed)
                    {
                        _loggedSparksFailed = true;
                        Plugin.Log.LogError($"[Railgunner] sparks tick failed: {e}");
                    }
                    if (i < SparkSystems.Count && SparkSystems[i] == sparks) SparkSystems.RemoveAt(i);
                    Destroy(sparks);
                }
            }
        }

        private static void TickPathUpdates()
        {
            for (var i = PathUpdates.Count - 1; i >= 0; i--)
            {
                var update = PathUpdates[i];
                if (Time.frameCount <= update.Frame) continue;

                PathUpdates.RemoveAt(i);
                RailshotFire.Guard("path update", () =>
                {
                    if (AstarPath.active != null) AstarPath.active.UpdateGraphs(update.Bounds);
                });
            }
        }

        public static void Clear()
        {
            foreach (var line in Lines) Destroy(line);
            Lines.Clear();
            foreach (var sparks in SparkSystems) Destroy(sparks);
            SparkSystems.Clear();
            PathUpdates.Clear();
        }

        private static void Destroy(BeamLine line)
        {
            try
            {
                if (line.Material != null) Object.Destroy(line.Material);
                if (line.Object != null) Object.Destroy(line.Object);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"[Railgunner] beam cleanup failed: {e}");
            }
        }

        private static void Destroy(Sparks sparks)
        {
            try
            {
                if (sparks.Material != null) Object.Destroy(sparks.Material);
                if (sparks.Object != null) Object.Destroy(sparks.Object);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"[Railgunner] sparks cleanup failed: {e}");
            }
        }

        // A line's v coordinate runs across its width: solid in the middle, soft at the edges, colour and opacity baked in.
        private static Texture2D CrossSectionTexture(string name, Color color, float solidCore)
        {
            const int height = 16;
            var texture = NewTexture(name, 2, height);
            for (var y = 0; y < height; y++)
            {
                var d = Mathf.Abs((y + 0.5f) / height * 2f - 1f);
                var a = d <= solidCore ? 1f : Mathf.SmoothStep(1f, 0f, (d - solidCore) / (1f - solidCore));
                var pixel = new Color(color.r, color.g, color.b, color.a * a);
                texture.SetPixel(0, y, pixel);
                texture.SetPixel(1, y, pixel);
            }
            texture.Apply();
            return texture;
        }

        // A soft round spark: white-hot centre, pale blue rim.
        private static Texture2D SparkTexture()
        {
            if (_sparkTexture != null) return _sparkTexture;

            const int size = 8;
            var texture = NewTexture("RailshotSpark", size, size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f) / size * 2f - 1f;
                    var dy = (y + 0.5f) / size * 2f - 1f;
                    var d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    var a = Mathf.SmoothStep(1f, 0f, d);
                    texture.SetPixel(x, y, Color.Lerp(Color.white, new Color(0.7f, 0.88f, 1f), d) * new Color(1f, 1f, 1f, a));
                }
            }
            texture.Apply();
            _sparkTexture = texture;
            return texture;
        }

        private static Texture2D NewTexture(string name, int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = name,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
        }
    }
}
