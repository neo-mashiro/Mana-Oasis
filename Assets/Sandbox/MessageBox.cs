using UnityEngine;

namespace Sandbox {
    
    public class MessageBox : MonoBehaviour {
        [SerializeField] private string title = default;
        [SerializeField, Multiline] private string message = default;
        [SerializeField] private string buttonText = "Confirm";

        private bool _showWindow = true;
        private const float Padding = 500f;

        private void OnGUI() {
            if (!_showWindow) { return; }
            
            var size = GUI.skin.label.CalcSize(new GUIContent(message));
            var maxWidth = Mathf.Min(Screen.width - Padding, size.x);
            var left = Screen.width * 0.5f - maxWidth * 0.5f;
            var top = Screen.height * 0.4f - size.y * 0.5f;

            var windowRect = new Rect(left, top, maxWidth, size.y);
            GUILayout.Window(123, windowRect, id => DrawWindow(id, maxWidth), title);
        }

        private void DrawWindow(int id, float maxWidth) {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(message);
            GUILayout.EndVertical();
            if (GUILayout.Button(buttonText)) {
                _showWindow = false;
            }
        }
    }
}