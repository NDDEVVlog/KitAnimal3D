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
    [SerializeField] private Transform _moveContainer;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Canvas _worldCanvas;

    [Header("World UI Settings")]
    [SerializeField] private Vector3 _offset = Vector3.up * 1.5f;
    [Range(0.001f, 1f)]
    [SerializeField] private float _scaleMultiplier = 0.02f; // Adjust this to set base size
    [SerializeField] private float _minScale = 0.1f;         // Minimum size allowed
    [SerializeField] private float _maxScale = 2.0f;         // Maximum size allowed

    private NodeConnection? _selectedTarget;
    private AnimalMoveData _selectedMove;
    private UniTaskCompletionSource<(NodeConnection, AnimalMoveData)> _selectionSource;

    private List<GameObject> _spawnedWorldButtons = new List<GameObject>();
    private Camera _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;
    }

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
                ClearUI();
            }
        }
    }

    private void InitializeUI(IEnumerable<NodeConnection> targets, IEnumerable<AnimalMoveData> moves)
    {
        ClearUI();
        _confirmButton.interactable = false;

        foreach (var target in targets)
        {
            StageNode resolvedNode = target.GetResolvedTarget();
            if (resolvedNode == null) continue;

            // Spawn at Node position + Offset
            Vector3 spawnPos = resolvedNode.Position + _offset;
            
            var btnObj = Instantiate(_targetButtonPrefab, spawnPos, Quaternion.identity);
            _spawnedWorldButtons.Add(btnObj);
            btnObj.transform.SetParent(_worldCanvas.transform, true);

            
            var capturedTarget = target; 
            btnObj.GetComponentInChildren<Button>().onClick.AddListener(() => {
                _selectedTarget = capturedTarget;
                ValidateSelection();
                HighlightButton(btnObj);
            });
        }

        foreach (var move in moves)
        {
            var btnObj = Instantiate(_moveButtonPrefab, _moveContainer);
            btnObj.GetComponent<Button>().image.sprite = move.moveIcon;
            btnObj.GetComponent<Button>().onClick.AddListener(() => {
                _selectedMove = move;
                ValidateSelection();
            });
        }

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() => {
            if (_selectedTarget.HasValue && _selectedMove != null)
                _selectionSource.TrySetResult((_selectedTarget.Value, _selectedMove));
        });
    }

    private void Update()
    {
        if (_spawnedWorldButtons.Count == 0 || _mainCam == null) return;

        Vector3 camPos = _mainCam.transform.position;

        foreach (var btn in _spawnedWorldButtons)
        {
            if (btn == null) continue;

            // Get direction to camera
                Vector3 dir = _mainCam.transform.position - btn.transform.position;

                // Convert direction to local space if needed
                Quaternion lookRot = Quaternion.LookRotation(dir);

                // Keep only X rotation
                Vector3 euler = btn.transform.rotation.eulerAngles;
                euler.z = lookRot.eulerAngles.z;

                // Apply
                btn.transform.rotation = Quaternion.Euler(euler);



            // 2. Distance-Based Scaling
            float distance = Vector3.Distance(camPos, btn.transform.position);
            
            // Calculate scale: further objects need higher scale to look the same size
            float scaleValue = distance * _scaleMultiplier;
            
            // Clamp so they don't get infinitely small/large
            scaleValue = Mathf.Clamp(scaleValue, _minScale, _maxScale);
            
            btn.transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
        }
    }

    private void HighlightButton(GameObject selected)
    {
        foreach (var btn in _spawnedWorldButtons)
        {
            var img = btn.GetComponentInChildren<Image>();
            if (img != null) img.color = (btn == selected) ? Color.green : Color.red;
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