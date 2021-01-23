using UnityEngine;
using Animancer;

namespace Players {

    public class FlyState : MotionState {

        [SerializeField] private MixerState.Transition2D mixerTransition2D;
        [SerializeField] private float blendSpeed = 10f;

        public override bool CanExitState {
            get {
                var nextState = AnimatorController.NextState;
                if (nextState.Priority > Priority) {
                    return true;
                }

                return nextState == AnimatorController.Airborne || nextState == AnimatorController.Move;
                // || nextState == AnimatorController.Climb;
            }
        }
        
        private void OnEnable() {
            Debug.Log("Enter fly state");
            // TODO: call the AnimatorController to enable the wings on the character
            // the wings should be part of the character and be included in the animation clips
            AnimatorController.Animancer.Play(mixerTransition2D);
        }
        
        private void OnDisable() {
            // TODO: call the AnimatorController to disable the wings on the character
            Debug.Log("Exit fly state");
        }

        private void Update() {
            mixerTransition2D.State.Parameter = Vector2.MoveTowards(
                mixerTransition2D.State.Parameter,
                new Vector2(Mathf.Max(AnimatorController.ParameterX, 0), AnimatorController.ParameterY),
                blendSpeed * Time.deltaTime);
        }
    }
}
