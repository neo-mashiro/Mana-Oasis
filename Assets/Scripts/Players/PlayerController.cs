using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using static Utilities.MathUtils;
using Managers;
using KinematicCharacterController;
using NaughtyAttributes;

namespace Players {
    
    public class PlayerController : MonoBehaviour, ICharacterController {
        
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private PlayerAnimatorController animatorController;
        [SerializeField] private KinematicCharacterMotor motor;

        [HorizontalLine(3, EColor.Red)]
        [Header("Ground Movement")]
        [SerializeField] private float maxWalkSpeed = 1.5f;
        [SerializeField] private float maxRunSpeed = 4f;
        [SerializeField] private float maxSprintSpeed = 7f;
        [SerializeField] private float movementSharpness = 10f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private Vector3 gravity = new Vector3(0, -30f, 0);

        [HorizontalLine(3, EColor.Blue)]
        [Header("Air Movement (in default mode)")]
        [SerializeField] private float maxAirMoveSpeed = 4f;
        [SerializeField] private float airAccelerationSpeed = 5f;
        [SerializeField] private float airDrag = 0.1f;

        [HorizontalLine(3, EColor.Yellow)]
        [Header("Jump")]
        [SerializeField] private bool allowJumpingWhenSliding = false;
        [SerializeField] private bool allowDoubleJump = false;
        [SerializeField] private bool allowWallJump = false;
        [SerializeField] private float jumpUpSpeed = 10f;
        [SerializeField] private float jumpScalableForwardSpeed = 0f;
        [SerializeField] private float jumpPreGroundingGraceTime = 0.1f;
        [SerializeField] private float jumpPostGroundingGraceTime = 0.1f;
        
        [HorizontalLine(3, EColor.Green)]
        [Header("Auto Mode")]
        [SerializeField] private float autoMoveSpeed = 15f;
        
        [HorizontalLine(3, EColor.Orange)]
        [Header("Air Mode")]
        [SerializeField] private float airMoveSpeed = 10f;
        [SerializeField] private float airSharpness = 15;
        [SerializeField] private float boostAirMoveSpeed = 15f;
        [SerializeField] private float maxAltitude = 200;
        
        [HorizontalLine(3, EColor.Indigo)]
        [Header("Swim Mode")]
        [SerializeField] private Transform swimReferencePoint;
        [SerializeField] private float swimSpeed = 3f;
        [SerializeField] private float swimSharpness = 3;
        [SerializeField] private float gravityUnderWater = -0.02f;
        
        [HorizontalLine(3, EColor.Pink)]
        [Header("Climb Mode")]
        [SerializeField] private float climbSpeed = 4f;
        [SerializeField] private float anchoringDuration = 0.5f;
        [SerializeField] private LayerMask ladderLayer;

        [HorizontalLine(3, EColor.Violet)]
        [Header("Obstruction and Orientation")]
        [SerializeField, ReorderableList] private List<Collider> ignoredColliders = new List<Collider>();
        [SerializeField] private OrientationMode orientationMode = OrientationMode.TowardsGravity;
        [SerializeField] private float orientationSharpness = 20f;

        // public properties
        public KinematicCharacterMotor Motor => motor;
        public ControlMode ControlMode { get; private set; }
        public MotionStateInfo MotionStateInfo { get; private set; }
        public float FlyStartingTime { get; private set; }
        
        public Vector3 Gravity {
            get => gravity;
            set => gravity = value;
        }

        // private properties
        private Ladder ActiveLadder { get; set; }
        
        private ClimbMode ClimbMode {
            get => _internalClimbMode;
            set {
                _internalClimbMode = value;
                _anchoringTimer = 0f;
                _anchoringStartPosition = motor.TransientPosition;
                _anchoringStartRotation = motor.TransientRotation;
            }
        }

        // position and rotation input vector
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        
        // universal velocity
        private Vector3 _currentVelocity, _lastVelocity;
        private Vector3 _extraVelocity = Vector3.zero;
        
        // ground move
        private bool _running = false;
        private bool _sprinting = false;

        // obstructions
        private readonly Collider[] _probedColliders = new Collider[8];
        private readonly RaycastHit[] _probedHits = new RaycastHit[8];

        // jump, crouch
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private bool _doubleJumpConsumed = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private bool _canWallJump = false;
        private Vector3 _wallJumpNormal;
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;

        // fly, swim
        private bool _jumpInputIsHeld = false;
        private bool _crouchInputIsHeld = false;
        private bool _boostMode = false;
        private float _submergence;
        private Collider _waterZone;
        
        // climb
        private float _ladderUpDownInput;
        private ClimbMode _internalClimbMode;

        private Vector3 _ladderTargetPosition;
        private Quaternion _ladderTargetRotation;
        private float _deviation = 0;
        private float _anchoringTimer = 0f;
        private Vector3 _anchoringStartPosition = Vector3.zero;
        private Quaternion _anchoringStartRotation = Quaternion.identity;

        // cached layers
        private LayerMask _groundLayer;
        private LayerMask _waterLayer;
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private void Awake() {
            motor.CharacterController = this;
            TransitionToMode(ControlMode.Default);
        }

        private void Start() {
            _groundLayer = LayerMask.GetMask("Ground");
            _waterLayer = LayerMask.GetMask("Water");
            
            AddIgnoredColliders(GetComponentsInChildren<Collider>());

            MotionStateInfo = new MotionStateInfo {
                State = animatorController.Idle, ParameterX = 0f, ParameterY = 0f
            };
        }

        private void TransitionToMode(ControlMode newMode) {
            var oldMode = ControlMode;
            if (oldMode == newMode) {
                return;
            }
            
            OnModeExit(oldMode, newMode);
            ControlMode = newMode;
            OnModeEnter(newMode, oldMode);
        }

        private void OnModeEnter(ControlMode mode, ControlMode fromMode) {
            switch (mode) {
                case ControlMode.Default:
                    break;

                case ControlMode.Air:
                    motor.SetCapsuleCollisionsActivation(true);          // detect collisions or not?
                    motor.SetMovementCollisionsSolvingActivation(true);  // solve collision or not?
                    motor.SetGroundSolvingActivation(false);
                    FlyStartingTime = Time.time;
                    break;
                
                case ControlMode.Swim:
                    motor.SetCapsuleCollisionsActivation(true);
                    motor.SetMovementCollisionsSolvingActivation(true);
                    motor.SetGroundSolvingActivation(false);
                    break;
                
                case ControlMode.Climb:
                    motor.SetMovementCollisionsSolvingActivation(false);
                    motor.SetGroundSolvingActivation(false);
                    ClimbMode = ClimbMode.Anchor;

                    // find the point on the ladder to snap to
                    _ladderTargetPosition = ActiveLadder.ClosestPointOnLadder(motor.TransientPosition, out _deviation);
                    _ladderTargetRotation = ActiveLadder.transform.rotation;
                    break;
            }
        }

        private void OnModeExit(ControlMode mode, ControlMode toMode) {
            switch (mode) {
                case ControlMode.Default:
                    break;
                
                case ControlMode.Air:
                    motor.SetGroundSolvingActivation(true);
                    FlyStartingTime = 0;
                    break;
                
                case ControlMode.Swim:
                    motor.SetCapsuleCollisionsActivation(true);
                    motor.SetMovementCollisionsSolvingActivation(true);
                    motor.SetGroundSolvingActivation(true);
                    _submergence = 0f;
                    break;
                
                case ControlMode.Climb:
                    motor.SetMovementCollisionsSolvingActivation(true);
                    motor.SetGroundSolvingActivation(true);
                    break;
            }
        }

        public void ProcessInput(ref PlayerCharacterInputs inputs) {
            // check air toggle input and apply mode transitions
            if (inputs.AirModeToggled && !_waterZone && !_isCrouching) {
                TransitionToMode(ControlMode == ControlMode.Air ? ControlMode.Default : ControlMode.Air);
            }

            // check climb toggle input and apply mode transitions
            if (inputs.ClimbModeToggled && !_isCrouching && ControlMode != ControlMode.Auto && ControlMode != ControlMode.Swim) {
                var hits = motor.CharacterOverlap(motor.TransientPosition, motor.TransientRotation,
                    _probedColliders, ladderLayer, QueryTriggerInteraction.Collide);
                
                if (hits > 0) {
                    Ladder ladder = null;
                    
                    // the target box trigger collider is often the outermost one that encloses the entire ladder,
                    // it will be the last one we hit when doing a capsule overlap cast, so we loop in reverse order
                    for (var i = hits - 1; i >= 0; i--) {
                        if (!ReferenceEquals(_probedColliders[i], null)) {
                            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                            ladder = _probedColliders[i].gameObject.GetComponent<Ladder>();
                            if (ladder) {
                                break;
                            }
                        }
                    }
                    
                    if (ladder) {
                        // player can only climb ladders in default or air mode
                        if (ControlMode == ControlMode.Default || ControlMode == ControlMode.Air) {
                            // if player is close enough to the top release point, transition to climb mode directly
                            if (motor.TransientPosition.y - ladder.TopReleasePoint.position.y >= -0.1f) {
                                ActiveLadder = ladder;
                                TransitionToMode(ControlMode.Climb);
                            }
                            // otherwise, player can only climb onto the ladder from the front side, not the back side
                            else {
                                var directionFromLadder = motor.TransientPosition - ladder.transform.position;
                                if (Vector3.Dot(directionFromLadder.normalized, ladder.transform.forward) < 0.01f) {
                                    ActiveLadder = ladder;
                                    TransitionToMode(ControlMode.Climb);
                                }
                            }
                        }
                        else if (ControlMode == ControlMode.Climb) {
                            ClimbMode = ClimbMode.DeAnchor;
                            _ladderTargetPosition = motor.TransientPosition;
                            _ladderTargetRotation = motor.TransientRotation;
                            TransitionToMode(ControlMode.Default);
                        }
                    }
                }
            }

            // essential input
            var moveVector = Vector3.ClampMagnitude(new Vector3(inputs.MovementX, 0f, inputs.MovementZ), 1f);
            var cameraPlanarDirection = Vector3.ProjectOnPlane(playerCamera.transform.rotation * Vector3.forward, motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f) {
                cameraPlanarDirection = Vector3.ProjectOnPlane(playerCamera.transform.rotation * Vector3.up, motor.CharacterUp).normalized;
            }
            var cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, motor.CharacterUp);

            switch (ControlMode) {
                case ControlMode.Default:
                    _moveInputVector = cameraPlanarRotation * moveVector;

                    // C# 8.0 switch expression feature not yet supported in Unity
                    // _lookInputVector = playerCamera.CameraPerspective switch {
                    //     Perspective.FirstPerson => cameraPlanarDirection,
                    //     Perspective.ThirdPerson => _moveInputVector.normalized,
                    //     _ => _lookInputVector
                    // };

                    // in first person view, player rotates towards camera (mouse rotation)
                    if (playerCamera.CameraPerspective == Perspective.FirstPerson) {
                        _lookInputVector = cameraPlanarDirection;
                    }
                    // in third person view, player rotates towards movement
                    else if (playerCamera.CameraPerspective == Perspective.ThirdPerson) {
                        _lookInputVector = _moveInputVector.normalized;
                    }

                    if (inputs.JumpDown) {
                        _jumpRequested = true;
                        _timeSinceJumpRequested = 0f;
                    }
                    
                    if (inputs.CrouchDown && motor.GroundingStatus.IsStableOnGround) {
                        // don't crouch until the player is fully stable on ground since the last frame
                        if (motor.LastGroundingStatus.IsStableOnGround) {
                            _shouldBeCrouching = true;
                            if (!_isCrouching) {
                                _isCrouching = true;
                                StartCoroutine(CrouchCapsuleCollider(2f, 1.2f, 3.5f));
                            }
                        }
                    }
                    // un-crouching is handled in AfterCharacterUpdate() where we can check if there's enough space to stand up
                    else if (inputs.CrouchUp) {
                        _shouldBeCrouching = false;
                    }

                    _running = !_isCrouching && inputs.ShiftHeld;
                    _sprinting = !_isCrouching && inputs.AltHeld;

                    break;

                case ControlMode.Air:
                    _moveInputVector = playerCamera.transform.rotation * moveVector;
                    
                    // C# 8.0 feature not yet supported by Unity
                    // _lookInputVector = playerCamera.CameraPerspective switch {
                    //     Perspective.FirstPerson => cameraPlanarDirection,
                    //     Perspective.ThirdPerson => _moveInputVector.normalized,
                    //     _ => _lookInputVector
                    // };
                    switch (playerCamera.CameraPerspective) {
                        case Perspective.FirstPerson:
                            _lookInputVector = cameraPlanarDirection;
                            break;
                        case Perspective.ThirdPerson:
                            _lookInputVector = _moveInputVector.normalized;
                            break;
                    }
                    
                    _jumpInputIsHeld = inputs.JumpHeld;
                    _crouchInputIsHeld = inputs.CrouchHeld;
                    _boostMode = inputs.ShiftHeld;
                    break;

                case ControlMode.Swim:
                    _moveInputVector = playerCamera.transform.rotation * moveVector;
                    
                    // C# 8.0 feature not yet supported by Unity
                    // _lookInputVector = playerCamera.CameraPerspective switch {
                    //     Perspective.FirstPerson => cameraPlanarDirection,
                    //     Perspective.ThirdPerson => _moveInputVector.normalized,
                    //     _ => _lookInputVector
                    // };
                    switch (playerCamera.CameraPerspective) {
                        case Perspective.FirstPerson:
                            _lookInputVector = cameraPlanarDirection;
                            break;
                        case Perspective.ThirdPerson:
                            _lookInputVector = _moveInputVector.normalized;
                            break;
                    }
                    
                    _jumpRequested = inputs.JumpHeld;
                    _jumpInputIsHeld = inputs.JumpHeld;
                    _crouchInputIsHeld = inputs.CrouchHeld;
                    break;

                case ControlMode.Climb:
                    _ladderUpDownInput = inputs.MovementZ;
                    break;
                
                case ControlMode.Auto:
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) {
            _lastVelocity = _currentVelocity;
            
            // check if the player is underwater
            var waterHits = motor.CharacterOverlap(motor.TransientPosition, motor.TransientRotation,
                _probedColliders, _waterLayer, QueryTriggerInteraction.Collide);
            
            if (waterHits > 0 && !ReferenceEquals(_probedColliders[0], null)) {
                var referencePosition = swimReferencePoint.position;
                var hitPoint = Physics.ClosestPoint(
                    referencePosition, _probedColliders[0],
                    _probedColliders[0].transform.position,
                    _probedColliders[0].transform.rotation);

                var verticalDistance = Vector3.Dot(referencePosition - hitPoint, motor.CharacterUp);
                var swimReferenceHeight = Vector3.Dot(referencePosition - transform.position, motor.CharacterUp);
                _submergence = 1 - Mathf.Clamp01(verticalDistance / swimReferenceHeight);

                // transition to swim mode if the reference point is almost underwater
                if (_submergence > 0.9f) {
                    if (ControlMode != ControlMode.Swim && ControlMode != ControlMode.Auto) {
                        TransitionToMode(ControlMode.Swim);
                        _waterZone = _probedColliders[0];
                    }
                }
                // transition back to default mode when the reference point moves high enough above the water surface
                else if (_submergence < 0.8f) {
                    if (ControlMode == ControlMode.Swim) {
                        TransitionToMode(ControlMode.Default);
                        _waterZone = null;
                    }
                }
                // when submergence is between 0.8 and 0.9, stay in the current mode (swim or move)
                // this gives player a chance to float up and down slightly on water without mode transitions
            }
            
            switch (ControlMode) {
                case ControlMode.Default:
                    break;
                case ControlMode.Auto:
                    break;
                case ControlMode.Air:
                    break;
                case ControlMode.Swim:
                    break;
                case ControlMode.Climb:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
            switch (ControlMode) {
                case ControlMode.Default:
                case ControlMode.Air:
                case ControlMode.Swim:
                    // normally, if gravity is straight down, rotation is only controlled by the look direction
                    if (_lookInputVector.sqrMagnitude > 0f && rotationSharpness > 0f) {
                        var smoothedLookDirection = Vector3.Slerp(motor.CharacterForward, _lookInputVector, EaseFactor(rotationSharpness, deltaTime)).normalized;
                        currentRotation = Quaternion.LookRotation(smoothedLookDirection, motor.CharacterUp);
                    }
                    
                    var currentUp = currentRotation * Vector3.up;
                    
                    // if gravity is not straight down, we need to reorient character's up towards the inverse gravity
                    if (orientationMode == OrientationMode.TowardsGravity) {
                        var smoothedUpDirection = Vector3.Slerp(currentUp, -gravity.normalized, EaseFactor(orientationSharpness, deltaTime));
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedUpDirection) * currentRotation;
                    }
                    
                    // bonus: reorient character's up towards the slope normal vector
                    else if (orientationMode == OrientationMode.TowardsGravityAndSlope) {
                        if (motor.GroundingStatus.IsStableOnGround) {
                            var characterBottomHemiCenter = motor.TransientPosition + currentUp * motor.Capsule.radius;
                            var smoothedGroundNormal = Vector3.Slerp(motor.CharacterUp, motor.GroundingStatus.GroundNormal, EaseFactor(orientationSharpness, deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                            // adjust the position to create a rotation around the bottom hemi center instead of the pivot
                            motor.SetTransientPosition(characterBottomHemiCenter + currentRotation * Vector3.down * motor.Capsule.radius);
                        }
                        else {
                            var smoothedUpDirection = Vector3.Slerp(currentUp, -gravity.normalized, EaseFactor(orientationSharpness, deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedUpDirection) * currentRotation;
                        }
                    }
                    
                    else {
                        var smoothedUpDirection = Vector3.Slerp(currentUp, Vector3.up, EaseFactor(orientationSharpness, deltaTime));
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedUpDirection) * currentRotation;
                    }
                    
                    break;

                case ControlMode.Climb:
                    if (ClimbMode == ClimbMode.Climb) {
                        currentRotation = ActiveLadder.transform.rotation;
                    }
                    else if (ClimbMode == ClimbMode.Anchor || ClimbMode == ClimbMode.DeAnchor) {
                        // rotate the character towards the ladder plane
                        currentRotation = Quaternion.Slerp(_anchoringStartRotation, _ladderTargetRotation, _anchoringTimer / anchoringDuration);
                    }
                    
                    break;

                default:
                    goto case ControlMode.Default;
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
            switch (ControlMode) {
                case ControlMode.Default: {
                    // ground movement
                    if (motor.GroundingStatus.IsStableOnGround) {
                        var currentVelocityMagnitude = currentVelocity.magnitude;
                        var effectiveGroundNormal = motor.GroundingStatus.GroundNormal;

                        if (currentVelocityMagnitude > 0f && motor.GroundingStatus.SnappingPrevented) {
                            var groundPointToCharacter = motor.TransientPosition - motor.GroundingStatus.GroundPoint;
                            effectiveGroundNormal = Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f
                                ? motor.GroundingStatus.OuterGroundNormal
                                : motor.GroundingStatus.InnerGroundNormal;
                        }

                        // project velocity onto the slope
                        currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                        var inputRight = Vector3.Cross(_moveInputVector, motor.CharacterUp);
                        var reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                        
                        // real move speed decreases as player's legs submerge more under the water (but not yet in swim mode)
                        var realSpeed = (_sprinting ? maxSprintSpeed : _running ? maxRunSpeed : maxWalkSpeed) * (1 - _submergence);
                        var targetVelocity = reorientedInput * realSpeed;
                        
                        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(movementSharpness, deltaTime));
                    }

                    // air movement
                    else {
                        if (_moveInputVector.sqrMagnitude > 0f) {
                            var addedVelocity = _moveInputVector * (airAccelerationSpeed * deltaTime);
                            var currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

                            if (currentVelocityOnInputsPlane.magnitude < maxAirMoveSpeed) {
                                var newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, maxAirMoveSpeed);
                                addedVelocity = newTotal - currentVelocityOnInputsPlane;
                            }
                            else {
                                if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f) {
                                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                                }
                            }

                            // prevent air-climbing sloped walls
                            if (motor.GroundingStatus.FoundAnyGround) {
                                if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f) {
                                    var obstructionNormal = Vector3.Cross(
                                        Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal),
                                        motor.CharacterUp).normalized;
                                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, obstructionNormal);
                                }
                            }

                            currentVelocity += addedVelocity;
                        }

                        // apply gravity and air drag
                        currentVelocity += gravity * deltaTime;
                        currentVelocity *= 1f / (1f + airDrag * deltaTime);
                    }

                    // handle jump
                    _jumpedThisFrame = false;
                    _timeSinceJumpRequested += deltaTime;
                    _jumpRequested = _jumpRequested && !_isCrouching;  // disable jump in crouch state

                    if (_jumpRequested) {
                        var isOnGround = allowJumpingWhenSliding ? motor.GroundingStatus.FoundAnyGround : motor.GroundingStatus.IsStableOnGround;

                        // double jump
                        if (_jumpConsumed) {
                            if (allowDoubleJump && !_doubleJumpConsumed && !isOnGround) {
                                motor.ForceUnground();
                                currentVelocity += motor.CharacterUp * jumpUpSpeed - Vector3.Project(currentVelocity, motor.CharacterUp);

                                _jumpRequested = false;
                                _doubleJumpConsumed = true;
                                _jumpedThisFrame = true;
                            }
                        }

                        // normal jump
                        else if (isOnGround || _timeSinceLastAbleToJump <= jumpPostGroundingGraceTime) {
                            var jumpDirection = motor.CharacterUp;

                            if (_canWallJump) {
                                jumpDirection = _wallJumpNormal;
                            }
                            else if (motor.GroundingStatus.FoundAnyGround && !motor.GroundingStatus.IsStableOnGround) {
                                jumpDirection = motor.GroundingStatus.GroundNormal;
                            }

                            // tell the motor to skip ground probing and snapping on its next update
                            // this is required whenever we want our character to leave the ground
                            motor.ForceUnground();

                            currentVelocity += jumpDirection * jumpUpSpeed - Vector3.Project(currentVelocity, motor.CharacterUp);
                            currentVelocity += _moveInputVector * jumpScalableForwardSpeed;

                            // reset jump state
                            _jumpRequested = false;
                            _jumpConsumed = true;
                            _jumpedThisFrame = true;
                        }
                    }

                    _canWallJump = false;
                    
                    // // in some states, interpolate forward speed from root motion can help smooth out movement
                    // var currentMotionState = animatorController.StateMachine.CurrentState;
                    //
                    // if (motor.GroundingStatus.IsStableOnGround && deltaTime > 0 &&
                    //     currentMotionState == animatorController.Combat) {
                    //     
                    //     var rootForwardSpeed = animatorController.RootMotionDeltaPosition.z / deltaTime;
                    //     var forwardVector = Vector3.Project(currentVelocity, motor.CharacterForward);
                    //     var planarVector = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterForward);
                    //     
                    //     currentVelocity = planarVector + forwardVector * (rootForwardSpeed / forwardVector.magnitude);
                    // }
                    //
                    // // reset animator root motion
                    // animatorController.RootMotionDeltaPosition = Vector3.zero;
                    // animatorController.RootMotionDeltaRotation = Quaternion.identity;

                    // apply extra velocity if there's any
                    if (_extraVelocity.sqrMagnitude > 0f) {
                        currentVelocity += _extraVelocity;
                        _extraVelocity = Vector3.zero;
                    }

                    break;
                }

                case ControlMode.Air: {
                    var verticalInput = 0f + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);
                    var moveSpeed = _boostMode ? boostAirMoveSpeed : airMoveSpeed;
                    var targetVelocity = _moveInputVector.normalized * moveSpeed;
                    targetVelocity += motor.CharacterUp.normalized * (verticalInput * moveSpeed * 0.5f);
                    
                    currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(airSharpness, deltaTime));
                    
                    if (_extraVelocity.sqrMagnitude > 0f) {
                        currentVelocity += new Vector3(_extraVelocity.x, Mathf.Min(_extraVelocity.y, 0), _extraVelocity.z);
                        _extraVelocity = Vector3.zero;
                    }

                    if (motor.TransientPosition.y >= maxAltitude && currentVelocity.y > 0) {
                        currentVelocity.y = 0;
                        GameManager.Instance.DisplaySystemMessage(Color.red, "ALTITUDE LIMIT", 2);
                    }
                    
                    break;
                }

                case ControlMode.Swim: {
                    var verticalInput = 0 + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);
                    var targetVelocity = _moveInputVector.normalized * swimSpeed;
                    targetVelocity += motor.CharacterUp.normalized * (verticalInput * swimSpeed * 1f);
                    var smoothedVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(swimSharpness, deltaTime));

                    // when we are floating on the water, project any positive upward velocity onto the surface
                    if (_submergence < 0.9f && smoothedVelocity.y > 0) {
                        smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, Vector3.up);
                    }
                    else {
                        smoothedVelocity.y += gravityUnderWater;
                    }

                    currentVelocity = smoothedVelocity;

                    if (_extraVelocity.sqrMagnitude > 0f) {
                        currentVelocity += new Vector3(_extraVelocity.x, Mathf.Min(_extraVelocity.y, 0), _extraVelocity.z);
                        _extraVelocity = Vector3.zero;
                    }
                    
                    break;
                }
                
                case ControlMode.Climb: {
                    currentVelocity = Vector3.zero;

                    switch (ClimbMode) {
                        case ClimbMode.Climb:
                            currentVelocity = (_ladderUpDownInput * ActiveLadder.transform.up).normalized * climbSpeed;
                            break;
                        // this is how we snap to the ladder, which is done through a simple interpolation of the
                        // character's position and rotation, in order to simplify snapping animations
                        case ClimbMode.Anchor:
                        case ClimbMode.DeAnchor:
                            var tmpPosition = Vector3.Lerp(_anchoringStartPosition, _ladderTargetPosition, (_anchoringTimer / anchoringDuration));
                            currentVelocity = motor.GetVelocityForMovePosition(motor.TransientPosition, tmpPosition, deltaTime);
                            break;
                    }
                    break;
                }
            }

            _currentVelocity = currentVelocity;
        }
        
        public void AfterCharacterUpdate(float deltaTime) {
            switch (ControlMode) {
                case ControlMode.Default: {
                    // clean up jump states
                    if (_jumpRequested && _timeSinceJumpRequested > jumpPreGroundingGraceTime) {
                        _jumpRequested = false;
                    }

                    if (allowJumpingWhenSliding ? motor.GroundingStatus.FoundAnyGround : motor.GroundingStatus.IsStableOnGround) {
                        if (!_jumpedThisFrame) {
                            _doubleJumpConsumed = false;
                            _jumpConsumed = false;
                        }
                        _timeSinceLastAbleToJump = 0f;
                    }
                    else {
                        // keep track of the time since we were last able to jump (for grace period)
                        _timeSinceLastAbleToJump += deltaTime;
                    }

                    // un-crouch if there's enough space to stand up
                    if (_isCrouching && !_shouldBeCrouching) {
                        motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                        var overlapsCount = motor.CharacterOverlap(motor.TransientPosition, motor.TransientRotation,
                            _probedColliders, motor.CollidableLayers, QueryTriggerInteraction.Ignore);
                        
                        // obstructions detected, back to crouching
                        if (overlapsCount > 0) {
                            motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                        }
                        // no obstructions, stand up
                        else {
                            _isCrouching = false;
                            motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                            StartCoroutine(CrouchCapsuleCollider(1.2f, 2f, 3.5f));
                        }
                    }
                    
                    break;
                }

                case ControlMode.Climb: {
                    // in climb state, check if we are still on the ladder
                    if (ClimbMode == ClimbMode.Climb) {
                        ActiveLadder.ClosestPointOnLadder(motor.TransientPosition, out _deviation);

                        // climb off the ladder, transition to de-anchor state
                        if (Mathf.Abs(_deviation) > 0.05f) {
                            ClimbMode = ClimbMode.DeAnchor;
                            
                            if (_deviation > 0) {
                                _ladderTargetPosition = ActiveLadder.TopReleasePoint.position;
                                _ladderTargetRotation = ActiveLadder.TopReleasePoint.rotation;
                            }
                            else if (_deviation < 0) {
                                _ladderTargetPosition = ActiveLadder.BottomReleasePoint.position;
                                _ladderTargetRotation = ActiveLadder.BottomReleasePoint.rotation;
                            }
                        }
                    }
                    // in anchor or de-anchor state, prepare to transition to either climb or default state
                    else if (ClimbMode == ClimbMode.Anchor || ClimbMode == ClimbMode.DeAnchor) {
                        if (_anchoringTimer >= anchoringDuration) {
                            if (ClimbMode == ClimbMode.Anchor) {
                                ClimbMode = ClimbMode.Climb;
                            }
                            else if (ClimbMode == ClimbMode.DeAnchor) {
                                TransitionToMode(ControlMode.Default);
                            }
                        }
                        
                        _anchoringTimer += deltaTime;
                    }

                    break;
                }
            }
            
            // determine player's expected motion state in the animator state machine
            MotionState motionState;
            var parameterX = 0f;
            var parameterY = 0f;
            
            switch (ControlMode) {
                case ControlMode.Default:
                    // possible states: idle, crouch, jump, airborne, land, move
                    if (_jumpedThisFrame) {
                        motionState = animatorController.Jump;
                    }
                    else if (_isCrouching) {
                        motionState = animatorController.Crouch;
                        parameterX = Vector3.Dot(_currentVelocity, motor.CharacterForward);  // moving speed
                    }
                    else if (!motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround) {
                        motionState = animatorController.Airborne;
                    }
                    else if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround) {
                        motionState = animatorController.Land;
                        parameterX = Vector3.Dot(_lastVelocity, -motor.CharacterUp);  // vertical landing speed
                    }
                    else {
                        if (_moveInputVector != Vector3.zero) {
                            motionState = animatorController.Move;
                            parameterX = Vector3.Dot(_currentVelocity, motor.CharacterForward);  // moving speed
                        }
                        else {
                            motionState = animatorController.Idle;
                        }
                    }
                    break;
                
                case ControlMode.Climb:
                    if (ClimbMode == ClimbMode.DeAnchor) {
                        motionState = animatorController.Idle;
                        break;
                    }
                    else {
                        motionState = animatorController.Climb;
                        parameterX = ClimbMode == ClimbMode.Climb ? Vector3.Dot(_currentVelocity, motor.CharacterUp) : 0f;
                        break;
                    }

                case ControlMode.Air:
                    motionState = animatorController.Fly;
                    parameterX = Vector3.Dot(_currentVelocity, motor.CharacterForward);  // forward moving speed
                    parameterY = Vector3.Dot(_currentVelocity, motor.CharacterUp);  // vertical moving speed
                    break;
                
                case ControlMode.Swim:
                    motionState = animatorController.Swim;
                    parameterX = Vector3.Dot(_currentVelocity, motor.CharacterForward);
                    parameterY = Vector3.Dot(_currentVelocity, motor.CharacterUp);
                    break;
                
                // case ControlMode.Auto:
                //     state = idle;
                //     break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            MotionStateInfo = new MotionStateInfo {
                State = motionState, ParameterX = parameterX, ParameterY = parameterY
            };
            
            animatorController.UpdateStateMachine();
        }

        public bool IsColliderValidForCollisions(Collider other) {
            // if a collider is ignored by the character controller, make sure that camera ignores it too (in camera settings)
            // otherwise it could be weird, for example, player is inside a collider but the camera still tries to move forward
            if (ignoredColliders.Count == 0) {
                return true;
            }

            return !ignoredColliders.Contains(other);
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
            
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
            if (ControlMode == ControlMode.Default) {
                // wall jump is only allowed if we are moving against an obstruction and not stable on ground
                if (allowWallJump && !motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable) {
                    _canWallJump = true;
                    _wallJumpNormal = hitNormal;
                }
            }
        }
        
        public void PostGroundingUpdate(float deltaTime) {
            switch (motor.GroundingStatus.IsStableOnGround) {
                // player lands on the ground
                case true when !motor.LastGroundingStatus.IsStableOnGround: {
                    break;
                }
                // player leaves the ground (not necessarily jumping, could be falling off edges, stepping on a steep slope, etc.)
                case false when motor.LastGroundingStatus.IsStableOnGround:
                    break;
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
            Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {
            
        }

        // make sure that motor's [discrete collision events] checkbox is checked, otherwise this won't be called.
        // this will be called once for every hit collider that has a rigidbody including the ground, no matter if the
        // character is moving or static, as a result, zero or more calls are possible in a single update.
        public void OnDiscreteCollisionDetected(Collider hitCollider) {
            if (_groundLayer.Contains(hitCollider.gameObject.layer)) {
                return;
            }
            
            if (hitCollider.CompareTag("Vehicle")) {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                var rb = hitCollider.GetComponent<Rigidbody>();
                AddExtraVelocity(rb.velocity * (rb.mass * 0.01f));
            }
        }
        
        public void AddIgnoredColliders(IEnumerable<Collider> colliders) {
            ignoredColliders.Clear();
            ignoredColliders.AddRange(colliders);
        }
        
        /// <summary>
        /// Adds extra velocity to the character to simulate explosion forces, hit impacts, wind zones, impulses, etc.
        /// </summary>
        public void AddExtraVelocity(Vector3 velocity) {
            if (ControlMode != ControlMode.Auto && ControlMode != ControlMode.Climb) {
                _extraVelocity = velocity;
            }
        }

        private IEnumerator CrouchCapsuleCollider(float fromHeight, float toHeight, float speed) {
            var height = fromHeight;
            
            // crouch down
            if (fromHeight > toHeight) {
                while (height > toHeight) {
                    height -= Time.deltaTime * speed;
                    height = Mathf.Max(height, toHeight - 0.001f);
                    motor.SetCapsuleDimensions(0.5f, height, height * 0.5f);
                    yield return null;
                }
            }
            // crouch up
            else {
                while (height < toHeight) {
                    height += Time.deltaTime * speed;
                    height = Mathf.Min(height, toHeight + 0.001f);
                    motor.SetCapsuleDimensions(0.5f, height, height * 0.5f);
                    yield return null;
                }
            }
        }
    }
}