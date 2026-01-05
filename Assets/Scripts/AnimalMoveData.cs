using UnityEngine;

[CreateAssetMenu(fileName = "AnimalMoveData", menuName = "Scriptable Objects/AnimalMoveData")]
public class AnimalMoveData : ScriptableObject
{   
    public string moveName;
    public Sprite moveIcon;
    public ActionType actionType;
    // REMOVED: public float speedMultiplier = 1f;

    [Header("Constraints")]
    public float maxDistance = 100f;
    public float maxInclineHeight = 2f;
    public float maxDropHeight = 10f;

    public bool CanPerform(Vector3 startPosition, Vector3 targetPosition)
    {
        float distance = Vector3.Distance(new Vector3(startPosition.x, 0, startPosition.z), new Vector3(targetPosition.x, 0, targetPosition.z));
        float heightDiff = targetPosition.y - startPosition.y;

        if (distance > maxDistance) return false;

        if (heightDiff > 0 && heightDiff > maxInclineHeight) return false;
        
        if (heightDiff < 0 && Mathf.Abs(heightDiff) > maxDropHeight) return false;

        return true;
    }
}