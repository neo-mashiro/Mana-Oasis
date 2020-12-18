using UnityEditor;
using UnityEngine;
using Utilities;

namespace Mana.Editor {
    
    [CustomPropertyDrawer(typeof(ObjectPoolItem))]
    public class ObjectPoolUI : PropertyDrawer {
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            // draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            // calculate rects
            var amountRect = new Rect(position.x, position.y, position.width * 0.4f, position.height);
            var prefabRect = new Rect(position.x + position.width * 0.4f + 5, position.y,
                position.width * 0.6f - 5, position.height);

            // draw the "amount" field
            GUI.backgroundColor = new Color(1f, 0.16f, 0.26f);
            EditorGUIUtility.labelWidth = 48;  // set field label width to fit the label text
            var amountLabel = new GUIContent("Amount", "Specifies the amount of objects to pre-instantiate.");
            EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("amount"), amountLabel);
            // EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("amount"), GUIContent.none);  // hide labels

            // draw the "prefab" field
            GUI.backgroundColor = new Color(0.59f, 0f, 1f);
            EditorGUIUtility.labelWidth = 40;
            EditorGUI.PropertyField(prefabRect, property.FindPropertyRelative("prefab"), new GUIContent("Prefab"));

            // reset indent level and field label width to default
            EditorGUIUtility.labelWidth = 0;
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}