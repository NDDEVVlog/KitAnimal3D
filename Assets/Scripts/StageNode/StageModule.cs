
using UnityEngine;
using System.Collections.Generic;

public class StageModule : MonoBehaviour
{
    [SerializeField] private StageNode _entryNode;
    [SerializeField] private List<StageNode> _exitNodes = new List<StageNode>();
    [SerializeField] private string _moduleName;

    public StageNode EntryNode => _entryNode;
    public IReadOnlyList<StageNode> ExitNodes => _exitNodes;
    public string ModuleName => string.IsNullOrEmpty(_moduleName) ? name : _moduleName;

    private void OnDrawGizmos()
    {
        if (_entryNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_entryNode.Position, Vector3.one * 0.4f);
        }

        Gizmos.color = Color.red;
        foreach (var exit in _exitNodes)
        {
            if (exit != null) Gizmos.DrawWireCube(exit.Position, Vector3.one * 0.4f);
        }
    }
}