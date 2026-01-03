using UnityEditor;
using UnityEngine;
using UnityEditor.EditorTools;
using System.Collections.Generic;

[EditorTool("Stage Linker Tool")]
public class StageLinkerTool : EditorTool
{
    private enum EditMode { None, Inspect, Link, Unlink, Create } // ADDED Create
    private EditMode _currentMode = EditMode.None;
    private StageNode _sourceNode;
    private StageModule _activeModule;

    // Defined the Rect for the UI so we can check if the mouse is hovering over it
    private Rect _panelRect = new Rect(10, 10, 220, 210); 

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
        // Prevent default Unity selection box when in Create mode
        if (_currentMode == EditMode.Create)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        DrawOverlayPanel();
        HandleInteraction();
        sceneView.Repaint();
    }

    private void DrawOverlayPanel()
    {
        Handles.BeginGUI();
        
        GUI.Box(_panelRect, GUIContent.none, EditorStyles.helpBox);
        GUILayout.BeginArea(new Rect(_panelRect.x + 10, _panelRect.y + 10, _panelRect.width - 20, _panelRect.height - 20));

        GUILayout.Label("STAGE LINKER SYSTEM", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // INSPECT MODE
        GUI.backgroundColor = _currentMode == EditMode.Inspect ? new Color(0.25f, 0.85f, 1f) : Color.white;
        if (GUILayout.Button("PICK / INSPECT", GUILayout.Height(25))) _currentMode = EditMode.Inspect;

        // CREATE MODE (NEW)
        GUI.backgroundColor = _currentMode == EditMode.Create ? new Color(1f, 0.5f, 0f) : Color.white;
        if (GUILayout.Button("CREATE NODE", GUILayout.Height(25)))
        {
            _currentMode = EditMode.Create;
            _sourceNode = null; // Clear selection when entering create mode
        }

        // LINK MODE
        GUI.backgroundColor = _currentMode == EditMode.Link ? Color.green : Color.white;
        if (GUILayout.Button("LINK MODE", GUILayout.Height(25))) _currentMode = EditMode.Link;

        // UNLINK MODE
        GUI.backgroundColor = _currentMode == EditMode.Unlink ? Color.red : Color.white;
        if (GUILayout.Button("UNLINK MODE", GUILayout.Height(25))) _currentMode = EditMode.Unlink;

        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("RESET TOOL", GUILayout.Height(25))) ResetSelection();

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
        // Don't process scene clicks if mouse is over the UI panel
        if (_panelRect.Contains(Event.current.mousePosition)) return;

        if (_currentMode == EditMode.Create)
        {
            HandleCreateMode();
            return; // Skip drawing connection buttons while in create mode
        }

        if (_currentMode == EditMode.None) return;

        // --- Existing Node/Module Interaction Logic ---
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

        // Only draw dotted “link intent” line in Link/Unlink
        if (_sourceNode != null && (_currentMode == EditMode.Link || _currentMode == EditMode.Unlink))
        {
            Handles.color = _currentMode == EditMode.Link ? Color.green : Color.red;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            // Just project ray out a bit for visual feedback
            Handles.DrawDottedLine(
                _sourceNode.Position,
                ray.GetPoint(5f),
                4f
            );
        }
    }

    private void HandleCreateMode()
    {
        Event e = Event.current;
        
        // Raycast from mouse to world
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        // Try to hit physics objects first (terrain, floors)
        if (Physics.Raycast(ray, out hit))
        {
            DrawCreationPreview(hit.point, hit.normal);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                CreateStageNode(hit.point);
                e.Use(); // Consume the event so we don't deselect things
            }
        }
        else
        {
            // Fallback: If no physics hit, hit the XZ plane at Y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);
                DrawCreationPreview(point, Vector3.up);

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    CreateStageNode(point);
                    e.Use();
                }
            }
        }
    }

    private void DrawCreationPreview(Vector3 position, Vector3 normal)
    {
        Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
        Handles.DrawWireDisc(position, normal, 0.5f);
        Handles.SphereHandleCap(0, position, Quaternion.identity, 0.2f, EventType.Repaint);
        
        Handles.Label(position + Vector3.up * 0.5f, "Click to Spawn", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.yellow } });
    }

    private void CreateStageNode(Vector3 position)
    {
        GameObject go = new GameObject("StageNode");
        go.transform.position = position;
        
        // Add the component
        var node = go.AddComponent<StageNode>();

        // Optional: Parent it to a module if one was previously selected, or just find the nearest
        if (_activeModule != null)
        {
            go.transform.SetParent(_activeModule.transform, true);
        }

        // Register Undo so Ctrl+Z works
        Undo.RegisterCreatedObjectUndo(go, "Create Stage Node");
        
        // Select the new object
        Selection.activeGameObject = go;
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
        if (_currentMode == EditMode.Inspect)
        {
            if (node != null)
            {
                Selection.activeGameObject = node.gameObject;
                EditorGUIUtility.PingObject(node.gameObject);
            }
            else if (module != null)
            {
                Selection.activeGameObject = module.gameObject;
                EditorGUIUtility.PingObject(module.gameObject);
            }
            return;
        }

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
        }
        else if (targetNode != null)
        {
            connection.targetType = ConnectionTargetType.Node;
            connection.targetNode = targetNode;
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

        var field = typeof(StageNode).GetField("_connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

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
        Object obj = EditorUtility.EntityIdToObject(instanceID);
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