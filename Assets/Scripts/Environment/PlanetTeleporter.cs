using UnityEngine;
using UnityEngine.Events;
using Players;

namespace Environment {
    
    public class PlanetTeleporter : MonoBehaviour {
        
        [SerializeField] private PlanetTeleporter teleportTo;
        
        public UnityAction<PlayerController> OnPlayerTeleport;
        
        private bool Teleported { get; set; } = false;

        private void OnTriggerEnter(Collider other) {
            if (!other.CompareTag("Player")) {
                return;
            }
            
            if (!Teleported) {
                var controller = other.GetComponent<PlayerController>();
                if (controller) {
                    var destination = teleportTo.transform;
                    controller.Motor.SetPositionAndRotation(destination.position, destination.rotation);
                    OnPlayerTeleport(controller);
                    teleportTo.Teleported = true;
                }
            }

            Teleported = false;
        }
    }
}