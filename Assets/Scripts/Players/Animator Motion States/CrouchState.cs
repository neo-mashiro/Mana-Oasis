using UnityEngine;
using Animancer;

namespace Players {

    public class CrouchState : MotionState {

        [SerializeField] private LinearMixerState.Transition mixerTransition;

        public override bool CanExitState =>
            AnimatorController.NextState.Priority > Priority ||
            AnimatorController.NextState == AnimatorController.Idle;

        private void OnEnable() {
            Debug.Log("Enter crouch state");
            AnimatorController.Animancer.Play(mixerTransition);
        }
        
        private void OnDisable() {
            Debug.Log("Exit crouch state");
        }
        
        private void Update() {
            mixerTransition.State.Parameter = AnimatorController.NextStateParameterX;
        }
    }
}
