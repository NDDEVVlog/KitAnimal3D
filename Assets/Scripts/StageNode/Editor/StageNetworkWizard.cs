using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
public class StageNetworkWizard : EditorWindow
{
    private Vector2 _scrollPosition;
    private List<StageNode> _allNodes = new();
    private List<StageNodeSpawnPoint> _spawnPoints = new();
    private List<StageNodeEndPoint> _endPoints = new();
    private bool _validationPassed = false;
    private string _validationMessage = "";
    [MenuItem("Tools/Stage Network Wizard")]
    public static void ShowWindow()
    {
        GetWindow<StageNetworkWizard>("Stage Wizard");
    }

    private void OnEnable()
    {
        RefreshData();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        DrawHeader();
        DrawStatistics();
        DrawCreationTools();
        DrawConnectionTools();
        DrawValidationSection();

        EditorGUILayout.EndScrollView();
    }

    private void RefreshData()
    {
        _allNodes = FindObjectsByType<StageNode>(FindObjectsSortMode.None).ToList();
        _spawnPoints = FindObjectsByType<StageNodeSpawnPoint>(FindObjectsSortMode.None).ToList();
        _endPoints = FindObjectsByType<StageNodeEndPoint>(FindObjectsSortMode.None).ToList();
        _validationMessage = "";
        _validationPassed = false;
    }

    private void DrawHeader()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Stage Network Manager", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh Data"))
        {
            RefreshData();
        }
        GUILayout.Space(10);
    }

    private void DrawStatistics()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Scene Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Nodes: {_allNodes.Count}");
        EditorGUILayout.LabelField($"Spawn Points: {_spawnPoints.Count}");
        EditorGUILayout.LabelField($"End Points: {_endPoints.Count}");
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private void DrawCreationTools()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Quick Creation", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Node")) CreateNodeObject<StageNode>("New Node");
        if (GUILayout.Button("Create Spawn Point")) CreateNodeObject<StageNodeSpawnPoint>("New SpawnPoint");
        if (GUILayout.Button("Create End Point")) CreateNodeObject<StageNodeEndPoint>("New EndPoint");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private void CreateNodeObject<T>(string name) where T : StageNode
    {
        GameObject go = new GameObject(name);
        go.AddComponent<T>();

        if (SceneView.lastActiveSceneView != null)
        {
            go.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
        }

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        Selection.activeGameObject = go;
        RefreshData();
    }

    private void DrawConnectionTools()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Connection Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select two or more nodes in the scene to connect them sequentially.", MessageType.Info);

        if (GUILayout.Button("Connect Selected Nodes Sequence"))
        {
            ConnectSelectedNodes();
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private void ConnectSelectedNodes()
    {
        var selectedNodes = Selection.gameObjects
            .Select(g => g.GetComponent<StageNode>())
            .Where(n => n != null)
            .OrderBy(n => n.transform.GetSiblingIndex())
            .ToList();

        if (selectedNodes.Count < 2)
        {
            Debug.LogWarning("Select at least 2 nodes to connect.");
            return;
        }

        for (int i = 0; i < selectedNodes.Count - 1; i++)
        {
            StageNode source = selectedNodes[i];
            StageNode target = selectedNodes[i + 1];

            Undo.RecordObject(source, "Connect Nodes");

            NodeConnection newConnection = new NodeConnection
            {
                targetType = ConnectionTargetType.Node,
                targetNode = target,
                specificActionRequirement = ActionType.None
            };

            source.AddConnection(newConnection);
            EditorUtility.SetDirty(source);
        }
        Debug.Log($"Connected {selectedNodes.Count} nodes sequentially.");
    }

    private void DrawValidationSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Graph Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate Paths (Start -> End)"))
        {
            ValidateGraph();
        }

        if (!string.IsNullOrEmpty(_validationMessage))
        {
            MessageType type = _validationPassed ? MessageType.Info : MessageType.Error;
            EditorGUILayout.HelpBox(_validationMessage, type);
        }

        EditorGUILayout.EndVertical();
    }

    private void ValidateGraph()
    {
        RefreshData();

        if (_spawnPoints.Count == 0)
        {
            _validationPassed = false;
            _validationMessage = "No Spawn Points found in the scene!";
            return;
        }

        if (_endPoints.Count == 0)
        {
            _validationPassed = false;
            _validationMessage = "No End Points found in the scene!";
            return;
        }

        List<string> errorLog = new List<string>();
        bool allPathsValid = true;

        foreach (var spawn in _spawnPoints)
        {
            if (!PathExistsToAnyEndPoint(spawn))
            {
                allPathsValid = false;
                errorLog.Add($"SpawnPoint '{spawn.name}' cannot reach any EndPoint.");
                EditorGUIUtility.PingObject(spawn);
            }
        }

        if (allPathsValid)
        {
            _validationPassed = true;
            _validationMessage = "SUCCESS: All Spawn Points can reach an End Point.";
        }
        else
        {
            _validationPassed = false;
            _validationMessage = "FAILURE:\n" + string.Join("\n", errorLog);
        }
    }

    private bool PathExistsToAnyEndPoint(StageNode startNode)
    {
        Queue<StageNode> queue = new Queue<StageNode>();
        HashSet<StageNode> visited = new HashSet<StageNode>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            StageNode current = queue.Dequeue();

            if (current is StageNodeEndPoint) return true;

            if (current.Connections == null) continue;

            foreach (var connection in current.Connections)
            {
                StageNode target = connection.GetResolvedTarget();

                if (target != null && !visited.Contains(target))
                {
                    visited.Add(target);
                    queue.Enqueue(target);
                }
            }
        }

        return false;
    }
}
