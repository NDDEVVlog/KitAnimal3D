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
        
        if (_motor != null)
        {
            _motor.DieEvent.AddListener(OnDeath);
        }

        _currentNode = _startingNode;
    }

    private void OnDestroy()
    {
        if (_motor != null)
        {
            _motor.DieEvent.RemoveListener(OnDeath);
        }
        _turnCts?.Cancel();
        _turnCts?.Dispose();
    }

    private void OnDeath()
    {
        Debug.Log($"[AnimalBrain] {name} died. Cancelling turn.");
        _stats.isDead = true;
        _turnCts?.Cancel(); 
    }

    public void BeginTurn()
    {
        if (_isRunningTurn || _stats.isDead) return;

        _isRunningTurn = true;
        
        _turnCts?.Cancel();
        _turnCts?.Dispose();
        _turnCts = new CancellationTokenSource();

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
    // FIX 1: Lưu tên lại trước khi chạy logic. 
    // Nếu object bị destroy, biến string này vẫn tồn tại trong bộ nhớ RAM bình thường.
    string myName = this.name; 

    try 
    {   
        Debug.Log("Execute GameLoop");
        while (_currentNode != null && 
               !_currentNode.IsFinalNode && 
               !_stats.isDead && 
               !token.IsCancellationRequested)
        {
            // ... (Phần logic giữ nguyên) ...
            
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
        // FIX 2: Sử dụng 'myName' thay vì 'name'
        Debug.Log($"[AnimalBrain] Turn execution cancelled for {myName}.");
    }
    catch (Exception ex)
    {
        // Nên có thêm catch Exception chung để bắt các lỗi khác không phải Cancel
        // Kiểm tra xem object còn sống không trước khi log
        if (this != null)
        {
            Debug.LogException(ex);
        }
    }
    finally
    {
        _isRunningTurn = false;
    }
}

    private async UniTask ProcessAction(NodeConnection connection, StageNode target, AnimalMoveData move, CancellationToken token)
    {
        if (_stats.isDead) return;

        // Determine Speed
        float speed = _stats.walkSpeed; // Default
        if (move.actionType == ActionType.Sprint) speed = _stats.sprintSpeed;
        if (move.actionType == ActionType.Swim) speed = _stats.swimSpeed;

        switch (move.actionType)
        {
            case ActionType.Jump:
                await _motor.PerformJump(target.Position);
                break;
            
            // ADDED: Swim Case
            case ActionType.Swim:
                await _motor.PerformSwim(target.Position, speed);
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
                // Walk or Sprint
                await _motor.MoveTo(target.Position, speed);
                break;
        }
    }
}