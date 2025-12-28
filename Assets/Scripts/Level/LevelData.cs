using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public string LevelName;
    public SceneAsset sceneAsset;
    public Sprite LevelThumbnail;
    public bool IsLocked;
}