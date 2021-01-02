using System.Linq;
using UnityEngine;

namespace Utilities {
    
    public static class UnityExtensions {
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
        
        /// <summary>
        /// Returns true if the layer is in the layer mask.
        /// </summary>
        public static bool Contains(this LayerMask layerMask, int layer) {
            return layerMask == (layerMask | (1 << layer));
        }
    }
}