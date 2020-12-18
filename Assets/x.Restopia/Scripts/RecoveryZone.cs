using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Players;
using static Utilities.CoroutineScheduler;

namespace x.Restopia.Scripts {
    
    /// <summary>
    /// Players inside a recovery zone will have their health and mana recovered by a certain percentage every 10
    /// seconds, the recovery effect disappears as soon as players exit the zone.
    /// </summary>
    public class RecoveryZone : MonoBehaviour {

        [SerializeField] private float recoverPercent = 0.03f;
        
        private Dictionary<string, Coroutine> _healthChargers = new Dictionary<string, Coroutine>();
        private Dictionary<string, Coroutine> _manaChargers = new Dictionary<string, Coroutine>();
        
        // singleton! singleton! singleton! singleton! singleton! singleton! singleton!

        private void OnTriggerEnter(Collider other) {
            if (!other.CompareTag("Player")) {
                return;
            }
            
            var status = other.gameObject.GetComponent<PlayerStatus>();
            if (status) {
                var healthCharger = RepeatSchedule(10f, 10f, status.RecoverHealthPercent, recoverPercent);
                var manaCharger = RepeatSchedule(10f, 10f, status.RecoverManaPercent, recoverPercent);
                _healthChargers.Add(status.PlayerName, StartCoroutine(healthCharger));
                _manaChargers.Add(status.PlayerName, StartCoroutine(manaCharger));
            }
        }

        private void OnTriggerExit(Collider other) {
            if (!other.CompareTag("Player")) {
                return;
            }

            var status = other.gameObject.GetComponent<PlayerStatus>();
            if (status) {
                StopCoroutine(_healthChargers[status.PlayerName]);
                StopCoroutine(_manaChargers[status.PlayerName]);
                _healthChargers.Remove(status.PlayerName);
                _manaChargers.Remove(status.PlayerName);
            }
        }
    }
}
