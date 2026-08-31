using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    [DisallowMultipleComponent]
    public sealed class BuildingConstructionSequence : MonoBehaviour
    {
        public const float SecondsPerStory = 1f;
        private readonly List<RendererState> _finishedRenderers = new();
        private readonly List<Material> _materials = new();
        private Transform _temporaryRoot;
        private Material _dirtMaterial;
        private BuildingConstructionFramePreview _frame;
        private float _width;
        private float _depth;
        private float _groundY;
        private float _storyHeight;
        private int _storyCount;
        private Action _changed;
        private Coroutine _routine;

        private readonly struct RendererState
        {
            public readonly Renderer Renderer;
            public readonly bool Enabled;
            public readonly Material[] Materials;

            public RendererState(Renderer renderer)
            {
                Renderer = renderer;
                Enabled = renderer.enabled;
                Materials = renderer.sharedMaterials;
            }
        }

        public int CompletedStories { get; private set; }
        public int RevealedBuildingStories { get; private set; }
        public int StoryCount => _storyCount;
        public bool IsComplete { get; private set; }
        public string StageLabel => IsComplete
            ? "CONSTRUCTION COMPLETE"
            : CompletedStories == _storyCount &&
              RevealedBuildingStories < _storyCount
                ? "REVEALING FINAL STORY"
            : CompletedStories == 0
                ? "PREPARING FOUNDATION"
                : $"BUILDING STORY {CompletedStories} OF {_storyCount}";

        public void Begin(GameObject finishedBuilding, float width,
            float depth, float height, Action changed = null,
            Vector3? localOrigin = null,
            Vector3? localScaleCompensation = null,
            bool useWorldSpace = false)
        {
            if (finishedBuilding == null || _temporaryRoot != null) return;
            _width = Mathf.Max(2f, width);
            _depth = Mathf.Max(2f, depth);
            _storyCount = Mathf.Clamp(Mathf.RoundToInt(
                Mathf.Max(3f, height) / 3.2f), 1, 12);
            _storyHeight = Mathf.Max(3f, height) / _storyCount;
            _groundY = useWorldSpace && localOrigin.HasValue
                ? localOrigin.Value.y
                : finishedBuilding.transform.position.y;
            _changed = changed;

            foreach (var renderer in finishedBuilding
                         .GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                var state = new RendererState(renderer);
                _finishedRenderers.Add(state);
                var revealMaterials = new Material[state.Materials.Length];
                var supportsReveal = false;
                for (var index = 0; index < state.Materials.Length; index++)
                {
                    var source = state.Materials[index];
                    if (source == null ||
                        !source.HasProperty("_ConstructionRevealHeight"))
                    {
                        revealMaterials[index] = source;
                        continue;
                    }
                    var reveal = new Material(source)
                    {
                        name = $"{source.name} — Construction Reveal"
                    };
                    reveal.SetFloat("_ConstructionRevealHeight", _groundY - 0.01f);
                    revealMaterials[index] = reveal;
                    _materials.Add(reveal);
                    supportsReveal = true;
                }
                renderer.sharedMaterials = revealMaterials;
                renderer.enabled = state.Enabled && supportsReveal;
            }

            _temporaryRoot = new GameObject("Temporary Construction Works")
                .transform;
            if (useWorldSpace)
            {
                // Use the exact coordinate contract of the standalone FRAME
                // preview. Imported model pivots, axis conversion rotations,
                // and package scales must never affect construction geometry.
                _temporaryRoot.SetParent(null, false);
                _temporaryRoot.SetPositionAndRotation(
                    localOrigin ?? transform.position, Quaternion.identity);
                _temporaryRoot.localScale = Vector3.one;
            }
            else
            {
                _temporaryRoot.SetParent(transform, false);
                _temporaryRoot.localPosition = localOrigin ?? Vector3.zero;
                _temporaryRoot.localScale =
                    localScaleCompensation ?? Vector3.one;
            }
            CreateMaterials();
            CreateBox("Excavated Dirt Footprint", new Vector3(0f, 0.025f, 0f),
                new Vector3(_width * BuildingConstructionFramePreview.FootprintScale,
                    0.05f,
                    _depth * BuildingConstructionFramePreview.FootprintScale),
                _dirtMaterial);
            var frameRoot = new GameObject("Progressive Construction Frame");
            frameRoot.transform.SetParent(_temporaryRoot, false);
            _frame = frameRoot.AddComponent<BuildingConstructionFramePreview>();
            _frame.Configure(_width, _depth, height);
            _changed?.Invoke();
            if (Application.isPlaying)
                _routine = StartCoroutine(AdvanceAutomatically());
        }

        public void AdvanceOneStageForQa()
        {
            if (IsComplete || _temporaryRoot == null) return;
            if (CompletedStories < _storyCount)
            {
                CompletedStories++;
                _frame?.RevealNextStory();
                if (CompletedStories > 1)
                {
                    _frame?.EncloseStory(CompletedStories - 2);
                    RevealedBuildingStories = CompletedStories - 1;
                    SetBuildingRevealHeight(_groundY +
                        RevealedBuildingStories * _storyHeight + 0.01f);
                }
                _changed?.Invoke();
                return;
            }
            if (RevealedBuildingStories < _storyCount)
            {
                // The uppermost frame gets a full beat on screen before the
                // final floor of the finished mesh is exposed.
                RevealedBuildingStories = _storyCount;
                SetBuildingRevealHeight(
                    _groundY + _storyCount * _storyHeight + 0.01f);
                _changed?.Invoke();
                return;
            }
            CompleteConstruction();
        }

        private IEnumerator AdvanceAutomatically()
        {
            while (!IsComplete)
            {
                yield return new WaitForSeconds(SecondsPerStory);
                AdvanceOneStageForQa();
            }
        }

        private void SetBuildingRevealHeight(float worldHeight)
        {
            foreach (var state in _finishedRenderers)
            {
                if (state.Renderer == null) continue;
                foreach (var material in state.Renderer.sharedMaterials)
                    if (material != null &&
                        material.HasProperty("_ConstructionRevealHeight"))
                        material.SetFloat("_ConstructionRevealHeight", worldHeight);
            }
        }

        private void CompleteConstruction()
        {
            IsComplete = true;
            foreach (var state in _finishedRenderers)
                if (state.Renderer != null)
                {
                    state.Renderer.sharedMaterials = state.Materials;
                    state.Renderer.enabled = state.Enabled;
                }
            if (_temporaryRoot != null)
            {
                if (Application.isPlaying) Destroy(_temporaryRoot.gameObject);
                else DestroyImmediate(_temporaryRoot.gameObject);
            }
            _temporaryRoot = null;
            _changed?.Invoke();
        }

        private void CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");
            _dirtMaterial = CreateMaterial(shader, "CF Construction Dirt",
                new Color(0.24f, 0.135f, 0.065f));
        }

        private Material CreateMaterial(Shader shader, string name, Color color)
        {
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.08f);
            _materials.Add(material);
            return material;
        }

        private void CreateBox(string name, Vector3 position, Vector3 scale,
            Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(_temporaryRoot, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            var collider = box.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private void OnDestroy()
        {
            if (_routine != null) StopCoroutine(_routine);
            if (!IsComplete)
                foreach (var state in _finishedRenderers)
                    if (state.Renderer != null)
                    {
                        state.Renderer.sharedMaterials = state.Materials;
                        state.Renderer.enabled = state.Enabled;
                    }
            if (_temporaryRoot != null)
            {
                if (Application.isPlaying) Destroy(_temporaryRoot.gameObject);
                else DestroyImmediate(_temporaryRoot.gameObject);
                _temporaryRoot = null;
            }
            foreach (var material in _materials)
                if (material != null)
                {
                    if (Application.isPlaying) Destroy(material);
                    else DestroyImmediate(material);
                }
        }
    }
}
