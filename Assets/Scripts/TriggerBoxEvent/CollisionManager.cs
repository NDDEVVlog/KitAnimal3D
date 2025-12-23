using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CollisionManager : MonoBehaviour
{
    [Header("Thứ tự thực hiện từ trên xuống dưới")]
    [SerializeReference] 
    public List<ICollisionEffect> effects = new List<ICollisionEffect>();

    [Header("Sự kiện thực thi cuối cùng")]
    public UnityEvent OnAllEffectsFinished;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            foreach (var effect in effects)
            {
                effect.Execute(collision, this.gameObject);
            }
            OnAllEffectsFinished?.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (var effect in effects)
            {
                effect.Execute(other, this.gameObject);
            }
            OnAllEffectsFinished?.Invoke();
        }
    }
}