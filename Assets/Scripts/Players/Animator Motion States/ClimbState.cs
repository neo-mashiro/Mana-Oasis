// // Animancer // https://kybernetik.com.au/animancer // Copyright 2020 Kybernetik //
//
// #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.
//
// using Animancer;
// using UnityEngine;
// using UnityEngine.Events;
//
// namespace Sandbox
// {
//     /// <summary>
//     /// A <see cref="CreatureState"/> which teleports back to the starting position, plays an animation then returns
//     /// to the <see cref="Creature.Idle"/> state.
//     /// </summary>
//
//     public class ClimbState : MotionState
//     {
//         /************************************************************************************************************************/
//
//         [SerializeField] private ClipState.Transition _Animation;
//         [SerializeField] private UnityEvent _OnEnterState;// See the Read Me.
//         [SerializeField] private UnityEvent _OnExitState;// See the Read Me.
//
//         private Vector3 _StartingPosition;
//
//         /************************************************************************************************************************/
//
//         private void Awake()
//         {
//             _Animation.Events.OnEnd = AnimatorController.ForceEnterIdleState;
//             _StartingPosition = transform.position;
//         }
//
//         /************************************************************************************************************************/
//
//         private void OnEnable()
//         {
//             AnimatorController.Animancer.Play(_Animation);
//             AnimatorController.transform.position = _StartingPosition;
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
//         public override bool CanExitState => false;
//
//         /************************************************************************************************************************/
//     }
// }
