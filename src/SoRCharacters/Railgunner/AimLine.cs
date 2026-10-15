using System.Collections.Generic;
using UnityEngine;

namespace SoRCharacters.Railgunner
{
    internal sealed class AimLine
    {
        private const float Z = -8f;
        private const float Width = 0.02f;
        private const float TileUnits = 0.64f;
        private const float StartOffset = 0.45f;

        private static readonly Dictionary<int, AimLine> Lines = new Dictionary<int, AimLine>();
        private static bool _loggedCreated;
        private static bool _loggedPlanFailed;

        private readonly GameObject _object;
        private readonly LineRenderer _renderer;
        private readonly Material _material;

        public static bool Any => Lines.Count != 0;

        private AimLine(Agent agent)
        {
            var gc = GameController.gameController;
            _object = new GameObject("RailshotAimLine");
            _object.layer = LayerOf(agent);
            _renderer = _object.AddComponent<LineRenderer>();
            _renderer.positionCount = 2;
            _renderer.useWorldSpace = true;
            _renderer.startWidth = Width;
            _renderer.endWidth = Width;
            // gc.lightUpShader is the game's white hit-flash shader (AgentHitbox.cs:719): it keeps only alpha and draws
            // white, so the line uses the normal sprite shader with the colour and fade from a texture stretched along it.
            _material = new Material(gc.normalShader);
            _material.mainTexture = LaserTexture();
            if (_material.HasProperty("_Color")) _material.color = Color.white;
            _renderer.material = _material;
            _renderer.textureMode = LineTextureMode.Stretch;
            _renderer.startColor = Color.white;
            _renderer.endColor = Color.white;
            _renderer.sortingOrder = 1000;

            if (!_loggedCreated)
            {
                _loggedCreated = true;
                Plugin.Log.LogInfo($"[Railgunner] aim line created (layer {_object.layer}, shader {_material.shader.name}, " +
                                   $"_MainTex {_material.HasProperty("_MainTex")}, _Color {_material.HasProperty("_Color")})");
            }
        }

        public static void Show(Agent agent)
        {
            if (!agent.localPlayer || agent.isPlayer <= 0) return;
            if (Lines.TryGetValue(agent.agentID, out var existing))
            {
                if (existing._object != null) return;
                existing.Destroy();
            }

            Lines[agent.agentID] = new AimLine(agent);
        }

        public static void Hide(Agent agent)
        {
            Hide(agent.agentID);
        }

        public static void Hide(int agentId)
        {
            if (!Lines.TryGetValue(agentId, out var line)) return;

            Lines.Remove(agentId);
            line.Destroy();
        }

        public static void DestroyAll()
        {
            foreach (var line in Lines.Values) line.Destroy();
            Lines.Clear();
        }

        public static void UpdateAll()
        {
            if (Lines.Count == 0) return;

            var snapshot = RailshotState.Snapshot();
            foreach (var pair in snapshot)
            {
                if (!Lines.TryGetValue(pair.Key, out var line)) continue;
                if (pair.Value.Agent == null || line._object == null)
                {
                    Hide(pair.Key);
                    continue;
                }
                line.Refresh(pair.Value.Agent, pair.Value.Armed ? pair.Value.AimDirection : Aim.Direction(pair.Value.Agent));
            }
        }

        private void Refresh(Agent agent, Vector2 direction)
        {
            var origin = (Vector2)agent.tr.position;
            var length = StopDistance(origin, direction, GameCompat.HasBreachingSlug(agent));
            var end = origin + direction * length;
            // The line starts a little in front of him (about where the barrel would be), not at his centre.
            var start = origin + direction * Mathf.Min(StartOffset, length);

            _object.layer = LayerOf(agent);
            _renderer.SetPosition(0, new Vector3(start.x, start.y, Z));
            _renderer.SetPosition(1, new Vector3(end.x, end.y, Z));
        }

        private static float StopDistance(Vector2 origin, Vector2 direction, bool breaching)
        {
            try
            {
                return RailshotFire.Plan(origin, direction, breaching).StopDistance;
            }
            catch (System.Exception e)
            {
                if (!_loggedPlanFailed)
                {
                    _loggedPlanFailed = true;
                    Plugin.Log.LogError($"[Railgunner] aim line plan failed, drawing full range: {e}");
                }
                return RailgunnerModule.Settings.BeamRangeTiles * TileUnits;
            }
        }

        private static Texture2D? _laserTexture;

        // Bright laser red at the shooter, fading to nothing at the far end.
        private static Texture2D LaserTexture()
        {
            if (_laserTexture != null) return _laserTexture;

            const int width = 64;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "RailshotLaserGradient",
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            for (var x = 0; x < width; x++)
            {
                var fade = 1f - x / (float)(width - 1);
                fade *= fade;
                texture.SetPixel(x, 0, new Color(1f, 0.08f, 0.05f, fade));
            }
            texture.Apply();
            _laserTexture = texture;
            return texture;
        }

        private static int LayerOf(Agent agent)
        {
            var sprite = agent.targetImage != null ? agent.targetImage.spr : null;
            return sprite != null ? sprite.gameObject.layer : 0;
        }

        private void Destroy()
        {
            if (_material != null) Object.Destroy(_material);
            if (_object != null) Object.Destroy(_object);
        }
    }
}
