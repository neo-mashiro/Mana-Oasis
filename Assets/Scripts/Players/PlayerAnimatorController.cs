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
        // [SerializeField] private ClimbState climb;
        // [SerializeField] private FlyState fly;
        // [SerializeField] private SwimState swim;
        // [SerializeField] private FlinchState flinch;
        // [SerializeField] private DieState die;
        
        [HorizontalLine(1, EColor.Red)]
        [Header("Combat Motion States")]
        // [SerializeField] private AttackState attack;
        [SerializeField] private float attackTimeout;
        [SerializeField, Range(1, 3)] private int maxCombo = 3;
        [SerializeField, Range(0, 1)] private float criticalHitChance = 0.05f;
        
        // [Header("Auto Motion States")]  // motion states in auto mode, including blend shapes
        
        // public properties
        public AnimancerComponent Animancer => animancer;
        public StateMachine<MotionState> StateMachine { get; private set; }
        public Action ForceEnterIdleState { get; private set; }
        
        public MotionState NextState { get; private set; }
        public float NextStateParameterX { get; private set; }
        public float NextStateParameterY { get; private set; }

        public Vector3 RootMotionDeltaPosition { get; set; }
        public Quaternion RootMotionDeltaRotation { get; set; }

        public MotionState Idle => idle;
        public MotionState Jump => jump;
        public MotionState Crouch => crouch;
        public MotionState Airborne => airborne;
        public MotionState Land => land;
        public MotionState Move => move;
        // public MotionState Climb => climb;
        // public MotionState Fly => fly;
        // public MotionState Swim => swim;
        // public MotionState Attack => attack;
        // public MotionState Flinch => flinch;
        // public MotionState Die => die;
        
        // private variables
        private StateMachine<MotionState>.InputBuffer _inputBuffer;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private void Awake() {
            StateMachine = new StateMachine<MotionState>(idle);
            ForceEnterIdleState = () => StateMachine.ForceSetState(idle);
            _inputBuffer = new StateMachine<MotionState>.InputBuffer(StateMachine);
        }

        public void UpdateStateMachine() {
            var motionStateInfo = playerController.MotionStateInfo;
            NextState = motionStateInfo.State;
            NextStateParameterX = motionStateInfo.ParameterX;
            NextStateParameterY = motionStateInfo.ParameterY;

            if (NextState != StateMachine.CurrentState) {
                // if (StateToEnter == attack) {
                //     _inputBuffer.TrySetState(attack, attackTimeout);
                // }
                // else
                {
                    StateMachine.TrySetState(NextState);
                }
            }
            
            _inputBuffer.Update();
        }
        
        private void OnAnimatorMove() {
            // accumulate root motion from the animator
            RootMotionDeltaPosition += animancer.Animator.deltaPosition;
            RootMotionDeltaRotation = animancer.Animator.deltaRotation * RootMotionDeltaRotation;
        }
    }
}
