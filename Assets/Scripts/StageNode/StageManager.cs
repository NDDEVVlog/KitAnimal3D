
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StageManager : MonoBehaviour
{
    [SerializeField] private List<StageModule> _sequence = new List<StageModule>();

    public IReadOnlyList<StageModule> Sequence => _sequence;

    public void AutoLinkSequence()
    {
        if (_sequence.Count < 2) return;

        for (int i = 0; i < _sequence.Count - 1; i++)
        {
            StageModule current = _sequence[i];
            StageModule next = _sequence[i + 1];

            if (current == null || next == null) continue;

            foreach (var exit in current.ExitNodes)
            {
                if (exit.Connections.Any(c => c.GetResolvedTarget() == next.EntryNode)) continue;
                
                exit.AddConnection(new NodeConnection
                {
                    targetType = ConnectionTargetType.Module,
                    targetModule = next,
                    label = $"To {next.ModuleName}"
                });
            }
        }
    }

    public void AlignModules()
    {
        for (int i = 0; i < _sequence.Count - 1; i++)
        {
            StageModule current = _sequence[i];
            StageModule next = _sequence[i + 1];

            if (current == null || next == null || current.ExitNodes.Count == 0) continue;

            StageNode referenceExit = current.ExitNodes[0];
            Vector3 entryLocalOffset = next.EntryNode.transform.position - next.transform.position;
            next.transform.position = referenceExit.Position - entryLocalOffset;
        }
    }
}