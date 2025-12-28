using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class CopyComponentsTool
{
    static Component[] clipboard;

    [MenuItem("GameObject/Tools/Copy All Components", false, 0)]
    static void Copy()
    {
        if (Selection.activeGameObject == null) return;
        clipboard = Selection.activeGameObject.GetComponents<Component>();
    }

    [MenuItem("GameObject/Tools/Paste All Components", false, 1)]
    static void Paste()
    {
        if (Selection.activeGameObject == null || clipboard == null) return;

        GameObject target = Selection.activeGameObject;
        
        // Register undo so you can Ctrl+Z if it goes wrong
        Undo.RegisterCompleteObjectUndo(target, "Paste All Components");

        foreach (Component comp in clipboard)
        {
            // Skip Transform as every object already has one
            if (comp is Transform) continue;

            ComponentUtility.CopyComponent(comp);
            ComponentUtility.PasteComponentAsNew(target);
        }
    }
}