// // Animancer // https://kybernetik.com.au/animancer // Copyright 2020 Kybernetik //
//
// #if !UNITY_EDITOR
// #pragma warning disable CS0618 // Type or member is obsolete (for MixerState in Animancer Lite).
// #endif
// #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.
//
// using UnityEngine;
// using Animancer;
//
// namespace Sandbox
// {
//     /// <summary>A <see cref="CreatureState"/> which plays a "getting hit" animation.</summary>
//     /// <example><see href="https://kybernetik.com.au/animancer/docs/examples/animator-controllers/3d-game-kit/flinch">3D Game Kit/Flinch</see></example>
//     /// https://kybernetik.com.au/animancer/api/Animancer.Examples.AnimatorControllers.GameKit/FlinchState
//     /// 
//
//     public class FlinchState : MotionState
//     {
//         /************************************************************************************************************************/
//
//         [SerializeField] private MixerState.Transition2D _Animation;
//         [SerializeField] private LayerMask _EnemyLayers;
//         [SerializeField] private float _EnemyCheckRadius = 1;
//         
//         public override StatePriority Priority => StatePriority.High;
//
//         /************************************************************************************************************************/
//
//         private void Awake()
//         {
//             _Animation.Events.OnEnd = AnimatorController.ForceEnterIdleState;
//         }
//
//         /************************************************************************************************************************/
//
//         public void OnDamageReceived() => AnimatorController.StateMachine.ForceSetState(this);
//
//         /************************************************************************************************************************/
//
//         private void OnEnable()
//         {
//             AnimatorController.ForwardSpeed = 0;
//             AnimatorController.Animancer.Play(_Animation);
//
//             var direction = DetermineHitDirection();
//
//             // Once we know which direction the hit came from, we need to convert it to be relative to the model.
//             // The Parameter X represents left/right so we project the direction onto the right vector.
//             // The Parameter Y represents forward/back so we project the direction onto the forward vector.
//             _Animation.State.Parameter = new Vector2(
//                 Vector3.Dot(AnimatorController.Animancer.transform.right, direction),
//                 Vector3.Dot(AnimatorController.Animancer.transform.forward, direction));
//         }
//
//         /************************************************************************************************************************/
//
//         /// <summary>
//         /// Since Animancer does not actually depend on the 3D Game Kit (except for this example), we cannot reference
//         /// any of its scripts from here so we cannot use their <c>IMessageReceiver</c> system which informs the
//         /// defending PlayerAnimatorController where the incoming hit came from.
//         /// <para></para>
//         /// So instead we just find the closest enemy and use that as the direction.
//         /// </summary>
//         private Vector3 DetermineHitDirection()
//         {
//             var position = AnimatorController.transform.position;
//             var closestEnemySquaredDistance = float.PositiveInfinity;
//             var closestEnemyDirection = Vector3.zero;
//
//             var enemies = Physics.OverlapSphere(position, _EnemyCheckRadius, _EnemyLayers);
//             for (int i = 0; i < enemies.Length; i++)
//             {
//                 var direction = enemies[i].transform.position - position;
//                 var squaredDistance = direction.magnitude;
//                 if (closestEnemySquaredDistance > squaredDistance)
//                 {
//                     closestEnemySquaredDistance = squaredDistance;
//                     closestEnemyDirection = direction;
//                 }
//             }
//
//             return closestEnemyDirection.normalized;
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
