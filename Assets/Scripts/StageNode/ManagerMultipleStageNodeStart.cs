using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class ManagerMultipleStageNodeStart : MonoBehaviour
{
    [Header("World UI")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private GameObject ButtonPrefab;

    [Header("Animals")]
    [SerializeField] private List<AnimalBrain> animalBrains = new();

    [Header("Runtime")]
    [SerializeField] private AnimalBrain selectedAnimalBrain;

    private UniTaskCompletionSource<AnimalBrain> _selectionSource;
    private readonly List<GameObject> _spawnedButtons = new();

    private  void Start()
    {
        // 1) Tắt hết brain lúc đầu
        foreach (var ab in animalBrains)
        {
            if (ab != null) ab.enabled = false;
        }
    }

    public void OpenSelectUI_Event()
    {
        OpenSelectUI().Forget();
    }
    public async UniTask  OpenSelectUI()
    {
        var token = this.GetCancellationTokenOnDestroy();
        try
        {
            selectedAnimalBrain = await SelectGameObjectToRun(animalBrains, token);
        }
        catch (OperationCanceledException)
        {

        }
    }

    public async UniTask<AnimalBrain> SelectGameObjectToRun(List<AnimalBrain> brains, CancellationToken token)
    {
        CleanupButtons();

        _selectionSource = new UniTaskCompletionSource<AnimalBrain>();

        using (token.Register(() => _selectionSource.TrySetCanceled()))
        {
            TurnOnUI(brains);

            // Đợi user chọn
            var selectedBrain = await _selectionSource.Task;

            Debug.Log("Pick and wait for Ui close");
            // Tắt UI
            CleanupButtons();

            // Enable đúng con được chọn
            if (selectedBrain != null) selectedBrain.enabled = true;

            return selectedBrain;
        }
    }

    public void TurnOnUI(IEnumerable<AnimalBrain> brains)
    {
        if (worldCanvas == null || ButtonPrefab == null) return;

        // Chuyển sang list để lấy index ổn định
        var list = brains.Where(b => b != null).ToList();

        for (int i = 0; i < list.Count; i++)
        {
            var brain = list[i];        // local copy (tránh closure bug)
            int index = i;              // local copy

            Vector3 spawnPos = brain.transform.position + Vector3.up * 5f;

            var btnObj = Instantiate(ButtonPrefab, spawnPos, Quaternion.identity, worldCanvas.transform);
            _spawnedButtons.Add(btnObj);
            btnObj.transform.localScale /=8; 
            var buttonComp = btnObj.GetComponent<Button>();
            if (buttonComp == null) continue;

            buttonComp.onClick.AddListener(() =>
            {
                // Set selection result (đây là thứ bạn thiếu)
                selectedAnimalBrain = brain;
                _selectionSource?.TrySetResult(brain);

                // Nếu muốn đồng bộ TurnManager ngay tại đây
                if (TurnManager.Instance != null)
                {
                    TurnManager.Instance.SelectTurn(index);
                    TurnManager.Instance.NotifyPickCompleted();
                }
                    
                
                
            });
        }
    }

    private void CleanupButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                Destroy(_spawnedButtons[i]);
        }
        _spawnedButtons.Clear();
    }
}
