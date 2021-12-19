using System;
using UnityEngine;
using Animancer;
using Animancer.FSM;
using NaughtyAttributes;

namespace Players {

    [DefaultExecutionOrder(-5000)]
    [RequireComponent(typeof(Animator), typeof(AnimancerComponent))]
    public class PlayerAnimatorController : MonoBehaviour {

        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private PlayerController playerController;
        
        [HorizontalLine(1, EColor.Violet)]
        [Header("Basic Motion States")]
        [SerializeField] private IdleState idle;
        [SerializeField] private JumpState jump;
        [SerializeField] private CrouchState crouch;
        [SerializeField] private AirborneState airborne;
        [SerializeField] private LandState land;
        [SerializeField] private MoveState move;
        [SerializeField] private ClimbState climb;
        [SerializeField] private FlyState fly;
        [SerializeField] private SwimState swim;
        // [SerializeField] private FlinchState flinch;
        // [SerializeField] private DieState die;
        
        // [HorizontalLine(1, EColor.Red)]
        // [Header("Combat Motion States")]
        // [SerializeField] private CombatState combat;
        // [SerializeField] private float attackTimeout;
        // [SerializeField, Range(1, 3)] private int maxCombo = 3;
        // [SerializeField, Range(0, 1)] private float criticalHitRate = 0.05f;
        
        // [HorizontalLine(1, EColor.Orange)]
        // [Header("Auto Motion States")]  // motion states in auto mode, including blend shapes

        [HorizontalLine(1, EColor.Green)]
        [Header("Inverse Kinematics (Look At)")]
        [SerializeField] private Transform lookAtTarget;
        [SerializeField, Range(0, 1)] private float weight = 1;
        [SerializeField, Range(0, 1)] private float bodyWeight = 0.3f;
        [SerializeField, Range(0, 1)] private float headWeight = 0.6f;
        [SerializeField, Range(0, 1)] private float eyesWeight = 1;
        [SerializeField, Range(0, 1)] private float clampWeight = 0.5f;
        
        [HorizontalLine(1, EColor.Pink)]
        [Header("Inverse Kinematics (Foot IK)")]
        [SerializeField, Range(0, 1)] private float notUsedWeight = 1;

        // public properties
        public AnimancerComponent Animancer => animancer;
        public StateMachine<MotionState> StateMachine { get; private set; }
        public Action ForceEnterIdleState { get; private set; }
        
        public MotionState NextState { get; private set; }
        public float ParameterX { get; private set; }
        public float ParameterY { get; private set; }

        public Vector3 RootMotionDeltaPosition { get; set; }
        public Quaternion RootMotionDeltaRotation { get; set; }

        public MotionState Idle => idle;
        public MotionState Jump => jump;
        public MotionState Crouch => crouch;
        public MotionState Airborne => airborne;
        public MotionState Land => land;
        public MotionState Move => move;
        public MotionState Climb => climb;
        public MotionState Fly => fly;
        public MotionState Swim => swim;
        // public MotionState Attack => attack;
        // public MotionState Flinch => flinch;
        // public MotionState Die => die;
        
        // private variables
        private StateMachine<MotionState>.InputBuffer _inputBuffer;
        private Animator _animator;
        private Transform _leftFoot;
        private Transform _rightFoot;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private void Awake() {
            StateMachine = new StateMachine<MotionState>(idle);
            ForceEnterIdleState = () => StateMachine.ForceSetState(idle);
            _inputBuffer = new StateMachine<MotionState>.InputBuffer(StateMachine);

            _animator = animancer.Animator;
            animancer.Layers[0].ApplyAnimatorIK = true;
            
            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        public void UpdateStateMachine(bool forceEnterState = false) {
            var motionStateInfo = playerController.MotionStateInfo;
            NextState = motionStateInfo.State;
            ParameterX = motionStateInfo.ParameterX;
            ParameterY = motionStateInfo.ParameterY;

            if (NextState != StateMachine.CurrentState) {
                // if (StateToEnter == attack) {
                //     _inputBuffer.TrySetState(attack, attackTimeout);
                // }
                // else
                {
                    if (forceEnterState) {
                        StateMachine.ForceSetState(NextState);  // skip CanEnterState, canExitState checks
                    }
                    else {
                        StateMachine.TrySetState(NextState);
                    }
                }
            }
            
            _inputBuffer.Update();
        }
        
        private void OnAnimatorMove() {
            // accumulate root motion from the animator
            RootMotionDeltaPosition += animancer.Animator.deltaPosition;
            RootMotionDeltaRotation = animancer.Animator.deltaRotation * RootMotionDeltaRotation;
        }
        
        // due to limitations in the Playables API, Unity will always call this method with layerIndex = 0
        private void OnAnimatorIK(int layerIndex) {
            // look at
            _animator.SetLookAtWeight(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
            _animator.SetLookAtPosition(lookAtTarget.transform.position);

            // foot ik (manually calculate)
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, transform.position);
            _animator.SetIKRotation(AvatarIKGoal.LeftFoot, transform.rotation);
        }
    }
}
