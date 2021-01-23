using Animancer;
using UnityEngine;

namespace Players {

    public class MoveState : MotionState {

        [SerializeField] private LinearMixerState.Transition mixerTransition;
        
        public override bool CanExitState =>
            AnimatorController.NextState != AnimatorController.Land;
        
        private void OnEnable() {
            Debug.Log("Enter move state");
            AnimatorController.Animancer.Play(mixerTransition);
        }
        
        private void OnDisable() {
            Debug.Log("Exit move state");
        }

        private void Update() {
            mixerTransition.State.Parameter = AnimatorController.ParameterX;
        }
    }
}
