using UnityEngine;
using UnityEngine.Playables;
using System.Diagnostics.CodeAnalysis;
using KinematicCharacterController;

namespace Environment {

    /// <summary>
    /// Attach this component to any moving platform driven by animation, so that characters controlled by KCC motor
    /// can stand on it and move with it properly.
    /// </summary>
    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    [RequireComponent(typeof(PhysicsMover))]
    public class MovingPlatform : MonoBehaviour, IMoverController {

        [SerializeField] private PhysicsMover mover;
        [SerializeField] private PlayableDirector director;

        private Transform _transform;
        
        public PhysicsMover Mover => mover;

        private void Start() {
            mover.MoverController = this;
            _transform = transform;
        }
        
        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime) {
            // remember pose before animation
            var initialPosition = _transform.position;
            var initialRotation = _transform.rotation;

            // evaluate animation to update transform's position and rotation
            director.time = Time.time % director.duration;
            director.Evaluate();

            // set platform's target pose to the values updated by animation
            // then the KCC physics mover will take care of the rest
            goalPosition = _transform.position;
            goalRotation = _transform.rotation;

            // in order for KCC characters to move with the platform, the real movement must be handled by the physics
            // mover instead of animation, so to separate them, we reset transform's pose to the initial values
            _transform.position = initialPosition;
            _transform.rotation = initialRotation;
        }
    }
}