using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // Assuming TextMeshPro, use Text if standard UI

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject _buttonPrefab;
    [SerializeField] private Transform _container;
    [SerializeField] private CanvasGroup _canvasGroup;

    private List<GameObject> _activeButtons = new List<GameObject>();

    public void ShowOptions(IEnumerable<PathConnection> options, Action<PathConnection> onSelected)
    {
        ClearButtons();
        ToggleCanvas(true);

        foreach (var option in options)
        {
            CreateButton(option, onSelected);
        }
    }

    public void Hide()
    {
        ToggleCanvas(false);
        ClearButtons();
    }

    private void CreateButton(PathConnection data, Action<PathConnection> callback)
    {
        GameObject btnObj = Instantiate(_buttonPrefab, _container);
        Button btn = btnObj.GetComponent<Button>();
        TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

        if (txt != null) txt.text = $"{data.actionType}: {data.optionName}";

        btn.onClick.AddListener(() => 
        {
            callback?.Invoke(data);
        });

        _activeButtons.Add(btnObj);
    }

    private void ClearButtons()
    {
        foreach (var btn in _activeButtons) Destroy(btn);
        _activeButtons.Clear();
    }

    private void ToggleCanvas(bool visible)
    {
        _canvasGroup.alpha = visible ? 1 : 0;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}