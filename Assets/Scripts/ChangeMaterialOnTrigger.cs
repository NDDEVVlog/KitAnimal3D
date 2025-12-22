using UnityEngine;
using System.Collections.Generic;

public class ChangeMaterialOnTrigger : MonoBehaviour
{
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private string colorPropertyName = "_Color"; // Standard Shader uses _Color

    public List<SkinnedMeshRenderer> cachedRenderers = new List<SkinnedMeshRenderer>();
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        InitializeRenderers();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void InitializeRenderers()
    {
        Debug.Log($"Initialized: Found {cachedRenderers.Count} renderers on {gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {   
        Debug.Log($"Something entered the trigger: {other.name} with tag {other.tag}");
        
        if (!other.CompareTag(targetTag)) 
        {
            Debug.LogWarning($"Tag mismatch! Expected {targetTag}, got {other.tag}");
            return;
        }

        // NEW: Apply color to the entering object, not this trigger object
        ApplyColorToObject(other.gameObject, targetColor);
    }

    public void ApplyColorToObject(GameObject targetObject, Color color)
    {
        var targetRenderers = new List<SkinnedMeshRenderer>();
        if (includeChildren)
            targetRenderers.AddRange(targetObject.GetComponentsInChildren<SkinnedMeshRenderer>());
        else if (targetObject.TryGetComponent<SkinnedMeshRenderer>(out var rootRenderer))
            targetRenderers.Add(rootRenderer);

        if (targetRenderers.Count == 0)
        {
            Debug.LogWarning($"No renderers found on {targetObject.name}");
            return;
        }

        foreach (var renderer in targetRenderers)
        {
            if (renderer == null) continue;

            // Important: Get existing block first to preserve other overrides
            renderer.GetPropertyBlock(propertyBlock);
            // Material material = renderer.material;
            // material.SetColor(colorPropertyName, color);
            // Debug.Log( material.GetColor(colorPropertyName));
            propertyBlock.SetColor(colorPropertyName, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        Debug.Log($"<color=green>Color applied to {targetObject.name} successfully!</color>");
    }
    public void ResetToDefault()
    {
        foreach (var renderer in cachedRenderers)
        {
            if (renderer == null) continue;
            renderer.SetPropertyBlock(null);
        }
    }
}