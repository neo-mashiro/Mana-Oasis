using System.Linq;
using UnityEngine;
using Animancer;

namespace Players {

    public class AirborneState : MotionState {

        [SerializeField] private ClipState.Transition clipTransition;

        private MotionState[] _unreachableStates;

        public override bool CanExitState {
            get {
                var nextState = AnimatorController.NextState;
                if (nextState.Priority > Priority) {
                    return true;
                }
                return !_unreachableStates.Contains(nextState);
            }
        }

        private void Start() {
            _unreachableStates = new[] {
                AnimatorController.Move, AnimatorController.Jump, AnimatorController.Crouch
            };
        }

        private void OnEnable() {
            Debug.Log("Enter airborne state");
            AnimatorController.Animancer.Play(clipTransition);
        }
        
        private void OnDisable() {
            Debug.Log("Exit airborne state");
        }
    }
}
