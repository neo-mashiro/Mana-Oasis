// // Animancer // https://kybernetik.com.au/animancer // Copyright 2020 Kybernetik //
//
// #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.
//
// using Animancer.FSM;
// using UnityEngine;
// using UnityEngine.Events;
// using Animancer;
//
// namespace Sandbox
// {
//     /// <summary>A <see cref="CreatureState"/> which plays a "dying" animation.</summary>
//     /// <example><see href="https://kybernetik.com.au/animancer/docs/examples/animator-controllers/3d-game-kit/die">3D Game Kit/Die</see></example>
//     /// https://kybernetik.com.au/animancer/api/Animancer.Examples.AnimatorControllers.GameKit/DieState
//     /// 
//
//     public class DieState : MotionState
//     {
//         /************************************************************************************************************************/
//
//         [SerializeField] private ClipState.Transition _Animation;
//         [SerializeField] private UnityEvent _OnEnterState;// See the Read Me.
//         [SerializeField] private UnityEvent _OnExitState;// See the Read Me.
//         
//         public override StatePriority Priority => StatePriority.High;
//
//         /************************************************************************************************************************/
//
//         private void Awake()
//         {
//             // Respawn immediately when the animation ends.
//             _Animation.Events.OnEnd = AnimatorController.Respawn.ForceEnterState;
//         }
//
//         /************************************************************************************************************************/
//
//         public void OnDeath()
//         {
//             AnimatorController.StateMachine.ForceSetState(this);
//         }
//
//         /************************************************************************************************************************/
//
//         private void OnEnable()
//         {
//             AnimatorController.Animancer.Play(_Animation);
//             AnimatorController.ForwardSpeed = 0;
//             _OnEnterState.Invoke();
//         }
//
//         /************************************************************************************************************************/
//
//         private void OnDisable()
//         {
//             _OnExitState.Invoke();
//         }
//
//         /************************************************************************************************************************/
//
//         public override bool FullMovementControl => false;
//
//         /************************************************************************************************************************/
//
//         public override bool CanExitState => false;
//
//         /************************************************************************************************************************/
//     }
// }
