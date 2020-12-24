 using UnityEngine;
 using UnityEngine.SceneManagement;

 namespace Utilities {
     
      public static class SceneUtils {

         private static GameObject[] _gameObjectsToMove;
         private static string _newSceneName;
         private static Scene _currentScene;

         /// <summary>
         /// Additively loads a new scene on top of the current scene, when the new scene is loaded, moves the array of
         /// game objects to the new scene and unloads the current scene. The game objects to move must be root, not
         /// children of any other game objects.<br/>
         /// <br/>
         /// [Caution] Using this function will have both scenes loaded in memory during transition, avoid using it if
         /// you might be low on memory especially when you have very large scenes.
         /// </summary>
         public static void LoadSceneWithGameObjects(string sceneName, GameObject[] gameObjects) {
             _gameObjectsToMove = gameObjects;
             _newSceneName = sceneName;
             _currentScene = SceneManager.GetActiveScene();

             SceneManager.sceneLoaded += OnSceneLoaded;
             SceneManager.LoadSceneAsync(_newSceneName, LoadSceneMode.Additive);
         }

         private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
             var newScene = SceneManager.GetSceneByName(_newSceneName);
             foreach (var go in _gameObjectsToMove) {
                 SceneManager.MoveGameObjectToScene(go.gameObject, newScene);
             }

             SceneManager.sceneLoaded -= OnSceneLoaded;
             SceneManager.UnloadSceneAsync(_currentScene);
         }
     }
 }