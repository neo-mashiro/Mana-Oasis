using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Animancer;

namespace Players {

    public class IdleState : MotionState {

        [SerializeField] private ClipState.Transition mainClipTransition;
        [SerializeField] private ClipState.Transition[] randomClipTransitions;
        [SerializeField] private float initialTimeoutDelay = 5;
        [SerializeField] private float minTimeout = 5;
        [SerializeField] private float maxTimeout = 10;

        private float _timeout;

        public override bool CanExitState =>
            AnimatorController.NextState != AnimatorController.Land;

        protected override void OnAwake() {
            Action onEnd = PlayMainAnimation;
            foreach (var clipTransition in randomClipTransitions) {
                clipTransition.Events.OnEnd = onEnd;
            }
        }

        private void OnEnable() {
            Debug.Log("Enter idle state");
            PlayMainAnimation();
            _timeout += initialTimeoutDelay;
        }
        
        private void OnDisable() {
            Debug.Log("Exit idle state");
        }

        private void Update() {
            // don't confuse AnimancerState with MotionState, they are completely different concepts
            // at any given time, there can only be one active AnimancerState in one active MotionState
            // => AnimancerState is the state of an animation clip or transition or mixer transition
            // => MotionState is a state node registered in the state machine
            var state = AnimatorController.Animancer.States.Current;
            if (state == mainClipTransition.State && state.Time >= _timeout) {
                PlayRandomAnimation();
            }
        }

        private void PlayMainAnimation() {
            _timeout = Random.Range(minTimeout, maxTimeout);
            AnimatorController.Animancer.Play(mainClipTransition);
        }
        
        private void PlayRandomAnimation() {
            var index = Random.Range(0, randomClipTransitions.Length);
            AnimatorController.Animancer.Play(randomClipTransitions[index]);
            CustomFade.Apply(AnimatorController.Animancer, Easing.Sine.InOut);
        }
    }
}
