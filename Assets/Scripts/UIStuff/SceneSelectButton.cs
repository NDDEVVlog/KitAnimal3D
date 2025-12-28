using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

[RequireComponent(typeof(Button))]
public class SceneSelectButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _levelNameText;
    [SerializeField] private Image _thumbnailImage;
    [SerializeField] private Image _lockIcon;
    [SerializeField] private Button _button;
    
    private string _targetScene;
    private Action<string> _onSceneSelected;

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
    }

    public void Initialize(LevelData data, Action<string> onSceneSelected)
    {
        if (data == null) return;

        //_levelNameText.text = data.LevelName;
        _thumbnailImage.sprite = data.LevelThumbnail;
        _targetScene = data.sceneAsset.name;
        _onSceneSelected = onSceneSelected;
        
        bool isLocked = data.IsLocked;
        _button.interactable = !isLocked;
        _lockIcon.gameObject.SetActive(isLocked);
        
        ResetState();
    }

    public void ResetState()
    {
        transform.DOKill();
        transform.localScale = Vector3.zero;
    }

    public void AnimatePopUp(float delay)
    {
        transform.DOScale(Vector3.one, 0.4f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private void OnClick()
    {
        if (string.IsNullOrEmpty(_targetScene))
        {
            Debug.LogError($"[SceneSelectButton] Scene Name is empty in Level Data: {_levelNameText.text}");
            return;
        }

        transform.DOKill();
        transform.DOScale(0.9f, 0.1f)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true)
            .OnComplete(() => _onSceneSelected?.Invoke(_targetScene));
    }

    private void OnDisable()
    {
        transform.DOKill();
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}