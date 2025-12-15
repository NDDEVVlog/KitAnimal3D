using System.Collections.Generic;
using UnityEngine;

public class StageNode : MonoBehaviour
{
    [SerializeField] private List<PathConnection> _connections = new List<PathConnection>();

    public Vector3 Position => transform.position;
    public IReadOnlyList<PathConnection> Connections => _connections;
    public bool IsFinalNode => _connections.Count == 0;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        foreach (var conn in _connections)
        {
            if (conn.targetNode == null) continue;

            Gizmos.color = conn.actionType switch
            {
                ActionType.Jump => Color.red,
                ActionType.Sprint => Color.yellow,
                _ => Color.green
            };

            Vector3 direction = (conn.targetNode.Position - transform.position) * 0.9f;
            Gizmos.DrawRay(transform.position, direction);
            Gizmos.DrawSphere(conn.targetNode.Position, 0.1f);
        }
    }
}