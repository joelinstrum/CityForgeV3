using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CityForgeV3.World
{
    public sealed class ThreeDimensionalCharacterAnimator : MonoBehaviour
    {
        private readonly Dictionary<string, AnimationClip> _clips =
            new(StringComparer.OrdinalIgnoreCase);
        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;
        private string _state = "";
        private Transform _animatedRoot;
        private Vector3 _anchoredLocalPosition;
        private Quaternion _anchoredLocalRotation;

        public string State => _state;
        public IReadOnlyCollection<string> States => _clips.Keys;

        public void Initialize(Animator animator, IEnumerable<AnimationClip> clips)
        {
            _animatedRoot = animator != null ? animator.transform : null;
            if (_animatedRoot != null)
            {
                _anchoredLocalPosition = _animatedRoot.localPosition;
                _anchoredLocalRotation = _animatedRoot.localRotation;
            }
            _clips.Clear();
            foreach (var clip in clips)
            {
                if (clip == null || clip.name.StartsWith("__preview__")) continue;
                var state = StateName(clip.name);
                if (!_clips.ContainsKey(state)) _clips.Add(state, clip);
            }
            if (_graph.IsValid()) _graph.Destroy();
            _graph = PlayableGraph.Create($"{name} Character Animation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput.Create(_graph, "Character", animator);
        }

        public bool Play(string state)
        {
            if (!_graph.IsValid() || !_clips.TryGetValue(state, out var clip))
                return false;
            var output = (AnimationPlayableOutput)_graph.GetOutput(0);
            if (_playable.IsValid()) _playable.Destroy();
            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetApplyFootIK(true);
            _playable.SetSpeed(1d);
            output.SetSourcePlayable(_playable);
            _state = state;
            if (!_graph.IsPlaying()) _graph.Play();
            return true;
        }

        private void Update()
        {
            if (!_playable.IsValid()) return;
            var clip = _playable.GetAnimationClip();
            if (clip == null || clip.length <= 0.01f ||
                _playable.GetTime() < clip.length) return;
            if (IsLoopingState(_state))
                _playable.SetTime(0d);
            else
            {
                _playable.SetTime(clip.length);
                _playable.SetSpeed(0d);
            }
        }

        private void LateUpdate()
        {
            // Some humanoid exports animate their root transform even when
            // Animator.applyRootMotion is disabled. Lot movement owns the
            // presentation position, so keep the animated mesh locked to it.
            if (_animatedRoot == null) return;
            _animatedRoot.localPosition = _anchoredLocalPosition;
            _animatedRoot.localRotation = _anchoredLocalRotation;
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        private static string StateName(string clipName)
        {
            var name = clipName.ToLowerInvariant();
            if (name.Contains("run_upstairs")) return "run_upstairs";
            if (name.Contains("look_around")) return "look_around";
            if (name.Contains("fold_arms")) return "fold_arms";
            if (name.Contains("hit_to_body")) return "hit_to_body";
            if (name.Contains("angry")) return "angry";
            if (name.Contains("flee")) return "flee";
            if (name.Contains("afraid")) return "afraid";
            if (name.Contains("agree")) return "agree";
            if (name.Contains("clap")) return "clap";
            if (name.Contains("fall")) return "fall";
            if (name.Contains("laugh")) return "laugh";
            if (name.Contains("wait")) return "wait";
            if (name.Contains("walk")) return "walk";
            if (name.Contains("run")) return "run";
            if (name.Contains("idle")) return "idle";
            if (name.Contains("bow")) return "bow";
            if (name.Contains("sit")) return "sit";
            return name;
        }

        private static bool IsLoopingState(string state) =>
            state is "walk" or "run" or "wait" or "idle" or "look_around" or
                "fold_arms" or "angry";
    }
}
