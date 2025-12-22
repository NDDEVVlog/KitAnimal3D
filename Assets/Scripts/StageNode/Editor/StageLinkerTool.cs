using UnityEditor;
using UnityEngine;
using UnityEditor.EditorTools;
using System.Collections.Generic;

[EditorTool("Stage Linker Tool")]
public class StageLinkerTool : EditorTool
{
    private enum EditMode { None, Link, Unlink }
    private EditMode _currentMode = EditMode.None;
    private StageNode _sourceNode;
    private StageModule _activeModule;

    public override GUIContent toolbarIcon => new GUIContent("Stage Linker", "Tool to manage StageNode connections");

    public override void OnActivated()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public override void OnWillBeDeactivated()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        ResetSelection();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        DrawOverlayPanel();
        HandleInteraction();
        sceneView.Repaint();
    }

    private void DrawOverlayPanel()
    {
        Handles.BeginGUI();
        var rect = new Rect(10, 10, 220, 140);
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        GUILayout.BeginArea(new Rect(20, 20, 180, 120));
        
        GUILayout.Label("STAGE LINKER SYSTEM", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        GUI.backgroundColor = _currentMode == EditMode.Link ? Color.green : Color.white;
        if (GUILayout.Button("LINK MODE", GUILayout.Height(25))) _currentMode = EditMode.Link;

        GUI.backgroundColor = _currentMode == EditMode.Unlink ? Color.red : Color.white;
        if (GUILayout.Button("UNLINK MODE", GUILayout.Height(25))) _currentMode = EditMode.Unlink;

        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("RESET SELECTION", GUILayout.Height(25))) ResetSelection();

        if (_sourceNode != null)
        {
            GUILayout.Space(5);
            GUI.color = Color.yellow;
            GUILayout.Label($"Source: {_sourceNode.name}", EditorStyles.miniBoldLabel);
            GUI.color = Color.white;
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void HandleInteraction()
    {
        if (_currentMode == EditMode.None) return;

        var modules = Object.FindObjectsByType<StageModule>(FindObjectsSortMode.None);
        var nodes = Object.FindObjectsByType<StageNode>(FindObjectsSortMode.None);

        foreach (var node in nodes)
        {
            if (node == null) continue;

            float size = _sourceNode == node ? 0.6f : 0.4f;
            Handles.color = GetNodeColor(node);

            if (Handles.Button(node.Position, Quaternion.identity, size, size, Handles.SphereHandleCap))
            {
                OnObjectPicked(node, node.GetComponentInParent<StageModule>());
            }
        }

        foreach (var module in modules)
        {
            if (module == null) continue;

            Handles.color = new Color(1, 1, 0, 0.2f);
            if (Handles.Button(module.transform.position, Quaternion.identity, 0.8f, 0.8f, Handles.CubeHandleCap))
            {
                OnObjectPicked(null, module);
            }
        }

        if (_sourceNode != null)
        {
            Handles.color = _currentMode == EditMode.Link ? Color.green : Color.red;
            Handles.DrawDottedLine(_sourceNode.Position, HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(2f), 4f);
        }
    }

    private Color GetNodeColor(StageNode node)
    {
        if (node == _sourceNode) return Color.yellow;
        
        var module = node.GetComponentInParent<StageModule>();
        if (module != null && module.EntryNode == node) return Color.green;
        
        return Color.red;
    }

    private void OnObjectPicked(StageNode node, StageModule module)
    {
        if (_sourceNode == null)
        {
            if (node != null) _sourceNode = node;
            else if (module != null && module.ExitNodes.Count > 0) _sourceNode = module.ExitNodes[0];
            _activeModule = module;
        }
        else
        {
            if (_currentMode == EditMode.Link) ApplyLink(_sourceNode, node, module);
            else if (_currentMode == EditMode.Unlink) ApplyUnlink(_sourceNode, node, module);

            if (!Event.current.shift) ResetSelection();
        }
    }

    private void ApplyLink(StageNode source, StageNode targetNode, StageModule targetModule)
    {
        if (source == targetNode) return;
        
        Undo.RecordObject(source, "Link Stage Node");

        var connection = new NodeConnection();
        if (targetModule != null && targetNode == null)
        {
            connection.targetType = ConnectionTargetType.Module;
            connection.targetModule = targetModule;
            connection.label = $"To {targetModule.ModuleName}";
        }
        else if (targetNode != null)
        {
            connection.targetType = ConnectionTargetType.Node;
            connection.targetNode = targetNode;
            connection.label = $"To {targetNode.name}";
        }

        if (connection.GetResolvedTarget() != null)
        {
            source.AddConnection(connection);
            EditorUtility.SetDirty(source);
        }
    }

    private void ApplyUnlink(StageNode source, StageNode targetNode, StageModule targetModule)
    {
        Undo.RecordObject(source, "Unlink Stage Node");
        
        var field = typeof(StageNode).GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (List<NodeConnection>)field.GetValue(source);

        list.RemoveAll(c => 
            (targetNode != null && c.targetNode == targetNode) || 
            (targetModule != null && c.targetModule == targetModule)
        );

        EditorUtility.SetDirty(source);
    }

    private void ResetSelection()
    {
        _sourceNode = null;
        _activeModule = null;
        _currentMode = EditMode.None;
    }
}

[InitializeOnLoad]
public static class StageHierarchyHighlighter
{
    static StageHierarchyHighlighter()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        Object obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is not GameObject target) return;

        if (target.TryGetComponent<StageNode>(out _))
        {
            DrawTag(selectionRect, "NODE", new Color(0.2f, 0.8f, 1f));
        }
        else if (target.TryGetComponent<StageModule>(out _))
        {
            DrawTag(selectionRect, "MODULE", new Color(0.2f, 1f, 0.4f));
        }
    }

    private static void DrawTag(Rect rect, string label, Color color)
    {
        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = color },
            fontStyle = FontStyle.Bold
        };

        Rect labelRect = new Rect(rect.xMax - 65, rect.y, 60, rect.height);
        EditorGUI.LabelField(labelRect, label, style);
    }
}