using UnityEngine;
using Utilities;

namespace Weapons {
    
    [RequireComponent(typeof(ObjectPool))]
    public class AutomaticRifle : MonoBehaviour {
        
        [Tooltip("The number of frames to wait before the rifle fires again.")]
        [SerializeField, Range(10, 20)] private int fireCooldown;
        
        private ObjectPool _cartridge;
        private IFireable _bullet;
        
        private AudioSource _audioSource;

        private int _frameCount;
        
        private void Start() {
            _cartridge = GetComponent<ObjectPool>().Pool;
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update() {
            if (Input.GetMouseButton(0)) {
                if (_frameCount % fireCooldown == 0) {
                    _bullet = _cartridge.Fetch("Bullet").GetComponent<IFireable>();
                    _bullet.Fire();
                    _audioSource.Play();
                }
                _frameCount++;
            }
            else {
                _frameCount = 0;  // reset the count when the mouse is released
            }
        }
    }
}

