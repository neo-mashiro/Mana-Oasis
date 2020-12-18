using System;
using UnityEngine;
using UnityEditor;

namespace Mana.Editor {
    
    public class InstanceIDSearchWindow : EditorWindow {
        
        private string _input = "";
    
        [MenuItem("Window/Search Object By Instance ID")]
        private static void Init() {
            var windowRect = new Rect(Screen.width / 2f - 150, Screen.height / 2f - 60, 300, 120);
            var window = (InstanceIDSearchWindow) GetWindowWithRect(typeof(InstanceIDSearchWindow),
                windowRect, false, "Search Window");
            window.Show();
        }

        private void OnGUI() {
            GUILayout.Space(10);
            GUILayout.Label("Search Game Object / Component By Instance ID", EditorStyles.boldLabel);
            GUILayout.Space(20);
            _input = EditorGUILayout.TextField("Enter Instance ID", _input,
                new GUIStyle("SearchTextField"));
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Search")) {
                if (Int32.TryParse(_input, out var instanceID)) {
                    var all = FindObjectsOfType(typeof(UnityEngine.Object));
                    
                    foreach (var obj in all) {
                        if (obj.GetInstanceID() == instanceID) {
                            if (obj is GameObject gameObject) {
                                Selection.activeGameObject = gameObject;
                            }
                            else if (obj is Component component) {
                                Selection.activeGameObject = component.gameObject;
                            }
                            return;
                        }
                    }
                    Debug.LogError("Cannot find an object or component with the entered ID.");
                }
                else {
                    Debug.LogError("Please enter a valid number.");
                }
            }
        }
    }
}