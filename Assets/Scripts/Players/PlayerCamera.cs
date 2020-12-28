using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utilities.MathUtils;
using NaughtyAttributes;

namespace Players {
    
    public class PlayerCamera : MonoBehaviour {
        
        [SerializeField] private PlayerController playerController;
        
        [HorizontalLine(color: EColor.Red)]
        [Header("Follow Target")]
        [SerializeField] private Transform followTarget;

        [HorizontalLine(color: EColor.Pink)]
        [Header("Distance")]
        [SerializeField] private float defaultFollowDistance = 6f;
        [SerializeField] private float minFollowDistance = 3f;
        [SerializeField] private float maxFollowDistance = 15f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float zoomSharpness = 10f;
        [SerializeField] private float followSharpness = 10000f;

        [HorizontalLine(color: EColor.Violet)]
        [Header("Rotation")]
        [SerializeField] private bool invertX = false;
        [SerializeField] private bool invertY = false;
        [SerializeField, Range(-90f, 90f)] public float defaultVerticalAngle = 20f;
        [SerializeField, Range(-90f, 90f)] public float minVerticalAngle = -40f;
        [SerializeField, Range(-90f, 90f)] public float maxVerticalAngle = 90f;
        [SerializeField] private float rotationSpeed = 1f;
        [SerializeField] private float rotationSharpness = 10000f;

        [HorizontalLine(color: EColor.Green)]
        [Header("Obstruction")]
        [SerializeField] private float obstructionCheckRadius = 0.1f;
        [SerializeField] private float obstructionSharpness = 1000f;
        [SerializeField] private LayerMask obstructionMask = -1;
        [SerializeField] private List<Collider> ignoredColliders = new List<Collider>();
        
        public Perspective CameraPerspective { get; private set; }
        private Vector3 TargetForward { get; set; }

        private Vector3 _followPosition;
        private float _followDistance;
        private float _actualDistance;
        private float _cachedDistance;
        private float _verticalAngle;
        private bool _perspectiveEnforced;

        private bool _isObstructed;
        private int _obstructionCount;
        private RaycastHit[] _obstructions = new RaycastHit[8];  // increase to 16, 32 if you may have more obstructions
        

        private void OnValidate() {
            defaultFollowDistance = Mathf.Clamp(defaultFollowDistance, minFollowDistance, maxFollowDistance);
            defaultVerticalAngle = Mathf.Clamp(defaultVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }

        private void Start() {
            CameraPerspective = Perspective.ThirdPerson;
            TargetForward = followTarget.forward;
            _followPosition = followTarget.position;
            
            _actualDistance = _followDistance = defaultFollowDistance;
            _verticalAngle = defaultVerticalAngle;
            
            AddIgnoredColliders(playerController.GetComponentsInChildren<Collider>());
        }

        public void ProcessInput(ref PlayerCameraInputs inputs, float deltaTime) {
            var rotationInput = new Vector3(inputs.MovementX, inputs.MovementY, 0f);
            
            if (invertX) {
                rotationInput.x *= -1f;
            }

            if (invertY) {
                rotationInput.y *= -1f;
            }

            // handle horizontal (planar) rotation input
            var targetUp = playerController.ControlMode == ControlMode.Climb ? Vector3.up : followTarget.up;
            var rotationFromInput = Quaternion.Euler(targetUp * (rotationInput.x * rotationSpeed));
            TargetForward = rotationFromInput * TargetForward;
            // this nested cross product operation won't change the planar direction if we are on ground
            // but if we are on a planet with self gravity field, this is required to compute the correct direction
            TargetForward = Vector3.Cross(targetUp, Vector3.Cross(TargetForward, targetUp));
            
            var planarRotation = Quaternion.LookRotation(TargetForward, targetUp);

            // handle vertical rotation input
            _verticalAngle -= rotationInput.y * rotationSpeed;
            if (CameraPerspective == Perspective.ThirdPerson) {
                _verticalAngle = Mathf.Clamp(_verticalAngle, minVerticalAngle, maxVerticalAngle);
                if (playerController.ControlMode == ControlMode.Free || playerController.ControlMode == ControlMode.Swim) {
                    _verticalAngle = Mathf.Clamp(_verticalAngle, -45f, 45f);
                }
            }
            else {
                _verticalAngle = Mathf.Clamp(_verticalAngle, -90f, 90f);
            }
            
            var verticalRotation = Quaternion.Euler(_verticalAngle, 0, 0);
            
            // combine planar and vertical rotations and apply
            var targetRotation = planarRotation * verticalRotation;
            targetRotation = Quaternion.Slerp(transform.rotation, targetRotation, EaseFactor(rotationSharpness, deltaTime));
            transform.rotation = targetRotation;
            
            // disable perspective switches in auto and climb mode
            if (playerController.ControlMode == ControlMode.Auto || playerController.ControlMode == ControlMode.Climb) {
                // enforce third-person perspective when player is in auto or climb mode
                if (CameraPerspective == Perspective.FirstPerson) {
                    SwitchToView(Perspective.ThirdPerson);
                    _perspectiveEnforced = true;
                }
            }
            else {
                // switch back to first-person perspective if third-person is previously enforced
                if (_perspectiveEnforced) {
                    SwitchToView(Perspective.FirstPerson);
                    _perspectiveEnforced = false;
                }
                // switch between third-person and first-person perspective on mouse click
                if (inputs.SwitchView) {
                    SwitchToView(CameraPerspective == Perspective.ThirdPerson ? Perspective.FirstPerson : Perspective.ThirdPerson);
                }
            }

            if (CameraPerspective == Perspective.FirstPerson) {
                _followPosition = Vector3.Lerp(_followPosition, followTarget.position, EaseFactor(followSharpness, deltaTime));
                _actualDistance = Mathf.Lerp(_actualDistance, _followDistance, EaseFactor(zoomSharpness, deltaTime));
                
                transform.position = _followPosition - targetRotation * Vector3.forward * _actualDistance;
            }
            
            else {
                if (Mathf.Abs(inputs.ZoomInput) > 0f) {
                    // disable zoom if camera is moved forward due to obstructions
                    var zoomInput = _isObstructed ? 0f : inputs.ZoomInput;
                    _followDistance += zoomInput * zoomSpeed;
                    _followDistance = Mathf.Clamp(_followDistance, minFollowDistance, maxFollowDistance);
                }

                _followPosition = Vector3.Lerp(_followPosition, followTarget.position, EaseFactor(followSharpness, deltaTime));

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

        public void AddIgnoredColliders(IEnumerable<Collider> colliders) {
            ignoredColliders.Clear();
            ignoredColliders.AddRange(colliders);
        }

        private void SwitchToView(Perspective toPerspective) {
            CameraPerspective = toPerspective;
            if (toPerspective == Perspective.FirstPerson) {
                _cachedDistance = _followDistance;
                _followDistance = 0f;
            }
            else {
                _followDistance = _cachedDistance;
            }
        }
    }
}