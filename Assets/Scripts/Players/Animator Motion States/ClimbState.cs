using UnityEngine;
using Animancer;

namespace Players {

    public class ClimbState : MotionState {

        [SerializeField] private LinearMixerState.Transition mixerTransition;

        public override bool CanExitState {
            get {
                var nextState = AnimatorController.NextState;
                return nextState == AnimatorController.Airborne ||
                       nextState == AnimatorController.Move ||
                       nextState == AnimatorController.Idle;
            }
        }
        
        private void OnEnable() {
            Debug.Log("Enter climb state");
            AnimatorController.Animancer.Play(mixerTransition);
        }
        
        private void OnDisable() {
            Debug.Log("Exit climb state");
        }

        private void Update() {
            mixerTransition.State.Parameter = AnimatorController.ParameterX;
        }
    }
}
