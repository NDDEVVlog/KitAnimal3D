using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;
using System;

public class AnimalBrain : MonoBehaviour
{
    [SerializeField] private AnimalStats _stats;
    [SerializeField] public StageNode _startingNode;
    [SerializeField] public InteractionUI _uiManager;
    [SerializeField] private List<AnimalMoveData> _availableMoves;
    [SerializeField] public ControlMode _currentControlMode = ControlMode.Manual;

    private AnimalMotor _motor;
    public StageNode _currentNode;

    private CancellationTokenSource _turnCts;
    private bool _isRunningTurn;

    private void Start()
    {
        _motor = GetComponent<AnimalMotor>();
        _motor.Initialize(_stats);
        
        // --- ADDED: Listen for death event from Motor ---
        if (_motor != null)
        {
            _motor.DieEvent.AddListener(OnDeath);
        }

        _currentNode = _startingNode;
    }

    private void OnDestroy()
    {
        // --- ADDED: Cleanup listener ---
        if (_motor != null)
        {
            _motor.DieEvent.RemoveListener(OnDeath);
        }
        _turnCts?.Cancel();
        _turnCts?.Dispose();
    }

    // --- ADDED: Handle Death ---
    private void OnDeath()
    {
        Debug.Log($"[AnimalBrain] {name} died. Cancelling turn.");
        _stats.isDead = true;
        
        // This triggers the cancellation token passed to ExecuteGameLoop
        // causing it to stop waiting for UI or movement immediately.
        _turnCts?.Cancel(); 
    }

    public void BeginTurn()
    {
        if (_isRunningTurn || _stats.isDead) return; // Don't start if dead

        _isRunningTurn = true;
        
        _turnCts?.Cancel();
        _turnCts?.Dispose();
        _turnCts = new CancellationTokenSource();

        // Link with GameObject destruction to ensure safety if object is destroyed
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
            _turnCts.Token, 
            this.GetCancellationTokenOnDestroy()
        ).Token;

        ExecuteGameLoop(linkedToken).Forget();
    }

    public void EndTurn()
    {
        _isRunningTurn = false;
        _turnCts?.Cancel();
    }

    private async UniTaskVoid ExecuteGameLoop(CancellationToken token)
    {
        try
        {
            // --- UPDATED: Check !isDead in loop condition ---
            while (_currentNode != null && 
                   !_currentNode.IsFinalNode && 
                   !_stats.isDead && 
                   !token.IsCancellationRequested)
            {
                // If dead before selecting, stop immediately
                if (_stats.isDead) break;

                (NodeConnection connection, AnimalMoveData move) = await _uiManager.WaitForSelection(
                    _currentNode.Position,
                    _currentNode.Connections,
                    _availableMoves,
                    _currentControlMode,
                    token
                );

                StageNode targetNode = connection.GetResolvedTarget();
                if (targetNode == null) continue;

                await ProcessAction(connection, targetNode, move, token);
                
                // If died during movement, break loop
                if (_stats.isDead || token.IsCancellationRequested) break;

                _currentNode = targetNode;

                if (_currentNode is StageNodeEndPoint stageNodeEndPoint)
                {   
                    Debug.Log($"Reached EndPoint: {stageNodeEndPoint.name}. Invoking OnStageCompleted...");
                    stageNodeEndPoint.OnStageCompleted?.Invoke();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // --- ADDED: Catch cancellation (Death or EndTurn) gracefully ---
            Debug.Log($"[AnimalBrain] Turn execution cancelled for {name}.");
        }
        finally
        {
            _isRunningTurn = false;
        }
    }

    private async UniTask ProcessAction(NodeConnection connection, StageNode target, AnimalMoveData move, CancellationToken token)
    {
        // Double check dead state before processing
        if (_stats.isDead) return;

        float speed = _stats.walkSpeed * move.speedMultiplier;

        switch (move.actionType)
        {
            case ActionType.Jump:
                await _motor.PerformJump(target.Position); // Note: You might want to pass token to Motor methods too
                break;

            case ActionType.Interact:
                if (connection.interactionObject != null)
                {
                    await _motor.MoveTo(connection.interactionObject.transform.position, speed);
                    if(!token.IsCancellationRequested && !_stats.isDead)
                        await _motor.PerformInteraction(connection.interactionObject);
                }
                
                if(!token.IsCancellationRequested && !_stats.isDead)
                    await _motor.MoveTo(target.Position, speed);
                break;

            default:
                await _motor.MoveTo(target.Position, speed);
                break;
        }
    }
}