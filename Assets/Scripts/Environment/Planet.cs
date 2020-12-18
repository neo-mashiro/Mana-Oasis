using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using Players;

namespace Environment {
    
    public class Planet : MonoBehaviour, IMoverController {
        
        public PhysicsMover planetMover;
        
        [SerializeField] private float gravity = 20;
        [SerializeField] private Vector3 orbitAxis = Vector3.forward;
        [SerializeField] private float orbitSpeed = 5;

        public PlanetTeleporter entrance;
        public PlanetTeleporter exit;

        private List<PlayerController> _controllersOnPlanet = new List<PlayerController>();
        private Vector3 _savedGravity;
        private Quaternion _lastRotation;

        private void Start() {
            entrance.OnPlayerTeleport += ControlGravity;
            exit.OnPlayerTeleport += UnControlGravity;
            
            _lastRotation = planetMover.transform.rotation;
            
            planetMover.MoverController = this;
        }

        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime) {
            goalPosition = planetMover.Rigidbody.position;
            goalRotation = Quaternion.Euler(orbitAxis * (orbitSpeed * deltaTime)) * _lastRotation;
            _lastRotation = goalRotation;

            // the gravity vector starts from the player character and points towards the planet center
            foreach (var controller in _controllersOnPlanet) {
                controller.Gravity = (planetMover.transform.position - controller.transform.position).normalized * gravity;
            }
        }

        private void ControlGravity(PlayerController controller) {
            _savedGravity = controller.Gravity;
            _controllersOnPlanet.Add(controller);
        }

        private void UnControlGravity(PlayerController controller) {
            controller.Gravity = _savedGravity;
            _controllersOnPlanet.Remove(controller);
        }
    }
}