    using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

public class InteractionUI : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _targetButtonPrefab;
    [SerializeField] private GameObject _moveButtonPrefab;

    [Header("Containers")]
    [SerializeField] private Transform _targetContainer;
    [SerializeField] private Transform _moveContainer;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private CanvasGroup _canvasGroup;

    private NodeConnection? _selectedTarget;
    private AnimalMoveData _selectedMove;
    private UniTaskCompletionSource<(NodeConnection, AnimalMoveData)> _selectionSource;

    public async UniTask<(NodeConnection, AnimalMoveData)> WaitForSelection(
        IEnumerable<NodeConnection> targets, 
        IEnumerable<AnimalMoveData> moves, 
        CancellationToken token)
    {
        InitializeUI(targets, moves);
        ToggleCanvas(true);

        _selectionSource = new UniTaskCompletionSource<(NodeConnection, AnimalMoveData)>();

        using (token.Register(() => _selectionSource.TrySetCanceled()))
        {
            try
            {
                return await _selectionSource.Task;
            }
            finally
            {
                ToggleCanvas(false);
            }
        }
    }

    private void InitializeUI(IEnumerable<NodeConnection> targets, IEnumerable<AnimalMoveData> moves)
    {
        ClearUI();
        _confirmButton.interactable = false;

        foreach (var target in targets)
        {
            var btnObj = Instantiate(_targetButtonPrefab, _targetContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = target.label;
            btnObj.GetComponent<Button>().onClick.AddListener(() => {
                _selectedTarget = target;
                ValidateSelection();
            });
        }

        foreach (var move in moves)
        {
            var btnObj = Instantiate(_moveButtonPrefab, _moveContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = move.moveName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => {
                _selectedMove = move;
                ValidateSelection();
            });
        }

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() => {
            if (_selectedTarget.HasValue && _selectedMove != null)
            {
                _selectionSource.TrySetResult((_selectedTarget.Value, _selectedMove));
            }
        });
    }

    private void ValidateSelection()
    {
        _confirmButton.interactable = _selectedTarget.HasValue && _selectedMove != null;
    }

    public void Hide() => ToggleCanvas(false);

    private void ClearUI()
    {
        foreach (Transform child in _targetContainer) Destroy(child.gameObject);
        foreach (Transform child in _moveContainer) Destroy(child.gameObject);
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