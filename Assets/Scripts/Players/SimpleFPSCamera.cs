using UnityEngine;

namespace Players {
	
	// a first person camera, to be attached on the player
	public class SimpleFPSCamera : MonoBehaviour {
		[SerializeField] private float sensitivity = 30f;
		[SerializeField] private float smoothSpeed = 50f;

		private Transform _player;
		private float _xRotation;

		private void Start() {
			LockCursor();
			_player = transform.parent;
		}

		private void Update() {
			// horizontal: player rotates around y axis, so does the camera (child)
			var h = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
			_player.transform.Rotate(Vector3.up * h);

			// vertical: only the camera rotates
			var v = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
			_xRotation -= v;
			_xRotation = Mathf.Clamp(_xRotation, -90, 90);
			var target = Quaternion.Euler(_xRotation, 0f, 0f);
			transform.localRotation = Quaternion.Slerp(transform.localRotation, target,  smoothSpeed * Time.deltaTime);

			if (Input.GetButtonDown("Cancel")) {  // escape key
				UnlockCursor();
			}
		}

		private static void LockCursor() {
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked; // lock the cursor within the game view
		}

		private static void UnlockCursor() {
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
		}
	}
}