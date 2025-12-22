using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "AnimalMoveData", menuName = "Scriptable Objects/AnimalMoveData")]
public class AnimalMoveData : ScriptableObject
{   
    public string moveName;
    public Sprite moveIcon; // Changed to Sprite for UI Image
    public ActionType actionType; // Link to the logic in Motor
    public float speedMultiplier = 1f; 
}