using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(AnimalMotor))]
public class AnimalBrain : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AnimalStats _stats;
    [SerializeField] private StageNode _startingNode;
    [SerializeField] private InteractionUI _uiManager;

    private AnimalMotor _motor;
    [SerializeField] private StageNode _currentNode;

    private void Awake()
    {
        _motor = GetComponent<AnimalMotor>();
    }

    private void Start()
    {
        _motor.Initialize(_stats);
        if (_startingNode != null)
        {
            transform.position = _startingNode.Position;
            _currentNode = _startingNode;
            
            // Fire and forget the async loop
            GameLoop().Forget();
        }
    }

    private async UniTaskVoid GameLoop()
    {
        // Get token to stop loop if object is destroyed
        var token = this.GetCancellationTokenOnDestroy();

        while (_currentNode != null && !_currentNode.IsFinalNode)
        {
            // 1. Wait for player selection
            PathConnection selectedConnection = await WaitForSelection(token);

            // 2. Hide UI
            _uiManager.Hide();

            // 3. Execute Movement
            await ExecuteAction(selectedConnection);

            // 4. Update Node
            _currentNode = selectedConnection.targetNode;
        }
    }

    private async UniTask<PathConnection> WaitForSelection(System.Threading.CancellationToken token)
    {
        // Create a source that completes when the UI is clicked
        var completionSource = new UniTaskCompletionSource<PathConnection>();

        // Register cancellation in case object is destroyed while waiting
        using (token.Register(() => completionSource.TrySetCanceled()))
        {
            _uiManager.ShowOptions(_currentNode.Connections, (choice) => 
            {
                // This fulfills the task
                completionSource.TrySetResult(choice);
            });

            return await completionSource.Task;
        }
    }

    private async UniTask ExecuteAction(PathConnection connection)
    {
        switch (connection.actionType)
        {
            case ActionType.Jump:
                await _motor.PerformJump(connection.targetNode.Position);
                break;
            
            case ActionType.Sprint:
                await _motor.MoveTo(connection.targetNode.Position, true);
                break;

            case ActionType.Interact:
                Debug.Log("Executing Interaction Action : " + connection.interactionObject.name);
                if (connection.interactionObject != null)
                {
                    await _motor.MoveTo(connection.interactionObject.transform.position, false);
                    await _motor.PerformInteraction(connection.interactionObject);
                }
                else
                {
                    Debug.LogError("Interaction selected but no Object assigned in StageNode!");
                }
                break;

            case ActionType.Walk:
            default:
                await _motor.MoveTo(connection.targetNode.Position, false);
                break;
        }
    }
}