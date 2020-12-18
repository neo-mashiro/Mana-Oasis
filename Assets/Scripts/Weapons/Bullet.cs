using System;
using System.Collections;
using UnityEngine;

namespace Weapons {
    
    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour, IFireable {

        [SerializeField, Range(20, 200)] private int bulletSpeed = 50;

        private Rigidbody _rigidbody;
        
        private void OnEnable() => _rigidbody = GetComponent<Rigidbody>();

        public void Load() {}

        public void Fire() {
            var parent = transform.parent;
            var direction = parent.forward;
            _rigidbody.position = parent.position + direction;
            _rigidbody.velocity = direction * bulletSpeed;
            
            StartCoroutine(Recycle(60f / bulletSpeed));
        }

        private void OnCollisionEnter(Collision other) => OnHit(other);
        
        public void OnHit(Collision other) {
            // gameObject.SetActive(false);  // don't SetActive here
        }

        public IEnumerator Recycle(float timeout) {
            yield return new WaitForSeconds(timeout);
            if (gameObject.activeSelf) {
                gameObject.SetActive(false);
            }
        }
    }
}
