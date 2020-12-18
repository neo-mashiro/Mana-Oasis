using System;
using System.Collections;
using UnityEngine;

namespace Weapons {
    
    [RequireComponent(typeof(ParticleSystem))]
    public class Magic : MonoBehaviour, IFireable {

        private ParticleSystem _particleSystem;
        
        private void OnEnable() => _particleSystem = GetComponent<ParticleSystem>();

        public void Load() {
            // animation here
        }

        public void Fire() {
            // handle _particleSystem
            
            StartCoroutine(Recycle(10));  // tweak the timeout based on the particle speed
        }

        private void OnCollisionEnter(Collision other) => OnHit(other);
        
        public void OnHit(Collision other) {
            // recycle the particle system immediately when detecting a hit
            gameObject.SetActive(false);
            // damage the hit point
        }

        public IEnumerator Recycle(float timeout) {
            yield return new WaitForSeconds(timeout);
            if (gameObject.activeSelf) {
                gameObject.SetActive(false);
            }
        }
    }
}