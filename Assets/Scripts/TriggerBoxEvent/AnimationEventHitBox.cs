using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class AnimationEventHitBox : MonoBehaviour
{
    public Collider hitBox;

    public void EnableHitBox()
    {
        if (hitBox == null)
        {
            hitBox = GetComponent<Collider>();
        }
        hitBox.enabled = true;

        
    }

    public void DisableHitBox()
    {
        if (hitBox == null)
        {
            hitBox = GetComponent<Collider>();
        }
        hitBox.enabled = false;

    }



}
