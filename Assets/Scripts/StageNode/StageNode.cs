
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ConnectionTargetType { Node, Module }

[Serializable]
public struct NodeConnection
{
    public ConnectionTargetType targetType;
    public StageNode targetNode;
    public StageModule targetModule;
    public string label;
    public Interactable interactionObject;

    public StageNode GetResolvedTarget()
    {
        return targetType switch
        {
            ConnectionTargetType.Node => targetNode,
            ConnectionTargetType.Module => targetModule != null ? targetModule.EntryNode : null,
            _ => null
        };
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

            Gizmos.color = connection.targetType == ConnectionTargetType.Module ? Color.green : Color.cyan;
            Gizmos.DrawLine(transform.position, target.Position);
            Gizmos.DrawSphere(target.Position, 0.12f);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}