using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _loadingBar;
    [SerializeField] private float _fadeDuration = 0.5f;
    public GameObject spawnGameobject;
    public ControlMode currentControlMode;

    public void SetSpawnGameobject(GameObject obj)
    {

         spawnGameobject = obj;
        
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        if (_loadingBar != null) _loadingBar.fillAmount = 0f;
        
        GetComponent<Canvas>().sortingOrder = 999;
        GetComponent<Canvas>().planeDistance = 5; 
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is null or empty!");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Scene '{sceneName}' cannot be loaded. Check Build Settings!");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ProcessSceneLoading(sceneName));
    }

    private IEnumerator ProcessSceneLoading(string sceneName)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOKill();
        yield return _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true).WaitForCompletion();

        if (_loadingBar != null) _loadingBar.fillAmount = 0f;

        // Cleanup memory before loading new scene
        DOTween.KillAll();
        System.GC.Collect();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (_loadingBar != null)
                _loadingBar.fillAmount = progress;

            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Wait a bit for initialization of the new scene
        yield return new WaitForSecondsRealtime(0.5f); 

        StageNodeSpawnPoint spawnPoint = FindFirstObjectByType<StageNodeSpawnPoint>();
        if (spawnPoint != null)
        {
            spawnPoint.spawnedObject = spawnGameobject; // Reset any previous reference
            spawnPoint.SpawnAtPoint();
            spawnPoint.controlMode = currentControlMode;
        }
        _canvasGroup.DOFade(0f, _fadeDuration)
            .SetUpdate(true)
            .OnComplete(() => _canvasGroup.blocksRaycasts = false);

        
    }
}