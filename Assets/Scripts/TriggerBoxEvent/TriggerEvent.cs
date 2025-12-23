using UnityEngine;
using UnityEngine.Events;

public class TriggerEvent : MonoBehaviour
{   
    public UnityEvent<Collider,GameObject> onHitBoxTrigger;
    private void OnTriggerEnter(Collider other)
    {   
        Debug.Log($"Something entered the trigger: {other.name} with tag {other.tag}");
        if (other.CompareTag("Player"))
        onHitBoxTrigger?.Invoke(other,other.gameObject);
    }
}
