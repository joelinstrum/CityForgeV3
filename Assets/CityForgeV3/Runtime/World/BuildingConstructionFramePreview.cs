using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    [DisallowMultipleComponent]
    public sealed class BuildingConstructionFramePreview : MonoBehaviour
    {
        public const float FootprintScale = 0.8f;
        private readonly List<Material> _materials = new();
        private Material _timber;
        private Material _panel;
        private float _width;
        private float _depth;
        private float _height;
        private int _stories;
        private int _revealedStories;
        public GameObject OwnerRoot { get; private set; }

        public void SetOwner(GameObject ownerRoot)
        {
            OwnerRoot = ownerRoot;
        }

        public void Build(float width, float depth, float height)
        {
            Configure(width, depth, height);
            while (_revealedStories < _stories)
                RevealNextStory();
        }

        public void Configure(float width, float depth, float height)
        {
            _width = Mathf.Max(2f, width * FootprintScale);
            _depth = Mathf.Max(2f, depth * FootprintScale);
            _height = Mathf.Max(3f, height);
            _stories = Mathf.Clamp(Mathf.RoundToInt(_height / 3.2f), 1, 12);
            _revealedStories = 0;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");
            _timber = new Material(shader)
            {
                name = "CF Full Construction Frame",
                color = new Color(0.72f, 0.39f, 0.11f)
            };
            if (_timber.HasProperty("_BaseColor"))
                _timber.SetColor("_BaseColor", _timber.color);
            if (_timber.HasProperty("_Smoothness"))
                _timber.SetFloat("_Smoothness", 0.05f);
            _materials.Add(_timber);
            _panel = new Material(shader)
            {
                name = "CF Construction Wall Panels",
                color = new Color(0.48f, 0.30f, 0.15f)
            };
            if (_panel.HasProperty("_BaseColor"))
                _panel.SetColor("_BaseColor", _panel.color);
            if (_panel.HasProperty("_Smoothness"))
                _panel.SetFloat("_Smoothness", 0.02f);
            _materials.Add(_panel);
        }

        public bool RevealNextStory()
        {
            if (_timber == null || _revealedStories >= _stories) return false;
            BuildStory(_revealedStories++);
            return true;
        }

        public bool EncloseStory(int story)
        {
            if (_panel == null || story < 0 || story >= _revealedStories)
                return false;
            var storyHeight = _height / _stories;
            var bottom = story * storyHeight;
            BuildWindowedWall($"Front Panel Wall — Story {story + 1}",
                _width, bottom, storyHeight, -_depth * 0.5f, false);
            BuildWindowedWall($"Rear Panel Wall — Story {story + 1}",
                _width, bottom, storyHeight, _depth * 0.5f, false);
            BuildWindowedWall($"Left Panel Wall — Story {story + 1}",
                _depth, bottom, storyHeight, -_width * 0.5f, true);
            BuildWindowedWall($"Right Panel Wall — Story {story + 1}",
                _depth, bottom, storyHeight, _width * 0.5f, true);
            return true;
        }

        private void BuildWindowedWall(string name, float length, float bottom,
            float storyHeight, float fixedAxis, bool alongDepth)
        {
            const float thickness = 0.10f;
            var bays = Mathf.Max(1, Mathf.RoundToInt(length / 3f));
            var bayWidth = length / bays;
            var windowWidth = Mathf.Min(1.45f, bayWidth * 0.52f);
            var windowBottom = bottom + storyHeight * 0.26f;
            var windowHeight = storyHeight * 0.50f;
            var wallTop = bottom + storyHeight;

            for (var bay = 0; bay < bays; bay++)
            {
                var centre = -length * 0.5f + (bay + 0.5f) * bayWidth;
                var sideWidth = Mathf.Max(0.12f,
                    (bayWidth - windowWidth) * 0.5f);
                CreateWallPanel($"{name} Bay {bay + 1} Left",
                    centre - windowWidth * 0.5f - sideWidth * 0.5f,
                    bottom + storyHeight * 0.5f, sideWidth, storyHeight,
                    fixedAxis, alongDepth, thickness);
                CreateWallPanel($"{name} Bay {bay + 1} Right",
                    centre + windowWidth * 0.5f + sideWidth * 0.5f,
                    bottom + storyHeight * 0.5f, sideWidth, storyHeight,
                    fixedAxis, alongDepth, thickness);
                CreateWallPanel($"{name} Bay {bay + 1} Sill",
                    centre, bottom + (windowBottom - bottom) * 0.5f,
                    windowWidth, windowBottom - bottom,
                    fixedAxis, alongDepth, thickness);
                CreateWallPanel($"{name} Bay {bay + 1} Header",
                    centre, windowBottom + windowHeight +
                            (wallTop - windowBottom - windowHeight) * 0.5f,
                    windowWidth, wallTop - windowBottom - windowHeight,
                    fixedAxis, alongDepth, thickness);
            }
        }

        private void CreateWallPanel(string name, float runningPosition,
            float y, float runningSize, float height, float fixedAxis,
            bool alongDepth, float thickness)
        {
            var position = alongDepth
                ? new Vector3(fixedAxis, y, runningPosition)
                : new Vector3(runningPosition, y, fixedAxis);
            var scale = alongDepth
                ? new Vector3(thickness, height, runningSize)
                : new Vector3(runningSize, height, thickness);
            CreateBeam(name, position, scale, _panel);
        }

        private void BuildStory(int story)
        {
            var storyHeight = _height / _stories;
            var bottom = story * storyHeight;
            var top = Mathf.Min(_height, (story + 1) * storyHeight);
            var segmentHeight = top - bottom;

            const float beam = 0.13f;
            var halfWidth = _width * 0.5f;
            var halfDepth = _depth * 0.5f;
            var widthBays = Mathf.Max(1, Mathf.CeilToInt(_width / 3f));
            var depthBays = Mathf.Max(1, Mathf.CeilToInt(_depth / 3f));

            for (var bay = 0; bay <= widthBays; bay++)
            {
                var x = Mathf.Lerp(-halfWidth, halfWidth,
                    bay / (float)widthBays);
                CreateBeam($"Front Full-Height Post {bay + 1}",
                    new Vector3(x, bottom + segmentHeight * 0.5f, -halfDepth),
                    new Vector3(beam, segmentHeight, beam), _timber);
                CreateBeam($"Rear Full-Height Post {bay + 1}",
                    new Vector3(x, bottom + segmentHeight * 0.5f, halfDepth),
                    new Vector3(beam, segmentHeight, beam), _timber);
            }
            for (var bay = 1; bay < depthBays; bay++)
            {
                var z = Mathf.Lerp(-halfDepth, halfDepth,
                    bay / (float)depthBays);
                CreateBeam($"Left Full-Height Post {bay + 1}",
                    new Vector3(-halfWidth, bottom + segmentHeight * 0.5f, z),
                    new Vector3(beam, segmentHeight, beam), _timber);
                CreateBeam($"Right Full-Height Post {bay + 1}",
                    new Vector3(halfWidth, bottom + segmentHeight * 0.5f, z),
                    new Vector3(beam, segmentHeight, beam), _timber);
            }

            foreach (var y in story == 0
                         ? new[] { bottom + 0.04f, top + 0.04f }
                         : new[] { top + 0.04f })
            {
                CreateBeam($"Front Story Rail {story + 1}",
                    new Vector3(0f, y, -halfDepth),
                    new Vector3(_width, beam, beam), _timber);
                CreateBeam($"Rear Story Rail {story + 1}",
                    new Vector3(0f, y, halfDepth),
                    new Vector3(_width, beam, beam), _timber);
                CreateBeam($"Left Story Rail {story + 1}",
                    new Vector3(-halfWidth, y, 0f),
                    new Vector3(beam, beam, _depth), _timber);
                CreateBeam($"Right Story Rail {story + 1}",
                    new Vector3(halfWidth, y, 0f),
                    new Vector3(beam, beam, _depth), _timber);
            }

            var low = bottom + 0.12f;
            var high = top - 0.12f;
            CreateDiagonal($"Front Diagonal {story + 1}",
                new Vector3(-halfWidth, low, -halfDepth - 0.02f),
                new Vector3(halfWidth, high, -halfDepth - 0.02f),
                beam, _timber);
            CreateDiagonal($"Right Diagonal {story + 1}",
                new Vector3(halfWidth + 0.02f, low, -halfDepth),
                new Vector3(halfWidth + 0.02f, high, halfDepth),
                beam, _timber);
        }

        private void CreateDiagonal(string name, Vector3 from, Vector3 to,
            float thickness, Material material)
        {
            var direction = to - from;
            var beam = CreateBeam(name, (from + to) * 0.5f,
                new Vector3(thickness, thickness, direction.magnitude), material);
            beam.transform.localRotation = Quaternion.LookRotation(direction);
        }

        private GameObject CreateBeam(string name, Vector3 position,
            Vector3 scale, Material material)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = name;
            beam.transform.SetParent(transform, false);
            beam.transform.localPosition = position;
            beam.transform.localScale = scale;
            var collider = beam.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var renderer = beam.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return beam;
        }

        private void OnDestroy()
        {
            foreach (var material in _materials)
                if (material != null)
                {
                    if (Application.isPlaying) Destroy(material);
                    else DestroyImmediate(material);
                }
        }
    }
}
