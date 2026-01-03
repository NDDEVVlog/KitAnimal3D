using UnityEngine;
using UnityEditor;
using UnityEditor.Animations; // Required for AnimatorController access
using System.Linq;

public class AnimatorParameterCopier : EditorWindow
{
    private AnimatorController sourceController;
    private AnimatorController targetController;
    private bool overwriteExistingValues = true;

    [MenuItem("Tools/Animation/Animator Parameter Copier")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorParameterCopier>("Param Copier");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy Animator Parameters", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Input Fields
        sourceController = (AnimatorController)EditorGUILayout.ObjectField("Source Controller", sourceController, typeof(AnimatorController), false);
        targetController = (AnimatorController)EditorGUILayout.ObjectField("Target Controller", targetController, typeof(AnimatorController), false);

        EditorGUILayout.Space();

        // Options
        overwriteExistingValues = EditorGUILayout.Toggle(new GUIContent("Overwrite Values", "If the parameter already exists in the target, update its default value to match the source."), overwriteExistingValues);

        EditorGUILayout.Space();

        // Validation and Button
        EditorGUI.BeginDisabledGroup(sourceController == null || targetController == null || sourceController == targetController);
        if (GUILayout.Button("Copy Parameters"))
        {
            CopyParameters();
        }
        EditorGUI.EndDisabledGroup();

        // Help text
        if (sourceController != null && sourceController == targetController)
        {
            EditorGUILayout.HelpBox("Source and Target cannot be the same.", MessageType.Warning);
        }
    }

    private void CopyParameters()
    {
        Undo.RecordObject(targetController, "Copy Animator Parameters");

        int addedCount = 0;
        int updatedCount = 0;

        foreach (AnimatorControllerParameter sourceParam in sourceController.parameters)
        {
            // Check if parameter exists in target
            bool exists = targetController.parameters.Any(p => p.name == sourceParam.name);

            if (!exists)
            {
                // Create new parameter
                targetController.AddParameter(sourceParam.name, sourceParam.type);
                
                // We need to fetch the newly created parameter to set its default value
                AnimatorControllerParameter newParam = targetController.parameters.First(p => p.name == sourceParam.name);
                CopyValues(sourceParam, newParam);
                
                addedCount++;
            }
            else if (overwriteExistingValues)
            {
                // Update existing parameter
                AnimatorControllerParameter targetParam = targetController.parameters.First(p => p.name == sourceParam.name);
                
                // Ensure types match before copying value
                if (targetParam.type == sourceParam.type)
                {
                    CopyValues(sourceParam, targetParam);
                    updatedCount++;
                }
                else
                {
                    Debug.LogWarning($"Skipped '{sourceParam.name}': Exists in target but types do not match ({sourceParam.type} vs {targetParam.type}).");
                }
            }
        }

        // Apply changes to asset
        EditorUtility.SetDirty(targetController);
        AssetDatabase.SaveAssets();

        Debug.Log($"<b>Operation Complete:</b> Added {addedCount} parameters, Updated {updatedCount} parameters.");
    }

    private void CopyValues(AnimatorControllerParameter source, AnimatorControllerParameter target)
    {
        switch (source.type)
        {
            case AnimatorControllerParameterType.Float:
                target.defaultFloat = source.defaultFloat;
                break;
            case AnimatorControllerParameterType.Int:
                target.defaultInt = source.defaultInt;
                break;
            case AnimatorControllerParameterType.Bool:
                target.defaultBool = source.defaultBool;
                break;
            case AnimatorControllerParameterType.Trigger:
                // Triggers don't usually have default values persisted in the editor same way as others, 
                // but we keep the switch for consistency.
                break;
        }
    }
}