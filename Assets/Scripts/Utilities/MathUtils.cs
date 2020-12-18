using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utilities {
    
    public static class MathUtils {

        /// <summary>
        /// Returns a framerate-independent t for lerp, slerp and other interpolations given the percentage of
        /// distance to cover per second.
        /// </summary>
        public static float EasePercent(float percentPerSecond, float deltaTime = 0f) {
            if (deltaTime == 0f) {
                deltaTime = Time.deltaTime;
            }

            return 1 - Mathf.Pow(1 - percentPerSecond, deltaTime);
        }
        
        /// <summary>
        /// Returns a framerate-independent t for lerp, slerp and other interpolations for the given sharpness
        /// (the opposite of smoothness).
        /// </summary>
        public static float EaseFactor(float sharpness, float deltaTime = 0f) {
            if (deltaTime == 0f) {
                deltaTime = Time.deltaTime;
            }

            return 1 - Mathf.Exp(-sharpness * deltaTime);
        }
    }
}
