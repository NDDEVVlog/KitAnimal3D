using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationExtractor : EditorWindow
{
    private bool deleteOriginalFbx = false;
    private string targetFolderName = "Extracted_Animations";

    [MenuItem("Tools/Animation Extractor")]
    public static void ShowWindow()
    {
        GetWindow<AnimationExtractor>("Anim Extractor");
    }

    void OnGUI()
    {
        GUILayout.Label("Batch Animation Extractor", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Instructions:", EditorStyles.miniLabel);
        GUILayout.Label("1. Select FBX files.", EditorStyles.miniLabel);
        GUILayout.Label("2. Click Extract.", EditorStyles.miniLabel);
        GUILayout.Space(10);

        targetFolderName = EditorGUILayout.TextField("Output Folder Name", targetFolderName);

        GUI.color = new Color(1f, 0.5f, 0.5f);
        deleteOriginalFbx = EditorGUILayout.Toggle("Delete Extra FBXs?", deleteOriginalFbx);
        GUI.color = Color.white;

        GUILayout.Space(20);

        if (GUILayout.Button("Extract Animations", GUILayout.Height(40)))
        {
            ExtractAnimations();
        }
    }

    void ExtractAnimations()
    {
        Object[] selection = Selection.objects;

        if (selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select files first.", "OK");
            return;
        }

        string activeDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(selection[0]));
        string outputDir = Path.Combine(activeDir, targetFolderName);

        if (!AssetDatabase.IsValidFolder(outputDir))
        {
            AssetDatabase.CreateFolder(activeDir, targetFolderName);
        }

        int processedCount = 0;
        string survivorPath = string.Empty;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (Object obj in selection)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                if (importer is ModelImporter)
                {
                    if (string.IsNullOrEmpty(survivorPath))
                    {
                        survivorPath = assetPath;
                    }

                    Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

                    foreach (Object asset in allAssets)
                    {
                        if (asset is AnimationClip clip)
                        {
                            if (clip.name.StartsWith("__preview__")) continue;

                            AnimationClip newClip = new AnimationClip();
                            EditorUtility.CopySerialized(clip, newClip);

                            string fbxName = Path.GetFileNameWithoutExtension(assetPath);
                            newClip.name = fbxName;

                            string newPath = Path.Combine(outputDir, fbxName + ".anim");
                            newPath = newPath.Replace("\\", "/");
                            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                            AssetDatabase.CreateAsset(newClip, newPath);
                            processedCount++;
                        }
                    }

                    if (deleteOriginalFbx && assetPath != survivorPath)
                    {
                        AssetDatabase.MoveAssetToTrash(assetPath);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Complete", $"Extracted {processedCount} animations. Kept file: {Path.GetFileName(survivorPath)}", "OK");
    }
}