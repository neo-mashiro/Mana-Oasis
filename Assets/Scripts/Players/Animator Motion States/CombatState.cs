// // Animancer // https://kybernetik.com.au/animancer // Copyright 2020 Kybernetik //
//
// #if !UNITY_EDITOR
// #pragma warning disable CS0618 // Type or member is obsolete (for NormalizedEndTime in Animancer Lite).
// #endif
// #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.
//
// using System;
// using UnityEngine;
// using UnityEngine.Events;
// using Animancer;
//
// namespace Sandbox
// {
//     /// <summary>A <see cref="CreatureState"/> which plays a series of "attack" animations.</summary>
//     /// <example><see href="https://kybernetik.com.au/animancer/docs/examples/animator-controllers/3d-game-kit/attack">3D Game Kit/Attack</see></example>
//     /// https://kybernetik.com.au/animancer/api/Animancer.Examples.AnimatorControllers.GameKit/AttackState
//     /// 
//
//     public sealed class CombatState : MotionState
//     {
//         /************************************************************************************************************************/
//
//         [SerializeField] private float _TurnSpeed = 400;
//         [SerializeField] private UnityEvent _SetWeaponOwner;// See the Read Me.
//         [SerializeField] private UnityEvent _OnStart;// See the Read Me.
//         [SerializeField] private UnityEvent _OnEnd;// See the Read Me.
//         [SerializeField] private ClipState.Transition[] _Animations;
//
//         private int _AttackIndex = int.MaxValue;
//         private ClipState.Transition _Attack;
//         
//         public override StatePriority Priority => StatePriority.Medium;
//
//         /************************************************************************************************************************/
//
//         private void Awake()
//         {
//             _SetWeaponOwner.Invoke();
//         }
//
//         /************************************************************************************************************************/
//
//         public override bool CanEnterState => AnimatorController.IsGrounded;
//
//         /************************************************************************************************************************/
//
//         /// <summary>
//         /// Start at the beginning of the sequence by default, but if the previous attack hasn't faded out yet then
//         /// perform the next attack instead.
//         /// </summary>
//         private void OnEnable()
//         {
//             if (_AttackIndex >= _Animations.Length - 1 ||
//                 _Animations[_AttackIndex].State.Weight == 0)
//             {
//                 _AttackIndex = 0;
//             }
//             else
//             {
//                 _AttackIndex++;
//             }
//
//             _Attack = _Animations[_AttackIndex];
//             Creature.Animancer.Play(_Attack);
//             Creature.ForwardSpeed = 0;
//             _OnStart.Invoke();
//         }
//
//         /************************************************************************************************************************/
//
//         private void OnDisable()
//         {
//             _OnEnd.Invoke();
//         }
//
//         private void OnAnimatorMove() {
//             // apply root motion of the attack animation to the character
//             // try to use AddExtraVelocity() to do so, but need to tweak.
//             // or better, we can reference the root motion example in KCC, accumulate root motion in
//             // PlayerAnimatorController, and then pass to KCC to update, and then reset to 0
//             AnimatorController.AccumulateRootMotion(RootDeltaPosition, RootDeltaRotation);
//         }
//
//         /************************************************************************************************************************/
//
//         public override bool FullMovementControl => false;
//
//         /************************************************************************************************************************/
//
//         private void FixedUpdate()
//         {
//             if (AnimatorController.CheckMotionState())
//                 return;
//
//             AnimatorController.TurnTowards(AnimatorController.Brain.Movement, _TurnSpeed);
//         }
//
//         /************************************************************************************************************************/
//
//         // Use the End Event time to determine when this state is alowed to exit.
//
//         // We cannot simply have this method return false and set the End Event to call Creature.CheckMotionState
//         // because it uses TrySetState (instead of ForceSetState) which would be prevented if this returned false.
//
//         // And we cannot have this method return true because that would allow other actions like jumping in the
//         // middle of an attack.
//
//         public override bool CanExitState
//             => _Attack.State.NormalizedTime >= _Attack.State.Events.NormalizedEndTime;
//
//         /************************************************************************************************************************/
//     }
// }
