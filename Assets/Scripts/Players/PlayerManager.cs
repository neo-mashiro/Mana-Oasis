using UnityEngine;
using KinematicCharacterController;

namespace Players {

    public class PlayerManager : MonoBehaviour {

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            playerCamera.SetFollowTarget(playerController.CameraFollowPoint);
            playerCamera.AddIgnoredColliders(playerController.GetComponentsInChildren<Collider>());
        }

        private void Update() {
            if (Input.GetMouseButtonDown(0)) {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            HandleCharacterInput();
        }

        private void LateUpdate() {
            HandleCameraInput();
        }

        private void HandleCharacterInput() {
            var characterInputs = new PlayerCharacterInputs {
                MovementZ = Input.GetAxisRaw("Vertical"),
                MovementX = Input.GetAxisRaw("Horizontal"),
                CameraRotation = playerCamera.transform.rotation,
                JumpDown = Input.GetKeyDown(KeyCode.Space),
                JumpHeld = Input.GetKey(KeyCode.Space),
                CrouchDown = Input.GetKeyDown(KeyCode.LeftControl),
                CrouchUp = Input.GetKeyUp(KeyCode.LeftControl),
                CrouchHeld = Input.GetKey(KeyCode.LeftControl),
                FreeModeToggled = Input.GetKeyUp(KeyCode.X),
                ClimbModeToggled = Input.GetKeyUp(KeyCode.E)
            };

            playerController.ProcessInput(ref characterInputs);
        }

        private void HandleCameraInput() {
            var rotationInput = new Vector3(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"), 0f);
            if (Cursor.lockState != CursorLockMode.Locked) {
                rotationInput = Vector3.zero;
            }

            var zoomInput = -Input.GetAxis("Mouse ScrollWheel");
            var clickInput = Input.GetMouseButtonDown(1); // switch between first-person and third-person perspective

            playerCamera.ProcessInput(rotationInput, zoomInput, clickInput, Time.deltaTime);
        }

    }
}