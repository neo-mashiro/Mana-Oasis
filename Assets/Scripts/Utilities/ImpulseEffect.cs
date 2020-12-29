using UnityEngine;
using NaughtyAttributes;
using Random = UnityEngine.Random;

namespace Utilities {

    /// <summary>
    /// Use this mono class to simulate impulse and shake effects on stationary game objects. If you have a moving
    /// game object, manually add Perlin noise to its expected real-time position/rotation each frame.
    /// </summary>
    public class ImpulseEffect : MonoBehaviour {

        [HorizontalLine(2, EColor.Orange), Header("Impulse")]
        [SerializeField] private Vector3 maxPositionImpulse = Vector3.one;
        [SerializeField] private Vector3 maxRotationImpulse = Vector3.one * 15;
        [SerializeField] private float frequency = 25;
        
        [HorizontalLine(2, EColor.Yellow), Header("Recovery")]
        [SerializeField] private float recoverySpeed = 1;
        [SerializeField] private float recoverySmoothness = 2;
        
        private float _inertia = 0;
        private float _seed;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private float Magnitude => Mathf.Pow(_inertia, recoverySmoothness);
        
        private void Start() {
            // each instance uses a random seed so that different game objects shake in different patterns
            _seed = Random.value;
            
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        private void Update() {
            if (_inertia == 0) return;
            
            // the new vector assigned is a value type and created locally so it will be allocated on the stack and
            // deallocated as soon as the function exits, this code will not generate memory garbage
            var positionNoise = new Vector3(PerlinNoise1D(0), PerlinNoise1D(1), PerlinNoise1D(2));
            var rotationNoise = new Vector3(PerlinNoise1D(3), PerlinNoise1D(4), PerlinNoise1D(5));
            
            transform.position = _initialPosition + _initialRotation * Vector3.Scale(maxPositionImpulse, positionNoise) * Magnitude;
            transform.rotation = _initialRotation * Quaternion.Euler(Vector3.Scale(maxRotationImpulse, rotationNoise) * Magnitude);

            _inertia = Mathf.Clamp01(_inertia - recoverySpeed * Time.deltaTime);
        }

        // keep the x sample fixed and change y each frame to create 1D Perlin noise, the returned noise in the 0...1
        // range is then mapped to the -1...1 range so that shake travels in both directions on the single axis.
        private float PerlinNoise1D(int axis) => Mathf.PerlinNoise(_seed + axis, Time.time * frequency) * 2 - 1;

        /// <summary>
        /// Shakes the game object by a force in the 0...1 range. Tune the force to create a realistic effect. For
        /// example, distant earthquake can use a force like 0.1, nearby explosions should use the max force of 1.
        /// </summary>
        [Button("Shake")]
        public void Shake(float force = 1) => _inertia = Mathf.Clamp01(_inertia + force);
    }
}