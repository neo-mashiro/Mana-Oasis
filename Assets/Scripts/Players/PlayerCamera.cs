using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utilities.MathUtils;

namespace Players {
    
    public class PlayerCamera : MonoBehaviour {

        [Header("Distance")]
        [SerializeField] private float defaultFollowDistance = 6f;
        [SerializeField] private float minFollowDistance = 3f;
        [SerializeField] private float maxFollowDistance = 15f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float zoomSharpness = 10f;
        [SerializeField] private float followSharpness = 10000f;

        [Header("Rotation")]
        [SerializeField] private bool invertX = false;
        [SerializeField] private bool invertY = false;
        [SerializeField, Range(-90f, 90f)] public float defaultVerticalAngle = 20f;
        [SerializeField, Range(-90f, 90f)] public float minVerticalAngle = -40f;
        [SerializeField, Range(-90f, 90f)] public float maxVerticalAngle = 90f;
        [SerializeField] private float rotationSpeed = 1f;
        [SerializeField] private float rotationSharpness = 10000f;

        [Header("Obstruction")]
        [SerializeField] private float obstructionCheckRadius = 0.1f;
        [SerializeField] private float obstructionSharpness = 1000f;
        [SerializeField] private LayerMask obstructionMask = -1;
        [SerializeField] private List<Collider> ignoredColliders = new List<Collider>();

        private Vector3 TargetForward { get; set; }
        public Perspective CameraPerspective { get; private set; }

        private Transform _followTarget;
        private Vector3 _followPosition;
        
        private float _followDistance;
        private float _actualDistance;
        private float _cachedDistance;
        private float _verticalAngle;

        private bool _isObstructed;
        private int _obstructionCount;
        private RaycastHit[] _obstructions = new RaycastHit[8];  // increase to 16, 32 if you may have more obstructions
        

        private void OnValidate() {
            defaultFollowDistance = Mathf.Clamp(defaultFollowDistance, minFollowDistance, maxFollowDistance);
            defaultVerticalAngle = Mathf.Clamp(defaultVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }

        private void Awake() {
            CameraPerspective = Perspective.ThirdPerson;
            _actualDistance = _followDistance = defaultFollowDistance;
            _verticalAngle = defaultVerticalAngle;
        }

        public void ProcessInput(Vector3 rotationInput, float zoomInput, bool clickInput, float deltaTime) {
            if (invertX) {
                rotationInput.x *= -1f;
            }

            if (invertY) {
                rotationInput.y *= -1f;
            }

            // handle horizontal (planar) rotation input
            var targetUp = _followTarget.up;
            var rotationFromInput = Quaternion.Euler(targetUp * (rotationInput.x * rotationSpeed));
            TargetForward = rotationFromInput * TargetForward;
            
            // this nested cross product operation won't change the planar direction if we are on ground
            // but if we are on a planet with self gravity field, this is required to compute the correct direction
            TargetForward = Vector3.Cross(targetUp, Vector3.Cross(TargetForward, targetUp));
            var planarRotation = Quaternion.LookRotation(TargetForward, targetUp);

            // handle vertical rotation input
            _verticalAngle -= rotationInput.y * rotationSpeed;
            _verticalAngle = CameraPerspective == Perspective.FirstPerson
                ? Mathf.Clamp(_verticalAngle, -90f, 90f)
                : Mathf.Clamp(_verticalAngle, minVerticalAngle, maxVerticalAngle);
            
            var verticalRotation = Quaternion.Euler(_verticalAngle, 0, 0);
            
            // combine planar and vertical rotations and apply
            var targetRotation = Quaternion.Slerp(transform.rotation, planarRotation * verticalRotation, EaseFactor(rotationSharpness, deltaTime));
            transform.rotation = targetRotation;

            // switch between third-person and first-person perspective
            if (clickInput) {
                if (CameraPerspective == Perspective.ThirdPerson) {
                    _cachedDistance = _followDistance;
                    _followDistance = 0f;
                    CameraPerspective = Perspective.FirstPerson;
                }
                else {
                    _followDistance = _cachedDistance;
                    CameraPerspective = Perspective.ThirdPerson;
                }
            }

            if (CameraPerspective == Perspective.FirstPerson) {
                _followPosition = Vector3.Lerp(_followPosition, _followTarget.position, EaseFactor(followSharpness, deltaTime));
                _actualDistance = Mathf.Lerp(_actualDistance, _followDistance, EaseFactor(zoomSharpness, deltaTime));
                
                transform.position = _followPosition - targetRotation * Vector3.forward * _actualDistance;
            }
            
            else {
                if (Mathf.Abs(zoomInput) > 0f) {
                    zoomInput = _isObstructed ? 0f : zoomInput;  // disable zoom if camera is moved forward due to obstructions
                    _followDistance += zoomInput * zoomSpeed;
                    _followDistance = Mathf.Clamp(_followDistance, minFollowDistance, maxFollowDistance);
                }

                _followPosition = Vector3.Lerp(_followPosition, _followTarget.position, EaseFactor(followSharpness, deltaTime));

                // check obstructions
                _obstructionCount = Physics.SphereCastNonAlloc(_followPosition, obstructionCheckRadius,
                    -transform.forward, _obstructions, _followDistance, obstructionMask, QueryTriggerInteraction.Ignore);
                
                var closestHit = new RaycastHit { distance = Mathf.Infinity };
                    
                for (var i = 0; i < _obstructionCount; i++) {
                    var isValid = _obstructions[i].distance > 0;
                    var isIgnored = ignoredColliders.Any(col => col == _obstructions[i].collider);

                    if (isValid && !isIgnored && _obstructions[i].distance < closestHit.distance) {
                        closestHit = _obstructions[i];
                    }
                }

                // obstructions detected
                if (closestHit.distance < Mathf.Infinity) {
                    _isObstructed = true;
                    _actualDistance = Mathf.Lerp(_actualDistance, closestHit.distance, EaseFactor(obstructionSharpness, deltaTime));
                }
                // no obstructions
                else {
                    _isObstructed = false;
                    _actualDistance = Mathf.Lerp(_actualDistance, _followDistance, EaseFactor(zoomSharpness, deltaTime));
                }

                // find the smoothed camera position and apply
                var targetPosition = _followPosition - targetRotation * Vector3.forward * _actualDistance;
                transform.position = targetPosition;
            }
        }
        
        public void SetFollowTarget(Transform target) {
            _followTarget = target;
            _followPosition = target.position;
            TargetForward = _followTarget.forward;
        }
        
        public void AddIgnoredColliders(IEnumerable<Collider> colliders) {
            ignoredColliders.Clear();
            ignoredColliders.AddRange(colliders);
        }
    }
}