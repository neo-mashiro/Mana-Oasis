using Players;
using UnityEditor;

[CustomEditor(typeof(MotionState))]
public class HideScriptFieldInEditor : Editor {
    
    private static readonly string[] FieldsToExclude = { "m_Script" };
     
    public override void OnInspectorGUI() {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, FieldsToExclude);
        serializedObject.ApplyModifiedProperties();
    }
}