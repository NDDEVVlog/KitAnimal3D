using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AnimalMotor))]
public class AnimalBrain : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AnimalStats _stats;
    [SerializeField] private StageNode _startingNode;
    [SerializeField] private InteractionUI _uiManager;

    private AnimalMotor _motor;
    private StageNode _currentNode;
    private bool _hasSelectedAction;
    private PathConnection _selectedConnection;

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
            StartCoroutine(GameLoop());
        }
    }

    private IEnumerator GameLoop()
    {
        while (_currentNode != null && !_currentNode.IsFinalNode)
        {
            // 1. Wait for player selection
            yield return WaitForSelection();

            // 2. Hide UI
            _uiManager.Hide();

            // 3. Execute Movement
            yield return ExecuteAction(_selectedConnection);

            // 4. Update Node
            _currentNode = _selectedConnection.targetNode;
        }
    }

    private IEnumerator WaitForSelection()
    {
        _hasSelectedAction = false;
        
        _uiManager.ShowOptions(_currentNode.Connections, (choice) => 
        {
            _selectedConnection = choice;
            _hasSelectedAction = true;
        });

        yield return new WaitUntil(() => _hasSelectedAction);
    }

    private IEnumerator ExecuteAction(PathConnection connection)
    {
        switch (connection.actionType)
        {
            case ActionType.Jump:
                yield return _motor.PerformJump(connection.targetNode.Position);
                break;
            
            case ActionType.Sprint:
                yield return _motor.MoveTo(connection.targetNode.Position, true);
                break;

            case ActionType.Walk:
            default:
                yield return _motor.MoveTo(connection.targetNode.Position, false);
                break;
        }
    }
}