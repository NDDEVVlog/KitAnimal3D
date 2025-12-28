using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ConnectionTargetType { Node, Module }

[Serializable]
public struct NodeConnection
{
    public ConnectionTargetType targetType;
    public StageNode targetNode;
    public StageModule targetModule;
    public Interactable interactionObject;
    
    [Tooltip("Select 'None' to allow any move type.")]
    public ActionType specificActionRequirement;

    public StageNode GetResolvedTarget()
    {
        return targetType switch
        {
            ConnectionTargetType.Node => targetNode,
            ConnectionTargetType.Module => targetModule != null ? targetModule.EntryNode : null,
            _ => null
        };
    }

    public bool IsActionAllowed(ActionType action)
    {
        if (specificActionRequirement == ActionType.None) return true;
        return specificActionRequirement == action;
    }
}

public class StageNode : MonoBehaviour
{
    [SerializeField] private List<NodeConnection> _connections = new List<NodeConnection>();
    
    public IReadOnlyList<NodeConnection> Connections => _connections;
    public Vector3 Position => transform.position;
    public bool IsFinalNode => _connections.Count == 0;

    public void AddConnection(NodeConnection connection) => _connections.Add(connection);

    private void OnDrawGizmos()
    {
        foreach (var connection in _connections)
        {
            StageNode target = connection.GetResolvedTarget();
            if (target == null) continue;

            Gizmos.color = connection.specificActionRequirement != ActionType.None ? Color.magenta : Color.cyan;
            Gizmos.DrawLine(transform.position, target.Position);
            Gizmos.DrawSphere(target.Position, 0.12f);

#if UNITY_EDITOR
            Vector3 midPoint = (transform.position + target.Position) * 0.5f;
            float distance = Vector3.Distance(transform.position, target.Position);
            
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;

            Handles.Label(midPoint, $"{distance:F1}m", style);
#endif
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}