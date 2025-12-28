using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class SceneSelectView : MonoBehaviour
{
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private SceneSelectButton _buttonPrefab;
    [SerializeField] private Button _backButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _staggerDelay = 0.05f;

    private List<SceneSelectButton> _spawnedButtons = new List<SceneSelectButton>();
    private System.Action _onBack;

    public void Setup(List<LevelData> levels, System.Action<string> onLevelSelected, System.Action onBack)
    {
        _onBack = onBack;
        //_backButton.onClick.RemoveAllListeners();
        //_backButton.onClick.AddListener(OnBackClicked);

        foreach (Transform child in _gridContainer)
        {
            Destroy(child.gameObject);
        }
        _spawnedButtons.Clear();

        foreach (var level in levels)
        {
            var btn = Instantiate(_buttonPrefab, _gridContainer);
            btn.Initialize(level, onLevelSelected);
            _spawnedButtons.Add(btn);
        }

        gameObject.SetActive(false);
        _canvasGroup.alpha = 0;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.interactable = false;
        
        Sequence seq = DOTween.Sequence();
        
        seq.Append(_canvasGroup.DOFade(1f, 0.3f));
        
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            _spawnedButtons[i].ResetState();
            _spawnedButtons[i].AnimatePopUp(i * _staggerDelay);
        }

        seq.OnComplete(() => _canvasGroup.interactable = true);
    }

    public void Hide()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}