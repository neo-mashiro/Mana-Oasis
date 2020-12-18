using System;
using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using static Utilities.MathUtils;
using static Utilities.CoroutineScheduler;

namespace Players {
    
    public class PlayerController : MonoBehaviour, ICharacterController {
        
        public KinematicCharacterMotor motor;

        [Header("Ground Movement")]
        [SerializeField] private float maxStableMoveSpeed = 10f;
        [SerializeField] private float stableMovementSharpness = 15f;
        [SerializeField] private float orientationSharpness = 20f;
        [SerializeField] private OrientationMode orientationMode = OrientationMode.TowardsMovement;
        [SerializeField] private Vector3 gravity = new Vector3(0, -30f, 0);

        [Header("Air Movement")]
        [SerializeField] private float maxAirMoveSpeed = 10f;
        [SerializeField] private float airAccelerationSpeed = 10f;
        [SerializeField] private float airDrag = 0.1f;

        [Header("Jump")]
        [SerializeField] private bool allowJumpingWhenSliding = false;
        [SerializeField] private bool allowDoubleJump = false;
        [SerializeField] private bool allowWallJump = false;
        [SerializeField] private float jumpUpSpeed = 10f;
        [SerializeField] private float jumpScalableForwardSpeed = 0f;
        [SerializeField] private float jumpPreGroundingGraceTime = 0.1f;
        [SerializeField] private float jumpPostGroundingGraceTime = 0.1f;
        
        [Header("Auto Mode")]
        [SerializeField] private float autoSpeed = 15f;
        
        [Header("Free Mode")]
        [SerializeField] private float freeMoveSpeed = 20f;
        [SerializeField] private float freeSharpness = 15;
        
        [Header("Swim Mode")]
        [SerializeField] private Transform swimmingReferencePoint;
        [SerializeField] private LayerMask waterLayer;
        [SerializeField] private float swimmingSpeed = 4f;
        [SerializeField] private float swimmingMovementSharpness = 3;
        [SerializeField] private float swimmingOrientationSharpness = 2f;
        [SerializeField] private float gravityUnderWater = -0.1f;
        
        [Header("Climb Mode")]
        [SerializeField] private float climbingSpeed = 4f;
        [SerializeField] private float anchoringDuration = 0.25f;
        [SerializeField] private LayerMask ladderLayer;

        [Header("Obstruction and Orientation")]
        [SerializeField] private List<Collider> ignoredColliders = new List<Collider>();
        [SerializeField] private BonusOrientationMode bonusOrientationMode = BonusOrientationMode.TowardsGravity;
        [SerializeField] private float bonusOrientationSharpness = 10f;
        
        [Header("References")]
        [SerializeField] private Transform meshRoot;
        [SerializeField] private Transform cameraFollowPoint;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip footstepSound;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip landSound;
        [SerializeField] private AudioClip flySound;
        [SerializeField] private AudioClip swimSound;

        // public properties
        public Vector3 Gravity {
            get => gravity;
            set => gravity = value;
        }
        
        public Transform MeshRoot {
            get => meshRoot;
            set => meshRoot = value;
        }
        
        public Transform CameraFollowPoint {
            get => cameraFollowPoint;
            set => cameraFollowPoint = value;
        }

        // private properties
        private ControlMode ControlMode { get; set; }
        
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
        private Collider[] _probedColliders = new Collider[8];
        private RaycastHit[] _probedHits = new RaycastHit[8];

        // jump and crouch
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
        
        // for free and swim mode
        private bool _jumpInputIsHeld = false;
        private bool _crouchInputIsHeld = false;
        
        // for swim mode
        private Collider _waterZone;
        
        // for climb mode
        private float _ladderUpDownInput;
        private ClimbState _internalClimbState;

        private Vector3 _ladderTargetPosition;
        private Quaternion _ladderTargetRotation;
        private float _onLadderSegmentState = 0;
        private float _anchoringTimer = 0f;
        private Vector3 _anchoringStartPosition = Vector3.zero;
        private Quaternion _anchoringStartRotation = Quaternion.identity;
        private Quaternion _rotationBeforeClimbing = Quaternion.identity;
        
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

                case ControlMode.Free:
                    motor.SetCapsuleCollisionsActivation(true);          // detect collisions or not?
                    motor.SetMovementCollisionsSolvingActivation(true);  // solve collision or not?
                    motor.SetGroundSolvingActivation(false);
                    _walkSound = flySound;
                    _dashSound = flySound;
                    _jumpSound = flySound;
                    _landSound = null;  // depend on ground tags
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
                    _rotationBeforeClimbing = motor.TransientRotation;

                    motor.SetMovementCollisionsSolvingActivation(false);
                    motor.SetGroundSolvingActivation(false);
                    ClimbState = ClimbState.Anchor;

                    // find the point on the ladder to snap to
                    _ladderTargetPosition = ActiveLadder.ClosestPointOnLadderSegment(motor.TransientPosition, out _onLadderSegmentState);
                    _ladderTargetRotation = ActiveLadder.transform.rotation;
                    break;
            }
        }

        private void OnModeExit(ControlMode mode, ControlMode toMode) {
            switch (mode) {
                case ControlMode.Default:
                    break;
                
                case ControlMode.Free:
                    motor.SetCapsuleCollisionsActivation(true);
                    motor.SetMovementCollisionsSolvingActivation(true);
                    motor.SetGroundSolvingActivation(true);
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
            // check and apply mode transitions
            if (inputs.FreeModeToggled && !_waterZone) {
                var nextMode = ControlMode == ControlMode.Free ? ControlMode.Default : ControlMode.Free;
                TransitionToMode(nextMode);
            }

            if (inputs.ClimbModeToggled) {
                var hits = motor.CharacterOverlap(
                    motor.TransientPosition,
                    motor.TransientRotation,
                    _probedColliders,
                    ladderLayer,
                    QueryTriggerInteraction.Collide);
                
                if (hits > 0 && !ReferenceEquals(_probedColliders[0], null)) {
                    var ladder = _probedColliders[0].gameObject.GetComponent<Ladder>();
                    if (ladder) {
                        if (ControlMode == ControlMode.Default || ControlMode == ControlMode.Free) {
                            ActiveLadder = ladder;
                            TransitionToMode(ControlMode.Climb);
                        }
                        else if (ControlMode == ControlMode.Climb) {
                            ClimbState = ClimbState.DeAnchor;
                            _ladderTargetPosition = motor.TransientPosition;
                            _ladderTargetRotation = _rotationBeforeClimbing;
                        }
                    }
                }
            }
            
            // input for moving up and down in free/swim mode
            _jumpInputIsHeld = inputs.JumpHeld;
            _crouchInputIsHeld = inputs.CrouchHeld;
            
            // essential input
            var moveVector = Vector3.ClampMagnitude(new Vector3(inputs.MovementX, 0f, inputs.MovementZ), 1f);
            var cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f) {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, motor.CharacterUp).normalized;
            }
            var cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, motor.CharacterUp);

            switch (ControlMode) {
                case ControlMode.Default:
                    _moveInputVector = cameraPlanarRotation * moveVector;

                    if (orientationMode == OrientationMode.TowardsCamera) {
                        _lookInputVector = cameraPlanarDirection;
                    }
                    else if (orientationMode == OrientationMode.TowardsMovement) {
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

                    break;

                case ControlMode.Free:
                    _moveInputVector = inputs.CameraRotation * moveVector;
                    _lookInputVector = cameraPlanarDirection;
                    break;

                case ControlMode.Swim:
                    _moveInputVector = inputs.CameraRotation * moveVector;
                    _lookInputVector = cameraPlanarDirection;
                    _jumpRequested = inputs.JumpHeld;
                    break;

                case ControlMode.Climb:
                    _ladderUpDownInput = inputs.MovementZ;
                    break;
            }
        }

        public void ProcessInput(ref AICharacterInputs inputs) {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        public void BeforeCharacterUpdate(float deltaTime) {
            // check if the player is underwater
            var waterHits = motor.CharacterOverlap(
                motor.TransientPosition,
                motor.TransientRotation,
                _probedColliders,
                waterLayer,
                QueryTriggerInteraction.Collide);
            
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
            }
            
            switch (ControlMode) {
                case ControlMode.Default:
                    break;
            }
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
            switch (ControlMode) {
                case ControlMode.Default:
                case ControlMode.Free:
                case ControlMode.Swim:
                    // normally, if gravity is straight down, rotation is only controlled by the look direction
                    if (_lookInputVector.sqrMagnitude > 0f && orientationSharpness > 0f) {
                        var smoothedLookDirection = Vector3.Slerp(
                            motor.CharacterForward,
                            _lookInputVector,
                            EaseFactor(orientationSharpness, deltaTime)).normalized;
                        
                        currentRotation = Quaternion.LookRotation(smoothedLookDirection, motor.CharacterUp);
                    }
                    
                    var currentUp = currentRotation * Vector3.up;
                    
                    // if gravity is not straight down, we need to reorient character's up towards the inverse gravity
                    if (bonusOrientationMode == BonusOrientationMode.TowardsGravity) {
                        var smoothedUpDirection = Vector3.Slerp(
                            currentUp,
                            -gravity.normalized,
                            EaseFactor(bonusOrientationSharpness, deltaTime));
                        
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedUpDirection) * currentRotation;
                    }
                    
                    // bonus: reorient character's up towards the slope normal vector
                    else if (bonusOrientationMode == BonusOrientationMode.TowardsGroundSlopeAndGravity) {
                        if (motor.GroundingStatus.IsStableOnGround) {
                            var characterBottomHemiCenter = motor.TransientPosition + currentUp * motor.Capsule.radius;
                            var smoothedGroundNormal = Vector3.Slerp(
                                motor.CharacterUp,
                                motor.GroundingStatus.GroundNormal,
                                EaseFactor(bonusOrientationSharpness, deltaTime));
                            
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                            // adjust the position to create a rotation around the bottom hemi center instead of the pivot
                            motor.SetTransientPosition(characterBottomHemiCenter + currentRotation * Vector3.down * motor.Capsule.radius);
                        }
                        else {
                            var smoothedUpDirection = Vector3.Slerp(
                                currentUp,
                                -gravity.normalized,
                                EaseFactor(bonusOrientationSharpness, deltaTime));

                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedUpDirection) * currentRotation;
                        }
                    }
                    
                    else {
                        var smoothedUpDirection = Vector3.Slerp(
                            currentUp,
                            Vector3.up,
                            EaseFactor(bonusOrientationSharpness, deltaTime));
                        
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedUpDirection) * currentRotation;
                    }
                    
                    break;

                case ControlMode.Climb:
                    if (ClimbState == ClimbState.Climb) {
                        currentRotation = ActiveLadder.transform.rotation;
                    }
                    else if (ClimbState == ClimbState.Anchor || ClimbState == ClimbState.DeAnchor) {
                        // rotate the character towards the ladder plane
                        currentRotation = Quaternion.Slerp(
                            _anchoringStartRotation,
                            _ladderTargetRotation,
                            _anchoringTimer / anchoringDuration);
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
                            if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f) {
                                effectiveGroundNormal = motor.GroundingStatus.OuterGroundNormal;
                            }
                            else {
                                effectiveGroundNormal = motor.GroundingStatus.InnerGroundNormal;
                            }
                        }

                        // project velocity onto the slope
                        currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) *
                                          currentVelocityMagnitude;

                        var inputRight = Vector3.Cross(_moveInputVector, motor.CharacterUp);
                        var reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized *
                                              _moveInputVector.magnitude;
                        var targetVelocity = reorientedInput * maxStableMoveSpeed;

                        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity,
                            EaseFactor(stableMovementSharpness, deltaTime));
                    }

                    // air movement
                    else {
                        if (_moveInputVector.sqrMagnitude > 0f) {
                            var addedVelocity = _moveInputVector * (airAccelerationSpeed * deltaTime);
                            var currentVelocityOnInputsPlane =
                                Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

                            if (currentVelocityOnInputsPlane.magnitude < maxAirMoveSpeed) {
                                var newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity,
                                    maxAirMoveSpeed);
                                addedVelocity = newTotal - currentVelocityOnInputsPlane;
                            }
                            else {
                                if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f) {
                                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity,
                                        currentVelocityOnInputsPlane.normalized);
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

                            currentVelocity += jumpDirection * jumpUpSpeed -
                                               Vector3.Project(currentVelocity, motor.CharacterUp);
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

                case ControlMode.Free: {
                    var verticalInput = 0f + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);
                    var targetVelocity = (_moveInputVector + motor.CharacterUp * verticalInput).normalized * freeMoveSpeed;
                    currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(freeSharpness, deltaTime));
                    break;
                }

                case ControlMode.Swim: {
                    var verticalInput = gravityUnderWater + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);
                    var targetVelocity = (_moveInputVector + motor.CharacterUp * verticalInput).normalized * swimmingSpeed;
                    var smoothedVelocity = Vector3.Lerp(currentVelocity, targetVelocity, EaseFactor(swimmingMovementSharpness, deltaTime));

                    // see if our swimming reference point would be out of water after the update
                    var resultingReferencePointPosition = swimmingReferencePoint.position + smoothedVelocity * deltaTime;
                    var waterTransform = _waterZone.transform;
                    var closestPointOnWater = Physics.ClosestPoint(
                        resultingReferencePointPosition,
                        _waterZone,
                        waterTransform.position,
                        waterTransform.rotation);

                    if (closestPointOnWater != resultingReferencePointPosition) {
                        // if so, project the velocity onto the water surface so that we won't fly off when being close to the surface
                        // we can set the reference point to be near the neck/shoulder, thus the player can stick her head out of water
                        var waterSurfaceNormal = (resultingReferencePointPosition - closestPointOnWater).normalized;
                        smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, waterSurfaceNormal);

                        // but we can jump out of water ???
                        if (_jumpRequested) {
                            smoothedVelocity += motor.CharacterUp * jumpUpSpeed - Vector3.Project(currentVelocity, motor.CharacterUp);
                        }
                    }

                    currentVelocity = smoothedVelocity;
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
                            Vector3 tmpPosition = Vector3.Lerp(_anchoringStartPosition, _ladderTargetPosition, (_anchoringTimer / anchoringDuration));
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
                    switch (ClimbState) {
                        case ClimbState.Climb:
                            // Detect getting off ladder during climbing
                            ActiveLadder.ClosestPointOnLadderSegment(motor.TransientPosition, out _onLadderSegmentState);
                            // within the ladder segments, _onLadderSegmentState is always 0
                            // otherwise, it is the distance from the closest extremity if we are out of bounds
                            if (Mathf.Abs(_onLadderSegmentState) > 0.05f) {
                                ClimbState = ClimbState.DeAnchor;

                                // If we're higher than the ladder top point
                                if (_onLadderSegmentState > 0) {
                                    _ladderTargetPosition = ActiveLadder.TopReleasePoint.position;
                                    _ladderTargetRotation = ActiveLadder.TopReleasePoint.rotation;
                                }
                                // If we're lower than the ladder bottom point
                                else if (_onLadderSegmentState < 0) {
                                    _ladderTargetPosition = ActiveLadder.BottomReleasePoint.position;
                                    _ladderTargetRotation = ActiveLadder.BottomReleasePoint.rotation;
                                }
                            }
                            break;
                        case ClimbState.Anchor:
                        case ClimbState.DeAnchor:
                            // Detect transitioning out from anchoring states
                            if (_anchoringTimer >= anchoringDuration) {
                                if (ClimbState == ClimbState.Anchor) {
                                    ClimbState = ClimbState.Climb;
                                }
                                else if (ClimbState == ClimbState.DeAnchor) {
                                    TransitionToMode(ControlMode.Default);
                                }
                            }

                            // Keep track of time since we started anchoring
                            _anchoringTimer += deltaTime;
                            break;
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
            // if we hit the ground in free mode, transition to default mode
            if (ControlMode == ControlMode.Free) {
                TransitionToMode(ControlMode.Default);
            }
            
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
            // player lands on the ground
            if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround) {
                _audioSource.PlayOneShot(_landSound);
                if (_bounceOffGround) {
                    Accelerate(motor.CharacterUp * (jumpUpSpeed * 0.3f));
                    _bounceOffGround = false;
                }
            }
            // player leaves the ground (not necessarily jumping, could be falling off platforms, etc.)
            else if (!motor.GroundingStatus.IsStableOnGround && motor.LastGroundingStatus.IsStableOnGround) {
                _audioSource.PlayOneShot(_jumpSound);
            }
        }

        /// <summary>
        /// Adds extra velocity to the character, to simulate explosion forces, hit impacts, wind zones, other impulses, etc.
        /// </summary>
        public void Accelerate(Vector3 velocity) {
            // example:
            //
            // if (Input.GetKeyDown(KeyCode.P)) {
            //     PlayerController.motor.ForceUnground(0.1f);
            //     PlayerController.Accelerate(-PlayerController.motor.CharacterForward * 10f);
            // }
            
            switch (ControlMode) {
                case ControlMode.Default:
                    _extraVelocity += velocity;
                    break;
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
            Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {
            
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider) {
            
        }

    }
}