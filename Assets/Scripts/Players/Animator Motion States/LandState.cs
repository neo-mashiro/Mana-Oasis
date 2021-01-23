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
                
                if (nextState == AnimatorController.Idle || nextState == AnimatorController.Fly) {
                    return _canExit;
                }
                
                // skip soft landing if the player starts moving as soon as hitting the ground
                return nextState == AnimatorController.Move;
            }
        }
        
        private void OnEnable() {
            Debug.Log("Enter land state");
            _verticalLandingSpeed = AnimatorController.ParameterX;
            
            // hard landing (can exit this state at any time)
            if (_verticalLandingSpeed < verticalSpeedThreshold) {
                _canExit = true;
            }
            // soft landing (can't transition out of this state until the playback completes)
            else {
                _canExit = false;
                // here a regular event is safer than the end event because the state might exit early
                clipTransition.Events.Clear();
                clipTransition.Events.Add(1.0f, () => _canExit = true);
            }
            
            AnimatorController.Animancer.Play(clipTransition);
        }

        private void OnDisable() {
            Debug.Log("Exit land state");
        }
    }
}
