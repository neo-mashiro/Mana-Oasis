using UnityEngine;

namespace Players {
    
    public class SimpleTPSController : MonoBehaviour {

        // Example Use of Attributes:
        // https://gist.github.com/neo-mashiro/7e03247557874e45f3d02d76e20cc4e4
        
        [SerializeField] private float speed = 6.0f;

        private Transform _mainCamera;
        private CharacterController _controller;

        private float _angle;
        private const float TurnSmoothTime = 0.1f;
        private float _turnSmoothVelocity;

        private void Start() {
            _mainCamera = Camera.main.transform;
            _controller = GetComponent<CharacterController>();
        }

        private void Update() {
            var h = Input.GetAxisRaw("Horizontal");
            var v = Input.GetAxisRaw("Vertical");
            var direction = new Vector3(h, 0, v);

            if (direction.magnitude >= 0.1f) {
                // we add the angle of main camera because we want the character to rotate as we move the camera 
                _angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _mainCamera.eulerAngles.y;
                direction = Quaternion.Euler(0, _angle, 0) * Vector3.forward;

                _angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, _angle,
                    ref _turnSmoothVelocity, TurnSmoothTime);
                transform.rotation = Quaternion.Euler(0, _angle, 0);

                // always make sure that the move vector is normalized
                _controller.Move(direction.normalized * (speed * Time.deltaTime));
            }
        }
    }
}