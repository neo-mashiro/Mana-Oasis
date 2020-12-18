using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities {
    
    [Serializable]
    public class ObjectPoolItem {
        public int amount;
        public GameObject prefab;
    }
    
    /// <summary>
    /// Pre-instantiates reusable game objects to be used at runtime.
    /// </summary>
    public class ObjectPool : MonoBehaviour {
        
        public ObjectPool Pool { get; private set; }

        [SerializeField] private List<ObjectPoolItem> items = default;
        
        private Dictionary<string, Queue<GameObject>> _pool;

        private void Awake() => Pool = this;

        private void Start() {
            _pool = new Dictionary<string, Queue<GameObject>>();
            
            foreach (var item in items) {
                var queue = new Queue<GameObject>();
                
                for (var i = 0; i < item.amount; i++) {
                    var instance = Instantiate(item.prefab, transform, true);
                    instance.SetActive(false);
                    queue.Enqueue(instance);
                }
                
                _pool.Add(item.prefab.tag, queue);
            }
        }

        /// <summary>
        /// Fetches and activates an instance from the object pool. Once activated, the fetched instance will be pushed
        /// back to the end of the queue for reuse. The instance prefab must handle recycling itself at a delayed time.
        /// This operation takes O(1) time.
        /// </summary>
        public GameObject Fetch(string tag) {
            if (!_pool.ContainsKey(tag)) {
                throw new ArgumentOutOfRangeException(tag);
            }
            
            var instance = _pool[tag].Dequeue();
            instance.SetActive(true);
            _pool[tag].Enqueue(instance);

            return instance;
        }
    }
}
