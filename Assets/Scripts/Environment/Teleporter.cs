using System;
using UnityEngine;
using UnityEngine.Events;
using Players;

namespace Environment {
    
    public class Teleporter : MonoBehaviour {
        
        [SerializeField] private Teleporter teleportTo;

        private UnityAction<PlayerStatus> OnPlayerTeleport;  // later, change this to an UI action

        private bool Teleported { get; set; } = false;

        private void Start() {
            OnPlayerTeleport += RecoverPlayerStatus;
        }

        private void OnTriggerEnter(Collider other) {
            if (!other.CompareTag("Player")) {
                return;
            }
            
            if (!Teleported) {
                var controller = other.GetComponent<PlayerController>();
                if (controller) {
                    var destination = teleportTo.transform;
                    controller.Motor.SetPositionAndRotation(destination.position, destination.rotation);
                    // OnPlayerTeleport(other.GetComponent<PlayerStatus>());
                    teleportTo.Teleported = true;
                }
            }

            Teleported = false;
        }

        private void RecoverPlayerStatus(PlayerStatus status) {
            // recover player's skill cooldown
            throw new NotImplementedException();
        }
    }
}