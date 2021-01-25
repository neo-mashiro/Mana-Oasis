using Animancer;
using UnityEngine;

namespace Players {
    
    public class SwimState : MotionState {

        [SerializeField] private MixerState.Transition2D mixerTransition2D;
        [SerializeField] private float blendSpeed = 16f;

        public override StatePriority Priority => StatePriority.Medium;

        public override bool CanExitState {
            get {
                var nextState = AnimatorController.NextState;
                if (nextState.Priority > Priority) {
                    return true;
                }

                return nextState == AnimatorController.Idle || nextState == AnimatorController.Move;
            }
        }
        
        private void OnEnable() {
            Debug.Log("Enter swim state");
            AnimatorController.Animancer.Play(mixerTransition2D);
        }
        
        private void OnDisable() {
            Debug.Log("Exit swim state");
        }

        private void Update() {
            mixerTransition2D.State.Parameter = Vector2.MoveTowards(
                mixerTransition2D.State.Parameter,
                new Vector2(Mathf.Max(AnimatorController.ParameterX, 0), AnimatorController.ParameterY),
                blendSpeed * Time.deltaTime);
        }
    }
}
