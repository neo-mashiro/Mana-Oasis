using System;
using System.Collections;
using UnityEngine;

namespace Utilities {
    
    public static class CoroutineScheduler {
        
        /// <summary>
        /// Creates a delayed persistent coroutine to be scheduled every few seconds until explicitly stopped.
        /// </summary>
        public static IEnumerator RepeatSchedule(float delay, float interval, Action callback) {
            yield return new WaitForSeconds(delay);
            var waitTime = new WaitForSeconds(interval);
            
            while (true) {
                callback();
                yield return waitTime;
            }
        }
        
        /// <summary>
        /// Creates a delayed persistent coroutine to be scheduled every few seconds until explicitly stopped.
        /// </summary>
        public static IEnumerator RepeatSchedule<T> (float delay, float interval, Action<T> callback, T parameter) {
            yield return new WaitForSeconds(delay);
            var waitTime = new WaitForSeconds(interval);
            
            while (true) {
                callback(parameter);
                yield return waitTime;
            }
        }

        /// <summary>
        /// Creates a delayed coroutine to be scheduled every few seconds for duration seconds.
        /// </summary>
        public static IEnumerator RepeatSchedule(float delay, float interval, float duration, Action callback) {
            yield return new WaitForSeconds(delay);
            var startTime = Time.time;
            var waitTime = new WaitForSeconds(interval);
            
            while (true) {
                callback();
                yield return waitTime;

                if (Time.time - startTime > duration) {
                    break;
                }
            }
        }

        /// <summary>
        /// Creates a delayed coroutine to be scheduled every few seconds for duration seconds.
        /// </summary>
        public static IEnumerator RepeatSchedule<T> (float delay, float interval, float duration, Action<T> callback, T parameter) {
            yield return new WaitForSeconds(delay);
            var startTime = Time.time;
            var waitTime = new WaitForSeconds(interval);
            
            while (true) {
                callback(parameter);
                yield return waitTime;

                if (Time.time - startTime > duration) {
                    break;
                }
            }
        }
        
    }
}
