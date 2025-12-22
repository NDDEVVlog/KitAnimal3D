using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimatorProTool : MonoBehaviour
{

    public Animator targetAnimator;


    public string selectedClipName;

    // We keep this hidden to avoid the Odin List crash
    [HideInInspector]
    public string[] clipNames = new string[0];


    public void RefreshClipNames()
    {
        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("Assign an Animator with a Controller first.");
            clipNames = new string[0];
            return;
        }

        // Get names, remove duplicates, and sort alphabetically
        clipNames = targetAnimator.runtimeAnimatorController.animationClips
            .Select(clip => clip.name)
            .Distinct()
            .OrderBy(name => name)
            .ToArray();

        Debug.Log($"Found {clipNames.Length} animation names.");
    }

    // This block uses Native Unity code to draw the dropdown
    // This bypasses Odin's broken "MenuTree" logic


    public void PlayByName()
    {
        if (targetAnimator != null && !string.IsNullOrEmpty(selectedClipName))
        {
            targetAnimator.Play(selectedClipName);
            Debug.Log($"Playing: {selectedClipName}");
        }
    }
}