
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(StageManager))]
public class StageManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StageManager manager = (StageManager)target;

        GUILayout.Space(15);
        GUI.backgroundColor = Color.cyan;
        
        if (GUILayout.Button("Align Modules in Space", GUILayout.Height(30)))
        {
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Undo.RecordObjects(allTransforms, "Align Modules");
            manager.AlignModules();
        }

        GUILayout.Space(5);
        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("Link Exit-to-Entry Logic", GUILayout.Height(30)))
        {
            manager.AutoLinkSequence();
            EditorUtility.SetDirty(manager);
            
            foreach(var module in manager.Sequence)
            {
                if (module == null) continue;
                foreach(var exit in module.ExitNodes)
                {
                    if (exit != null) EditorUtility.SetDirty(exit);
                }
            }
            
            AssetDatabase.SaveAssets();
        }

        GUI.backgroundColor = Color.white;
    }
}