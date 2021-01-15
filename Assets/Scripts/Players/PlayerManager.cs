using NaughtyAttributes;
using Sandbox;
using UnityEngine;

namespace Players {

    public class PlayerManager : MonoBehaviour {

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private PlayerAnimatorController playerAnimatorController;

        private void Start() => Cursor.lockState = CursorLockMode.Locked;

        private void Update() {
            UpdatePlayerAnimatorController();
            UpdateCharacter();
        }
        
        private void LateUpdate() => UpdateCamera();

        private void UpdateCharacter() {
            // to be modified when migrating to the new Input System package
            var characterInputs = new PlayerCharacterInputs {
                MovementZ = Input.GetAxisRaw("Vertical"),
                MovementX = Input.GetAxisRaw("Horizontal"),
                JumpDown = Input.GetKeyDown(KeyCode.Space),
                JumpHeld = Input.GetKey(KeyCode.Space),
                CrouchDown = Input.GetKeyDown(KeyCode.LeftControl),
                CrouchUp = Input.GetKeyUp(KeyCode.LeftControl),
                CrouchHeld = Input.GetKey(KeyCode.LeftControl),
                AirModeToggled = Input.GetKeyUp(KeyCode.X),
                ClimbModeToggled = Input.GetKeyUp(KeyCode.E),
                ShiftHeld = Input.GetKey(KeyCode.LeftShift)
            };

            playerController.ProcessInput(ref characterInputs);
        }

        private void UpdateCamera() {
            var playerCameraInputs = new PlayerCameraInputs {
                MovementX = Input.GetAxisRaw("Mouse X"),
                MovementY = Input.GetAxisRaw("Mouse Y"),
                ZoomInput = -Input.GetAxis("Mouse ScrollWheel"),
                SwitchView = Input.GetMouseButtonDown(1)
            };

            playerCamera.ProcessInput(ref playerCameraInputs, Time.deltaTime);
        }

        private void UpdatePlayerAnimatorController() {
            playerAnimatorController.UpdateStateMachine();
        }

        [Button("Simulate External Force")]
        public void SimulateExternalForce() {
            playerController.Motor.ForceUnground();
            playerController.AddExtraVelocity(-playerController.Motor.CharacterForward * 20f);
        }
    }
}