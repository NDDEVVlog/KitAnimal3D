using UnityEngine;
using System;

[Serializable]
public class ApplyForceEffect : ICollisionEffect
{   
    public string customName = "Lực đẩy va chạm";
    public float forceMagnitude = 20f;
    public int boneIndex = 1; // Xương chậu thường là 1

    public void Execute(Collision collision, GameObject source)
    {
        // 1. Tìm script RagdollController
        RagdollController ragdoll = collision.gameObject.GetComponentInParent<RagdollController>();
        
        if (ragdoll != null)
        {
            // Bật Ragdoll ngay lập tức
            ragdoll.Die(); 

            // 2. Xác định Rigidbody để đẩy
            // Nếu đụng trúng xương lẻ thì lấy xương đó, nếu đụng trúng cha thì lấy xương đầu tiên trong danh sách
            Rigidbody targetRb = collision.collider.GetComponent<Rigidbody>();

            if (targetRb == null || collision.gameObject == ragdoll.gameObject)
            {
                // Nếu va chạm vào object cha, lấy xương chậu (thường là Element 0 hoặc 1 trong list ragdoll)
                if(ragdoll.ragdollRigidbodies.Length > 0) {
                    targetRb = ragdoll.ragdollRigidbodies[boneIndex]; 
                }
            }

            if (targetRb != null)
            {
                Debug.Log("Lực đang tác động vào: " + targetRb.name);

                // Tính hướng đẩy
                Vector3 forceDirection = targetRb.transform.position - source.transform.position;
                forceDirection.y = 0.2f; // Đẩy hơi hất lên
                forceDirection.Normalize();

                // Tác động lực
                targetRb.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
                
                Debug.DrawRay(targetRb.transform.position, forceDirection * 5f, Color.magenta, 2f);
            }
        }
    }

    public void Execute(Collider other, GameObject source) { /* Trigger thường không có lực vật lý */ }
}

[Serializable]
public class KillAnimalEffect : ICollisionEffect
{   
    public string customName = "Animation Ragdoll Die";
    public void Execute(Collision collision, GameObject source)
    {
        var ragdoll = collision.gameObject.GetComponentInParent<RagdollController>();
        if (ragdoll != null) ragdoll.Die();
    }

    public void Execute(Collider other, GameObject source)
    {
        var ragdoll = other.GetComponentInParent<RagdollController>();
        if (ragdoll != null) ragdoll.Die();
    }
}