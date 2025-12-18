using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    // Unique ID for the Animator (e.g., 0 for Slide, 1 for Bike, 2 for NPC)
    public int AnimationTypeID; 

    // The logic that runs when the animal interacts with this object
    // Changed: IEnumerator -> UniTask, Removed Action callback
    public abstract UniTask ExecuteInteraction(AnimalMotor motor);
}