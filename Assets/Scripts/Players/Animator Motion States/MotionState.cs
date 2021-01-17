using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Animancer.FSM;

namespace Players {
    
    /// <summary>
    /// Combat states (attack, flinch, die) have the highest priority, they take precedence over the swim state
    /// (enforced if the player is under water), which has medium priority. The swim state in turn takes precedence
    /// over other basic motion states, which have the lowest priority.
    /// </summary>
    public enum StatePriority { Low, Medium, High }
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public abstract class MotionState : StateBehaviour {

        protected PlayerAnimatorController AnimatorController { get; private set; }
        
        public virtual StatePriority Priority => StatePriority.Low;

        private void Awake() {
            AnimatorController = GetComponentInParent<PlayerAnimatorController>();
            OnAwake();
        }

        protected virtual void OnAwake() { }
    }
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static class MotionStateExtensions {
        
        /// <summary>
        /// Simple extension method to check if a collection contains the current motion state.
        /// </summary>
        public static bool In<T>(this T state, IEnumerable<T> motionStates) where T : MotionState {
            return motionStates.Contains(state);
        }

    }
}
