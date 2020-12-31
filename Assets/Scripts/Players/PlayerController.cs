using System;
using System.Collections.Generic;
using UnityEngine;
using static Utilities.MathUtils;
using KinematicCharacterController;
using Managers;
using NaughtyAttributes;

namespace Players {
    
    public class PlayerController : MonoBehaviour, ICharacterController {
        
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private KinematicCharacterMotor motor;

        [HorizontalLine(3, EColor.Red)]
        [Header("Ground Movement")]
        [SerializeField] private float maxStableMoveSpeed = 5f;
        [SerializeField] private float stableMovementSharpness = 10f;
        [SerializeField] private float rotationSharpness = 20f;
        [SerializeField] private float boostMoveSpeed = 15f;
        [SerializeField] private Vector3 gravity = new Vector3(0, -30f, 0);

        [HorizontalLine(3, EColor.Blue)]
        [Header("Air Movement (in default mode)")]
        [SerializeField] private float maxAirMoveSpeed = 10f;
        [SerializeField] private float airAccelerationSpeed = 10f;
        [SerializeField] private float airDrag = 0.1f;

        [HorizontalLine(3, EColor.Black)]
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
        [SerializeField] private float airMoveSpeed = 15f;
        [SerializeField] private float airSharpness = 15;
        [SerializeField] private float boostAirMoveSpeed = 20f;
        [SerializeField] private float maxAltitude = 400;
        
        [HorizontalLine(3, EColor.Indigo)]
        [Header("Swim Mode")]
        [SerializeField] private Transform swimmingReferencePoint;
        [SerializeField] private LayerMask waterLayer;
        [SerializeField] private float swimmingSpeed = 4f;
        [SerializeField] private float swimmingMovementSharpness = 3;
        [SerializeField] private float swimmingOrientationSharpness = 2f;
        [SerializeField] private float gravityUnderWater = -0.02f;
        
        [HorizontalLine(3, EColor.Pink)]
        [Header("Climb Mode")]
        [SerializeField] private float climbingSpeed = 4f;
        [SerializeField] private float anchoringDuration = 0.5f;
        [SerializeField] private LayerMask ladderLayer;

        [HorizontalLine(3, EColor.Violet)]
        [Header("Obstruction and Orientation")]
        [SerializeField, ReorderableList] private List<Collider> ignoredColliders = new List<Collider>();
        [SerializeField] private OrientationMode orientationMode = OrientationMode.TowardsGravity;
        [SerializeField] private float orientationSharpness = 20f;
        
        [HorizontalLine(3, EColor.Yellow)]
        [Header("Mesh and Costumes")]
        [SerializeField] private Transform meshRoot;
        [SerializeField] private Mesh costumeMesh;

        [HorizontalLine(3, EColor.White)]
        [Header("Audio Clips")]
        [SerializeField] private AudioClip footstepSound;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip landSound;
        [SerializeField] private AudioClip flySound;
        [SerializeField] private AudioClip swimSound;

        // public properties
        public KinematicCharacterMotor Motor => motor;
        
        public Vector3 Gravity {
            get => gravity;
            set => gravity = value;
        }
        
        public Transform MeshRoot {
            get => meshRoot;
            set => meshRoot = value;
        }

        // private properties
        public ControlMode ControlMode { get; private set; }

        public float FlyStartingTime { get; private set; }

        private Ladder ActiveLadder { get; set; }
        
        private ClimbState ClimbState {
            get => _internalClimbState;
            set {
                _internalClimbState = value;
                _anchoringTimer = 0f;
                _anchoringStartPosition = motor.TransientPosition;
                _anchoringStartRotation = motor.TransientRotation;
            }
        }

        // attached components
        private AudioSource _audioSource;
        private PlayerStatus _playerStatus;
        
        // position and rotation inputs
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        
        // obstructions
        private readonly Collider[] _probedColliders = new Collider[8];
        private readonly RaycastHit[] _probedHits = new RaycastHit[8];

        // for default mode
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private bool _doubleJumpConsumed = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;
        private bool _canWallJump = false;
        private Vector3 _wallJumpNormal;
        private bool _bounceOffGround = false;
        private bool _boostInputIsHeld = false;
        
        // for air and swim mode
        private bool _jumpInputIsHeld = false;
        private bool _crouchInputIsHeld = false;
        private Collider _waterZone;
        
        // for climb mode
        private float _ladderUpDownInput;
        private ClimbState _internalClimbState;

        private Vector3 _ladderTargetPosition;
        private Quaternion _ladderTargetRotation;
        private float _deviation = 0;
        private float _anchoringTimer = 0f;
        private Vector3 _anchoringStartPosition = Vector3.zero;
        private Quaternion _anchoringStartRotation = Quaternion.identity;

        // velocity from extra force
        private Vector3 _extraVelocity = Vector3.zero;

        // sound effects
        private AudioClip _walkSound;
        private AudioClip _dashSound;
        private AudioClip _jumpSound;
        private AudioClip _landSound;
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private void Awake() {
            motor.CharacterController = this;
            TransitionToMode(ControlMode.Default);
        }

        private void Start() {
            _audioSource = GetComponent<AudioSource>();
            _playerStatus = GetComponent<PlayerStatus>();
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
                    _walkSound = null;  // depend on ground tags
                    _dashSound = null;  // depend on ground tags
                    _jumpSound = jumpSound;
                    _landSound = null;  // depend on ground tags
                    break;

                case ControlMode.Air:
                    motor.SetCapsuleCollisionsActivation(true);          // detect collisions or not?
                    motor.SetMovementCollisionsSolvingActivation(true);  // solve collision or not?
                    motor.SetGroundSolvingActivation(false);
                    _walkSound = flySound;
                    _dashSound = flySound;
                    _jumpSound = flySound;
                    _landSound = null;  // depend on ground tags
                    FlyStartingTime = Time.time;
                    break;
                
                case ControlMode.Swim:
                    motor.SetCapsuleCollisionsActivation(true);
                    motor.SetMovementCollisionsSolvingActivation(true);
                    motor.SetGroundSolvingActivation(false);
                    _walkSound = swimSound;
                    _dashSound = null;
                    _jumpSound = swimSound;
                    _landSound = null;
                    break;
                
                case ControlMode.Climb:
                    motor.SetMovementCollisionsSolvingActivation(false);
                    motor.SetGroundSolvingActivation(false);
                    ClimbState = ClimbState.Anchor;

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
                    break;
                
                case ControlMode.Climb:
                    motor.SetMovementCollisionsSolvingActivation(true);
                    motor.SetGroundSolvingActivation(true);
                    break;
            }
            
            _walkSound = null;
            _dashSound = null;
            _jumpSound = null;
            _landSound = null;
        }

        public void ProcessInput(ref PlayerCharacterInputs inputs) {
            // check air toggle input and apply mode transitions
            if (inputs.AirModeToggled && !_waterZone) {
                TransitionToMode(ControlMode == ControlMode.Air ? ControlMode.Default : ControlMode.Air);
            }

            // check climb toggle input and apply mode transitions
            if (inputs.ClimbModeToggled && ControlMode != ControlMode.Auto && ControlMode != ControlMode.Swim) {
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
                            ClimbState = ClimbState.DeAnchor;
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
                    
                    if (inputs.CrouchDown) {
                        _shouldBeCrouching = true;
                        if (!_isCrouching) {
                            _isCrouching = true;
                            motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                            meshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                        }
                    }
                    // un-crouching is handled in AfterCharacterUpdate() where we can check if there's enough space to stand up
                    else if (inputs.CrouchUp) {
                        _shouldBeCrouching = false;
                    }

                    _boostInputIsHeld = inputs.ShiftHeld;

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
                    _boostInputIsHeld = inputs.ShiftHeld;
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

        public void ProcessInput(ref AICharacterInputs inputs) {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        public void BeforeCharacterUpdate(float deltaTime) {
            // check if the player is underwater
            var waterHits = motor.CharacterOverlap(motor.TransientPosition, motor.TransientRotation,
                _probedColliders, waterLayer, QueryTriggerInteraction.Collide);
            
            if (waterHits > 0 && !ReferenceEquals(_probedColliders[0], null)) {
                var hitPoint = Physics.ClosestPoint(
                    swimmingReferencePoint.position,
                    _probedColliders[0],
                    _probedColliders[0].transform.position,
                    _probedColliders[0].transform.rotation);
                
                // transition to swim mode if the swimming reference point is underwater
                if (hitPoint == swimmingReferencePoint.position) {
                    if (ControlMode != ControlMode.Swim && ControlMode != ControlMode.Auto) {
                        TransitionToMode(ControlMode.Swim);
                        _waterZone = _probedColliders[0];
                    }
                }
                // transition back to default mode when the reference point moves high enough above the water surface
                else if (swimmingReferencePoint.position.y - hitPoint.y > 0.1f) {
                    if (ControlMode == ControlMode.Swim) {
                        TransitionToMode(ControlMode.Default);
                        _waterZone = null;
                    }
                }
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
                    if (ClimbState == ClimbState.Climb) {
                        currentRotation = ActiveLadder.transform.rotation;
                    }
                    else if (ClimbState == ClimbState.Anchor || ClimbState == ClimbState.DeAnchor) {
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
                        var targetVelocity = reorientedInput * (_boostInputIsHeld ? boostMoveSpeed : maxStableMoveSpeed);
                        

                        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(stableMovementSharpness, deltaTime));
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

                    if (_jumpRequested) {
                        var isOnGround = allowJumpingWhenSliding
                            ? motor.GroundingStatus.FoundAnyGround
                            : motor.GroundingStatus.IsStableOnGround;

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

                    // apply extra velocity if any
                    if (_extraVelocity.sqrMagnitude > 0f) {
                        currentVelocity += _extraVelocity;
                        _extraVelocity = Vector3.zero;
                    }

                    break;
                }

                case ControlMode.Air: {
                    var verticalInput = 0f + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);
                    var moveSpeed = _boostInputIsHeld ? boostAirMoveSpeed : airMoveSpeed;
                    var targetVelocity = (_moveInputVector + motor.CharacterUp * verticalInput).normalized * moveSpeed;
                    currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(airSharpness, deltaTime));
                    
                    if (_extraVelocity.sqrMagnitude > 0f) {
                        currentVelocity += new Vector3(_extraVelocity.x, Mathf.Min(_extraVelocity.y, 0), _extraVelocity.z);
                        _extraVelocity = Vector3.zero;
                    }

                    if (motor.TransientPosition.y >= maxAltitude && currentVelocity.y > 0) {
                        currentVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                        GameManager.Instance.DisplaySystemMessage(Color.red, "ALTITUDE LIMIT", 2);
                    }
                    
                    break;
                }

                case ControlMode.Swim: {
                    var verticalInput = 0 + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);
                    var targetVelocity = (_moveInputVector + motor.CharacterUp * verticalInput).normalized * swimmingSpeed;
                    var smoothedVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(swimmingMovementSharpness, deltaTime));

                    // see if our swimming reference point would be out of water after the update
                    var newReferencePointPosition = swimmingReferencePoint.position + smoothedVelocity * deltaTime;
                    var closestPointOnWater = Physics.ClosestPoint(newReferencePointPosition, _waterZone,
                        _waterZone.transform.position, _waterZone.transform.rotation);

                    if ((closestPointOnWater - newReferencePointPosition).sqrMagnitude > 1e-2) {
                        // if so, project the velocity onto the water surface so that we won't fly off when being close to the surface
                        // we can set the reference point to be near the neck/shoulder, thus the player can stick her head out of water
                        var waterSurfaceNormal = (newReferencePointPosition - closestPointOnWater).normalized;
                        smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, waterSurfaceNormal);
                    }
                    
                    currentVelocity = new Vector3(smoothedVelocity.x, smoothedVelocity.y + gravityUnderWater, smoothedVelocity.z);
                    
                    if (_extraVelocity.sqrMagnitude > 0f) {
                        currentVelocity += new Vector3(_extraVelocity.x, Mathf.Min(_extraVelocity.y, 0), _extraVelocity.z);
                        _extraVelocity = Vector3.zero;
                    }
                    
                    break;
                }
                
                case ControlMode.Climb: {
                    currentVelocity = Vector3.zero;

                    switch (ClimbState) {
                        case ClimbState.Climb:
                            currentVelocity = (_ladderUpDownInput * ActiveLadder.transform.up).normalized * climbingSpeed;
                            break;
                        // this is how we snap to the ladder, which is done through a simple interpolation of the character's
                        // position and rotation, but in a real game this would normally be done with specific animations
                        case ClimbState.Anchor:
                        case ClimbState.DeAnchor:
                            var tmpPosition = Vector3.Lerp(_anchoringStartPosition, _ladderTargetPosition, (_anchoringTimer / anchoringDuration));
                            currentVelocity = motor.GetVelocityForMovePosition(motor.TransientPosition, tmpPosition, deltaTime);
                            break;
                    }
                    break;
                }
            }
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
                        var overlapsCount = motor.CharacterOverlap(
                            motor.TransientPosition,
                            motor.TransientRotation,
                            _probedColliders,
                            motor.CollidableLayers,
                            QueryTriggerInteraction.Ignore);
                        
                        // obstructions detected, back to crouching
                        if (overlapsCount > 0) {
                            motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                        }
                        // no obstructions, stand up
                        else {
                            meshRoot.localScale = new Vector3(1f, 1f, 1f);
                            _isCrouching = false;
                        }
                    }
                    
                    break;
                }

                case ControlMode.Climb: {
                    // in climb state, check if we are still on the ladder
                    if (ClimbState == ClimbState.Climb) {
                        ActiveLadder.ClosestPointOnLadder(motor.TransientPosition, out _deviation);

                        // climb off the ladder, transition to de-anchor state
                        if (Mathf.Abs(_deviation) > 0.05f) {
                            ClimbState = ClimbState.DeAnchor;
                            
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
                    else if (ClimbState == ClimbState.Anchor || ClimbState == ClimbState.DeAnchor) {
                        if (_anchoringTimer >= anchoringDuration) {
                            if (ClimbState == ClimbState.Anchor) {
                                ClimbState = ClimbState.Climb;
                            }
                            else if (ClimbState == ClimbState.DeAnchor) {
                                TransitionToMode(ControlMode.Default);
                            }
                        }
                        
                        _anchoringTimer += deltaTime;
                    }

                    break;
                }
            }
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
            // change audio sound based on ground tags
            if (hitCollider.CompareTag("SnowGround")) {
                _walkSound = footstepSound;
                _dashSound = footstepSound;
                _jumpSound = jumpSound;
                _landSound = landSound;
            }
            else if (hitCollider.CompareTag("WoodGround")) {

            }
            else if (hitCollider.CompareTag("MarbleGround")) {

            }
            else if (hitCollider.CompareTag("MeadowGround")) {

            }
            else if (hitCollider.CompareTag("SandGround")) {

            }
            else if (hitCollider.CompareTag("BounceGround")) {
                // bounce off the ground a little bit on the next update
                _bounceOffGround = true;
            }
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
                    _audioSource.PlayOneShot(_landSound);
                    if (_bounceOffGround) {
                        AddExtraVelocity(motor.CharacterUp * (jumpUpSpeed * 0.3f));
                        _bounceOffGround = false;
                    }

                    break;
                }
                // player leaves the ground (not necessarily jumping, could be falling off platforms, etc.)
                case false when motor.LastGroundingStatus.IsStableOnGround:
                    _audioSource.PlayOneShot(_jumpSound);
                    break;
            }
        }

        /// <summary>
        /// Adds extra velocity to the character to simulate explosion forces, hit impacts, wind zones, impulses, etc.
        /// </summary>
        public void AddExtraVelocity(Vector3 velocity) {
            if (ControlMode != ControlMode.Auto && ControlMode != ControlMode.Climb) {
                _extraVelocity = velocity;
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
            Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {
            
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider) {
            
        }
        
        public void AddIgnoredColliders(IEnumerable<Collider> colliders) {
            ignoredColliders.Clear();
            ignoredColliders.AddRange(colliders);
        }

    }
}