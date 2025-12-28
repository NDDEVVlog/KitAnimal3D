using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DetectUIElements();
        }
    }

    private void DetectUIElements()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("UIDebugger: No EventSystem found in scene.");
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            Debug.Log($"<b>[UI Debugger] Hit {results.Count} elements (Top to Bottom):</b>");
            
            foreach (var result in results)
            {
                string color = result.gameObject.GetComponent<Button>() ? "green" : "white";
                string hierarchy = GetHierarchyPath(result.gameObject.transform);
                
                Debug.Log($"<color={color}>• {result.gameObject.name}</color>\n" +
                          $"   <i>Depth: {result.depth} | SortingLayer: {result.sortingLayer} | Order: {result.sortingOrder}</i>\n" +
                          $"   Path: {hierarchy}");
            }
        }
        else
        {
            Debug.Log("<b>[UI Debugger] No UI elements hit.</b>");
        }
    }

    private string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}