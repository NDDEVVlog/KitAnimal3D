using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnSelectMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button animalButton;

    private TurnManager tm;

    private void Awake()
    {
        tm = TurnManager.Instance;
        animalButton.onClick.AddListener(ExecuteTurn);
    }

    private void OnEnable()
    {
        tm.OnRequestPickNext.AddListener(OpenMenu);
    }

    private void OnDisable()
    {
        tm.OnRequestPickNext.RemoveListener(OpenMenu);
    }

    public void OpenMenu()
    {
        // Nếu đang AutoRandom thì không cần menu
        // (Menu vẫn có thể tồn tại trong scene, nhưng sẽ không mở)
        RefreshList();
        panel.SetActive(true);
    }

    public void CloseMenu()
    {
        panel.SetActive(false);
    }

    private void RefreshList()
    {

    }

    private void ExecuteTurn()
    {
        //tm.SelectTurn();
        CloseMenu();
    }
}
