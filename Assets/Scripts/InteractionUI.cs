using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;

public enum ControlMode
{
    Manual,
    RandomWithInput
}

public class InteractionUI : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _targetButtonPrefab; 
    [SerializeField] private GameObject _moveButtonPrefab;
    //[SerializeField] private GameObject _randomModePrompt;

    [Header("Containers")]
    [SerializeField] private Transform _moveContainer;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Canvas _worldCanvas;

    [Header("World UI Settings")]
    [SerializeField] private Vector3 _offset = Vector3.up * 1.5f;
    [Range(0.001f, 1f)]
    [SerializeField] private float _scaleMultiplier = 0.02f;
    [SerializeField] private float _minScale = 0.1f;
    [SerializeField] private float _maxScale = 2.0f;

    private NodeConnection? _selectedTarget;
    private AnimalMoveData _selectedMove;
    private UniTaskCompletionSource<(NodeConnection, AnimalMoveData)> _selectionSource;

    private List<GameObject> _spawnedWorldButtons = new List<GameObject>();
    private List<Button> _spawnedMoveButtons = new List<Button>();
    private Camera _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;
        //_randomModePrompt.SetActive(false);
    }

    public async UniTask<(NodeConnection, AnimalMoveData)> WaitForSelection(
        Vector3 currentPosition,
        IEnumerable<NodeConnection> targets, 
        IEnumerable<AnimalMoveData> moves,
        ControlMode mode,
        CancellationToken token)
    {
        ToggleCanvas(true);
        _selectionSource = new UniTaskCompletionSource<(NodeConnection, AnimalMoveData)>();

        using (token.Register(() => _selectionSource.TrySetCanceled()))
        {
            try
            {
                if (mode == ControlMode.RandomWithInput)
                {
                    return await HandleRandomSelection(currentPosition, targets, moves, token);
                }
                else
                {
                    InitializeManualUI(currentPosition, targets, moves);
                    return await _selectionSource.Task;
                }
            }
            finally
            {
                ToggleCanvas(false);
                ClearUI();
            }
        }
    }

    private async UniTask<(NodeConnection, AnimalMoveData)> HandleRandomSelection(
        Vector3 currentPosition,
        IEnumerable<NodeConnection> targets,
        IEnumerable<AnimalMoveData> moves,
        CancellationToken token)
    {
        //_randomModePrompt.SetActive(true);
        
        await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: token);

        var validOptions = new List<(NodeConnection connection, AnimalMoveData move)>();

        foreach (var target in targets)
        {
            StageNode node = target.GetResolvedTarget();
            if (node == null) continue;

            foreach (var move in moves)
            {
                if (IsValidMove(currentPosition, node.Position, target, move))
                {
                    validOptions.Add((target, move));
                }
            }
        }

        if (validOptions.Count == 0)
        {
            Debug.LogError("No valid moves available in Random Mode!");
            return (targets.First(), moves.First());
        }

        int rnd = UnityEngine.Random.Range(0, validOptions.Count);
        return validOptions[rnd];
    }

    private void InitializeManualUI(Vector3 currentPosition, IEnumerable<NodeConnection> targets, IEnumerable<AnimalMoveData> moves)
    {
        ClearUI();
        _confirmButton.interactable = false;

        foreach (var target in targets)
        {
            StageNode resolvedNode = target.GetResolvedTarget();
            if (resolvedNode == null) continue;

            if (!moves.Any(m => IsValidMove(currentPosition, resolvedNode.Position, target, m)))
                continue;

            Vector3 spawnPos = resolvedNode.Position + _offset;
            var btnObj = Instantiate(_targetButtonPrefab, spawnPos, Quaternion.identity);
            _spawnedWorldButtons.Add(btnObj);
            btnObj.transform.SetParent(_worldCanvas.transform, true);
            
            var capturedTarget = target; 
            btnObj.GetComponentInChildren<Button>().onClick.AddListener(() => {
                _selectedTarget = capturedTarget;
                UpdateMoveButtonsState(currentPosition, resolvedNode.Position, moves);
                HighlightButton(btnObj);
                ValidateSelection();
            });
        }

        foreach (var move in moves)
        {
            var btnObj = Instantiate(_moveButtonPrefab, _moveContainer);
            var btn = btnObj.GetComponent<Button>();
            btn.image.sprite = move.moveIcon;
            btn.interactable = false; 

            btn.onClick.AddListener(() => {
                _selectedMove = move;
                HighlightMoveButton(btn);
                ValidateSelection();
            });
            _spawnedMoveButtons.Add(btn);
        }

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() => {
            if (_selectedTarget.HasValue && _selectedMove != null)
                _selectionSource.TrySetResult((_selectedTarget.Value, _selectedMove));
        });
    }

    private void UpdateMoveButtonsState(Vector3 currentPos, Vector3 targetPos, IEnumerable<AnimalMoveData> moves)
    {
        int index = 0;
        foreach (var move in moves)
        {
            if (index >= _spawnedMoveButtons.Count) break;

            bool isValid = IsValidMove(currentPos, targetPos, _selectedTarget.Value, move);
            _spawnedMoveButtons[index].interactable = isValid;
            
            if (!isValid && _selectedMove == move) _selectedMove = null;
            
            index++;
        }
    }

    private bool IsValidMove(Vector3 start, Vector3 end, NodeConnection connection, AnimalMoveData move)
    {
        if (!connection.IsActionAllowed(move.actionType)) return false;
        return move.CanPerform(start, end);
    }

    private void Update()
    {
        if (_spawnedWorldButtons.Count == 0 || _mainCam == null) return;

        Vector3 camPos = _mainCam.transform.position;
        foreach (var btn in _spawnedWorldButtons)
        {
            if (btn == null) continue;
            
            Vector3 dir = _mainCam.transform.position - btn.transform.position;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            Vector3 euler = btn.transform.rotation.eulerAngles;
            euler.z = lookRot.eulerAngles.z;
            btn.transform.rotation = Quaternion.Euler(euler);

            float distance = Vector3.Distance(camPos, btn.transform.position);
            float scaleValue = Mathf.Clamp(distance * _scaleMultiplier, _minScale, _maxScale);
            btn.transform.localScale = Vector3.one * scaleValue;
        }
    }

    private void HighlightButton(GameObject selected)
    {
        foreach (var btn in _spawnedWorldButtons)
        {
            var img = btn.GetComponentInChildren<Image>();
            if (img != null) img.color = (btn == selected) ? Color.green : Color.white;
        }
    }

    private void HighlightMoveButton(Button selected)
    {
        foreach (var btn in _spawnedMoveButtons)
        {
            var colors = btn.colors;
            colors.normalColor = (btn == selected) ? Color.green : Color.white;
            btn.colors = colors;
        }
    }

    private void ValidateSelection()
    {
        _confirmButton.interactable = _selectedTarget.HasValue && _selectedMove != null;
    }

    private void ClearUI()
    {
        foreach (Transform child in _moveContainer) Destroy(child.gameObject);
        foreach (var btn in _spawnedWorldButtons) if (btn != null) Destroy(btn);
        
        _spawnedWorldButtons.Clear();
        _spawnedMoveButtons.Clear();
        //_randomModePrompt.SetActive(false);
        _selectedTarget = null;
        _selectedMove = null;
    }

    private void ToggleCanvas(bool visible)
    {
        _canvasGroup.alpha = visible ? 1 : 0;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}