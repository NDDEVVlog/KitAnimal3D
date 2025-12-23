using UnityEngine;

public interface ICollisionEffect
{
    // Dành cho va chạm vật lý (Collision)
    void Execute(Collision collision, GameObject source);
    
    // Dành cho va chạm xuyên thấu (Trigger)
    void Execute(Collider other, GameObject source);
}