
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AnimalBrain : MonoBehaviour
{
    [SerializeField] private AnimalStats _stats;
    [SerializeField] private StageNode _startingNode;
    [SerializeField] private InteractionUI _uiManager;
    [SerializeField] private List<AnimalMoveData> _availableMoves;

    private AnimalMotor _motor;
    private StageNode _currentNode;

    private void Start()
    {
        _motor = GetComponent<AnimalMotor>();
        _motor.Initialize(_stats);
        _currentNode = _startingNode;
        ExecuteGameLoop().Forget();
    }

    private async UniTaskVoid ExecuteGameLoop()
    {
        var token = this.GetCancellationTokenOnDestroy();

        while (_currentNode != null && !_currentNode.IsFinalNode)
        {
            (NodeConnection connection, AnimalMoveData move) = await _uiManager.WaitForSelection(
                _currentNode.Connections, 
                _availableMoves, 
                token
            );

            StageNode targetNode = connection.GetResolvedTarget();
            if (targetNode == null) continue;

            await ProcessAction(connection, targetNode, move);
            _currentNode = targetNode;
        }
    }

    private async UniTask ProcessAction(NodeConnection connection, StageNode target, AnimalMoveData move)
    {
        float speed = _stats.walkSpeed * move.speedMultiplier;

        switch (move.actionType)
        {
            case ActionType.Jump:
                await _motor.PerformJump(target.Position);
                break;

            case ActionType.Interact:
                if (connection.interactionObject != null)
                {
                    await _motor.MoveTo(connection.interactionObject.transform.position, speed);
                    await _motor.PerformInteraction(connection.interactionObject);
                }
                await _motor.MoveTo(target.Position, speed);
                break;

            default:
                await _motor.MoveTo(target.Position, speed);
                break;
        }
    }
}