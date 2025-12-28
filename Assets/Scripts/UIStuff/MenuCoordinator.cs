using UnityEngine;
using System.Collections.Generic;

public class MenuCoordinator : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private MainMenuView _mainMenuView;
    [SerializeField] private SceneSelectView _sceneSelectView;

    [Header("Data")]
    [SerializeField] private List<LevelData> _allLevels;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        _mainMenuView.Setup(OnLetsGoClicked);
        _sceneSelectView.Setup(_allLevels, OnLevelSelected, OnBackToMenu);

        _mainMenuView.Show();
        _sceneSelectView.gameObject.SetActive(false);
    }

    private void OnLetsGoClicked()
    {
        _mainMenuView.Hide();
        _sceneSelectView.Show();
    }

    private void OnBackToMenu()
    {
        _sceneSelectView.Hide();
        _mainMenuView.Show();
    }

    private void OnLevelSelected(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }
}