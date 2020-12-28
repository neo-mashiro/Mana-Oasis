using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Utilities;

namespace Managers {
    
    /// <summary>
    /// [!Caution] This is the sealed singleton class for game management.
    /// </summary>
    public sealed class GameManager : MonoSingleton<GameManager> {
        
        [SerializeField] private GameObject loadingScreenPanel;
        [SerializeField] private float loadingScreenFadingSpeed = 2f;

        private Image _loadingScreenImage;
        private Slider _loadingBar;
        private TextMeshProUGUI _loadingBarText;
        private CanvasGroup _loadingScreenCanvasGroup;

        protected override void OnAwake() {
            _loadingScreenImage = loadingScreenPanel.GetComponent<Image>();
            _loadingBar = loadingScreenPanel.GetComponentInChildren<Slider>();
            _loadingBarText = loadingScreenPanel.GetComponentInChildren<TextMeshProUGUI>();
            _loadingScreenCanvasGroup = loadingScreenPanel.GetComponent<CanvasGroup>();
            
            loadingScreenPanel.SetActive(false);
        }

        /// <summary>
        /// Switches from the current scene to a new scene, when the new scene is fully loaded, moves the specified
        /// array of game objects to the new scene and unloads the current scene. The game objects to move must be
        /// root, not children of any other game objects.
        /// </summary>
        public void SwitchScene(string sceneName, GameObject[] gameObjects = null) {
            var currentScene = SceneManager.GetActiveScene();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
                var newScene = SceneManager.GetSceneByName(sceneName);
                foreach (var go in gameObjects) {
                    SceneManager.MoveGameObjectToScene(go.gameObject, newScene);
                }

                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.UnloadSceneAsync(currentScene);
            }
        }

        /// <summary>
        /// Starts a coroutine that smoothly switches from the current scene to a new scene by displaying a loading
        /// screen in between while the new scene is initializing. The loading screen canvas always has the highest
        /// sort order to be rendered on the very top, it must also be a mono singleton which persists across scenes.
        /// </summary>
        public void SwitchSceneWithLoadingScreen(string sceneName) {
            ResetLoadingScreen(sceneName);
            StartCoroutine(SmoothTransitionToScene(sceneName));
        }

        private void ResetLoadingScreen(string sceneName) {
            _loadingScreenCanvasGroup.alpha = 0;  // reset the canvas group to transparent
            _loadingScreenImage.sprite = Resources.Load<Sprite>($"Loading Screens/{sceneName}");
            _loadingBar.value = 0;
            _loadingBarText.SetText("{0}%", 0);
            
            loadingScreenPanel.SetActive(true);
        }
        
        private IEnumerator SmoothTransitionToScene(string sceneName) {
            // the loading screen fades in and hides the current scene
            while (_loadingScreenCanvasGroup.alpha < 1) {
                _loadingScreenCanvasGroup.alpha += 0.02f * loadingScreenFadingSpeed;
                yield return null;
            }
            
            // cache the current scene and async load the new scene in background
            var currentScene = SceneManager.GetActiveScene();
            var loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            // smoothly progress the loading bar (fake), we have to fake here since there's no way to track the real
            // loading progress in Unity, the loadOperation.progress property cannot report values between 0 and 0.9
            while (!loadOperation.isDone) {
                _loadingBar.value += Time.unscaledDeltaTime;
                _loadingBar.value = Mathf.Min(_loadingBar.value, 1);
                _loadingBarText.SetText("{0}%", (int) (_loadingBar.value * 100f));
                yield return null;
            }
            
            // if the loading bar hasn't yet hit 100% when the new scene is already loaded, progress it until 100%
            while (_loadingBar.value < 1) {
                _loadingBar.value += Time.unscaledDeltaTime * 1.5f;
                _loadingBarText.SetText("{0}%", (int) (_loadingBar.value * 100f));
                yield return null;
            }
            
            // now that the new scene is loaded and loading bar hits 100%, unload the previous scene in background
            SceneManager.UnloadSceneAsync(currentScene);
                
            // the loading screen fades out and the new scene gets rendered
            while (_loadingScreenCanvasGroup.alpha > 0) {
                _loadingScreenCanvasGroup.alpha -= 0.02f * loadingScreenFadingSpeed;
                yield return null;
            }

            loadingScreenPanel.SetActive(false);
        }
    }
}
