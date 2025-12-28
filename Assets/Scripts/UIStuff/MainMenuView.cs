using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VectorGraphics;

[RequireComponent(typeof(CanvasGroup))]
public class MainMenuView : MonoBehaviour
{
    [SerializeField] private Button _letsGoButton;
    [SerializeField] private RectTransform _leftArrow;
    [SerializeField] private RectTransform _rightArrow;
    [SerializeField] private CanvasGroup _canvasGroup;

    private System.Action _onLetsGo;

    public void Setup(System.Action onLetsGo)
    {
        _onLetsGo = onLetsGo;
        _letsGoButton.onClick.RemoveAllListeners();
        _letsGoButton.onClick.AddListener(OnLetsGoClicked);
        
        KillAnimations();
        StartIdleAnimation();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.interactable = true;
        _canvasGroup.alpha = 1f;
        _canvasGroup.DOFade(1f, 0.3f).SetUpdate(true);
        
        KillAnimations();
        StartIdleAnimation();
    }

    public void Hide()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.DOFade(0f, 0.2f)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnLetsGoClicked()
    {   
        
        KillAnimations();
        _letsGoButton.transform.DOScale(1.1f, 0.1f)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true)
            .OnComplete(() => _onLetsGo?.Invoke());
    }

    private void StartIdleAnimation()
    {
        _leftArrow.DOAnchorPosX(_leftArrow.anchoredPosition.x - 20f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
            
        _rightArrow.DOAnchorPosX(_rightArrow.anchoredPosition.x + 20f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
            
        _letsGoButton.transform.DOScale(1.05f, 0.8f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void KillAnimations()
    {
        _leftArrow.DOKill();
        _rightArrow.DOKill();
        _letsGoButton.transform.DOKill();
        _canvasGroup.DOKill();
        
        _letsGoButton.transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        KillAnimations();
    }

    private void OnDestroy()
    {
        KillAnimations();
    }
}