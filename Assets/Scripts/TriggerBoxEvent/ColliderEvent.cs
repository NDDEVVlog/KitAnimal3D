using UnityEngine;
using UnityEngine.Events;

public class ColliderEvent : MonoBehaviour
{   
    public UnityEvent<Collision,GameObject> OnCollisionEnterEvent;
    void OnCollisionEnter(Collision collision)
    {   
        if (collision.collider.CompareTag("Player"))
        OnCollisionEnterEvent?.Invoke(collision,this.gameObject);
    }
}
