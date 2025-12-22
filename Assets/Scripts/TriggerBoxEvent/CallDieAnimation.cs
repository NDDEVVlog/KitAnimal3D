using UnityEngine;

public class CallDieAnimation : MonoBehaviour
{   
    public void CallDie(Collider other)
    {
        AnimalMotor animalMotor = other.GetComponent<AnimalMotor>();
        Debug.Log("Call Die Animation on " + other.name);
        if (animalMotor != null)
        {   
            animalMotor.Die();
        }
    }
}
