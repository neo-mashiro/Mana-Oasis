using System;
using UnityEngine;
using Animancer;

namespace Players {

    public class LandState : MotionState {
        
        [Tooltip("If the vertical speed is greater than this value, animate soft landing, else hard landing.")]
        [SerializeField] private float verticalSpeedThreshold = 11f;
        
        [SerializeField] private ClipState.Transition clipTransition;

        private float _verticalLandingSpeed;
        private bool _canExit;

        public override bool CanExitState {
            get {
                var nextState = AnimatorController.NextState;
                if (nextState.Priority > Priority) {
                    return true;
                }

                return nextState == AnimatorController.Idle && _canExit;
            }
        }
        
        private void OnEnable() {
            Debug.Log("Enter land state");
            _verticalLandingSpeed = AnimatorController.NextStateParameterX;
            
            // hard landing (can exit this state as soon as possible)
            if (_verticalLandingSpeed < verticalSpeedThreshold) {
                _canExit = true;
            }
            // soft landing (can't transition out of this state until the playback completes)
            else {
                _canExit = false;
                clipTransition.Events.OnEnd = () => _canExit = true;
            }
            
            AnimatorController.Animancer.Play(clipTransition);
        }

        private void OnDisable() {
            Debug.Log("Exit land state");
        }
    }
}
