using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using static Utilities.MathUtils;
using NaughtyAttributes;

namespace Players {
    
    public class PlayerCamera : MonoBehaviour {
        
        [SerializeField] private PlayerController playerController;
        
        [HorizontalLine(3, EColor.Red)]
        [Header("Target")]
        [SerializeField] private Transform followTarget;

        [HorizontalLine(3, EColor.Pink)]
        [Header("Distance")]
        [SerializeField] private float defaultFollowDistance = 6f;
        [SerializeField] private float minFollowDistance = 3f;
        [SerializeField] private float maxFollowDistance = 15f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float zoomSharpness = 10f;
        [SerializeField] private float followSharpness = 10000f;

        [HorizontalLine(3, EColor.Violet)]
        [Header("Rotation")]
        [SerializeField] private bool invertX = false;
        [SerializeField] private bool invertY = false;
        [SerializeField, Range(-90f, 90f)] public float defaultVerticalAngle = 20f;
        [SerializeField, Range(-90f, 90f)] public float minVerticalAngle = -40f;
        [SerializeField, Range(-90f, 90f)] public float maxVerticalAngle = 90f;
        [SerializeField] private float rotationSpeed = 1f;
        [SerializeField] private float rotationSharpness = 10000f;

        [HorizontalLine(3, EColor.Green)]
        [Header("Obstruction")]
        [SerializeField] private float obstructionCheckRadius = 0.1f;
        [SerializeField] private float obstructionSharpness = 1000f;
        [SerializeField] private LayerMask obstructionMask = -1;
        [SerializeField, ReorderableList] private List<Collider> ignoredColliders = new List<Collider>();
        
        [HorizontalLine(3, EColor.Orange)]
        [Header("Camera Shake")]
        [SerializeField] private Vector3 maxPositionShake = new Vector3(0.25f, 0.5f, 0f);
        [SerializeField] private Vector3 maxRotationShake = new Vector3(2, 0, 2);
        [SerializeField] private float frequency = 0.5f;
        [SerializeField] private float recoverySharpness = 2;
        [Tooltip("Camera will start to shake above this height."), Range(0, 50)]
        [SerializeField] private float minShakeAltitude = 32;
        [Tooltip("Camera will reach maximum shake at this height."), Range(128, 300)]
        [SerializeField] private float maxShakeAltitude = 160;
        
        public Perspective CameraPerspective { get; private set; }
        private Vector3 TargetForward { get; set; }

        private Vector3 _followPosition;
        private float _followDistance;
        private float _actualDistance;
        private float _cachedDistance;
        private float _verticalAngle;
        private bool _perspectiveEnforced;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        private bool _isObstructed;
        private int _obstructionCount;
        private readonly RaycastHit[] _obstructions = new RaycastHit[8];
        
        private float _seed;
        private float _deltaAltitude;
        private float _windPulse;
        private float _actualPulse;
        private Vector3 _positionNoise = Vector3.zero;
        private Vector3 _rotationNoise = Vector3.zero;


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

            _seed = Random.value;
            _deltaAltitude = maxShakeAltitude - minShakeAltitude;
            
            AddIgnoredColliders(playerController.GetComponentsInChildren<Collider>());
        }

        public void ProcessInput(ref PlayerCameraInputs inputs, float deltaTime) {
            /// --------------------------------------------------------------------------------------------------------
            /// 1. update camera's perspective
            /// --------------------------------------------------------------------------------------------------------
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
            
            /// --------------------------------------------------------------------------------------------------------
            /// 2. update camera's rotation
            /// --------------------------------------------------------------------------------------------------------
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
                if (playerController.ControlMode == ControlMode.Air || playerController.ControlMode == ControlMode.Swim) {
                    _verticalAngle = Mathf.Clamp(_verticalAngle, -45f, 45f);
                }
            }
            else {
                _verticalAngle = Mathf.Clamp(_verticalAngle, -90f, 90f);
            }
            
            var verticalRotation = Quaternion.Euler(_verticalAngle, 0, 0);
            
            // combine planar and vertical rotations and apply
            _targetRotation = planarRotation * verticalRotation;
            _targetRotation = Quaternion.Slerp(transform.rotation, _targetRotation, EaseFactor(rotationSharpness, deltaTime));
            transform.rotation = _targetRotation;

            /// --------------------------------------------------------------------------------------------------------
            /// 3. update camera's position (depends on the correct rotation)
            /// --------------------------------------------------------------------------------------------------------
            if (CameraPerspective == Perspective.FirstPerson) {
                _followPosition = Vector3.Lerp(_followPosition, followTarget.position, EaseFactor(followSharpness, deltaTime));
                _actualDistance = Mathf.Lerp(_actualDistance, _followDistance, EaseFactor(zoomSharpness, deltaTime));
                
                transform.position = _followPosition - _targetRotation * Vector3.forward * _actualDistance;
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
                _targetPosition = _followPosition - _targetRotation * Vector3.forward * _actualDistance;
                transform.position = _targetPosition;
            }
            
            /// --------------------------------------------------------------------------------------------------------
            /// 4. add camera shake for third-person perspective in air mode (simple Perlin noise)
            /// --------------------------------------------------------------------------------------------------------
            if (CameraPerspective == Perspective.ThirdPerson && playerController.ControlMode == ControlMode.Air) {
                // the degree of camera shake depends on wind pulse magnitude, which is a function of flying height
                _windPulse = Mathf.Clamp01((_targetPosition.y - minShakeAltitude) / _deltaAltitude);
                
                if (_windPulse == 0) {
                    return;
                }
                
                // instead of shaking suddenly, apply wind pulse gradually within 5 seconds since taking off
                // this is to avoid jittery camera movements when player transitions to air mode at high altitude
                var flyingTime = Time.time - playerController.FlyStartingTime;
                _actualPulse = Mathf.Clamp(flyingTime, 0, 5) * 0.2f * _windPulse;
                
                _positionNoise = new Vector3(PerlinNoise1D(0), PerlinNoise1D(1), PerlinNoise1D(2));
                _rotationNoise = new Vector3(PerlinNoise1D(3), PerlinNoise1D(4), PerlinNoise1D(5));
                
                transform.position += _targetRotation * Vector3.Scale(maxPositionShake, _positionNoise) * _actualPulse;
                transform.rotation *= Quaternion.Euler(Vector3.Scale(maxRotationShake, _rotationNoise) * _actualPulse);
            }
            else if (_actualPulse > 0.005f) {
                // smoothly recover the camera from noticeable shaking
                _actualPulse = Mathf.Lerp(_actualPulse, 0, EaseFactor(recoverySharpness, deltaTime));
                
                transform.position += _targetRotation * Vector3.Scale(maxPositionShake, _positionNoise) * _actualPulse;
                transform.rotation *= Quaternion.Euler(Vector3.Scale(maxRotationShake, _rotationNoise) * _actualPulse);
            }
        }

        public void AddIgnoredColliders(IEnumerable<Collider> colliders) {
            ignoredColliders.Clear();
            ignoredColliders.AddRange(colliders);
        }
        
        private float PerlinNoise1D(int axis) {
            return Mathf.PerlinNoise(_seed + axis, Time.time * frequency) * 2 - 1;
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