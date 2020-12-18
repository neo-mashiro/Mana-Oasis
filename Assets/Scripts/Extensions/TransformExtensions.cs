using System.Linq;
using UnityEngine;

namespace Extensions {
    
    public static class TransformExtensions {
        /// <summary>
        /// Returns all immediate children, or an empty array of length 0 if not found.
        /// </summary>
        public static Transform[] GetAllDirectChildren(this Transform transform) {
            return transform.Cast<Transform>().ToArray();
        }
        
        /// <summary>
        /// Returns components of all immediate children, or an empty array of length 0 if not found.
        /// </summary>
        public static T[] GetComponentsInDirectChildren<T> (this Transform transform) where T : Component {
            return transform.Cast<Transform>().SelectMany(t => t.GetComponents<T>()).ToArray();
        }
        
    }
}