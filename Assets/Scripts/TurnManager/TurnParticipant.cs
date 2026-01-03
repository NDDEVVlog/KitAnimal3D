using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TurnParticipant : MonoBehaviour
{
    [Header("Identification")]
    public string displayName;

    [Header("Behaviours to toggle")]
    [Tooltip("Các MonoBehaviour sẽ được bật khi BeginTurn và tắt khi EndTurn.")]
    public List<Behaviour> toggleBehaviours = new();

    [Header("Events")]
    public UnityEvent OnTurnBegin;
    public UnityEvent OnTurnEnd;

    public bool IsValidForTurn =>
        gameObject.activeInHierarchy && enabled;

    public void BeginTurn()
    {  
        Debug.Log("BeginNewTurn");
        foreach (var b in toggleBehaviours)
            if (b != null) b.enabled = true;

        OnTurnBegin?.Invoke();
    }

    public void EndTurn()
    {
        OnTurnEnd?.Invoke();

        foreach (var b in toggleBehaviours)
            if (b != null) b.enabled = false;
    }

    private void Reset()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = gameObject.name;
    }
}
