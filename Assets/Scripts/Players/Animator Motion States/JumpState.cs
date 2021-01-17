using UnityEngine;
using Animancer;

namespace Players {

    public class JumpState : MotionState {

        [SerializeField] private ClipState.Transition clipTransition;

        public override bool CanExitState =>
            AnimatorController.NextState.Priority > Priority || _finished;

        private bool _finished;

        protected override void OnAwake() {
            clipTransition.Events.OnEnd = () => _finished = true;
        }

        private void OnEnable() {
            Debug.Log("Enter jump state");
            _finished = false;
            AnimatorController.Animancer.Play(clipTransition);
        }
        
        private void OnDisable() {
            Debug.Log("Exit jump state");
        }
    }
}
