using UnityEngine;

namespace Players {
    
    // a simple first person controller script
    public class SimpleFPSController : MonoBehaviour {
        [SerializeField] private float jumpForce = 1.1f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float moveSpeed = 5.0f;

        private CharacterController _player;
        private Vector3 _moveVector = Vector3.zero;

        private void Start() {
            _player = GetComponent<CharacterController>();
        }

        private void Update() {
            if (_player.isGrounded) {
                var vecX = Vector3.right * (Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime);
                var vecZ = Vector3.forward * (Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);

                // combine movement in horizontal & vertical directions and convert to world space
                _moveVector = transform.TransformDirection(vecX + vecZ);

                if (Input.GetButtonDown("Jump")) {
                    _moveVector.y = jumpForce;
                }
            }

            _moveVector.y -= gravity * Time.deltaTime;  // apply gravity
            _player.Move(_moveVector);
        }
    }
}