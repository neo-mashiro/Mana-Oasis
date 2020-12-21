using UnityEngine;
// ReSharper disable StaticMemberInGenericType

namespace Utilities {
    
    /// <summary>
    /// An abstract mono singleton class from which you can create various types of singleton component.
    /// Beyond global access, this singleton persists across scenes, enforces uniqueness, ensures thread safety,
    /// supports lazy instantiation and comes with a virtual OnAwake() method that you can override with your own
    /// custom initialization code.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : Component {
	
        private static bool _quit = false;
        private static readonly object Mutex = new object();  // exclusive access for thread safety
        
        private static T _instance;

        public static T Instance {
            get {
                // prevent access to the singleton instance when the application quits
                if (_quit) {
                    return null;
                }

                lock (Mutex) {
                    if (_instance == null) {
                        _instance = FindObjectOfType<T>();
				
                        if (_instance == null) {
                            var newGameObject = new GameObject($"[{typeof(T).Name}]");
                            _instance = newGameObject.AddComponent<T>();
                            DontDestroyOnLoad(newGameObject);
                        }
                    }
                    return _instance;
                }
            }
        }
        
        private void OnApplicationQuit() => _quit = true;
        private void OnDestroy() => _quit = true;
        
        private void Awake() {
            if (_instance == null) {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnAwake();
            }
            else {
                Destroy(gameObject);
            }
        }
	
        /// <summary>
        /// Override this function to include your custom initialization code.
        /// </summary>
        protected virtual void OnAwake() { }
    }
}