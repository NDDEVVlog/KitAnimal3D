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
        RagdollController ragdoll = collision.gameObject.GetComponentInParent<RagdollController>();
        if (ragdoll != null && ragdoll.ragdollRigidbodies.Length > boneIndex)
        {
            Rigidbody targetRb = ragdoll.ragdollRigidbodies[boneIndex];
            Vector3 dir = (targetRb.transform.position - source.transform.position).normalized;
            dir.y = 0.3f;
            targetRb.AddForce(dir * forceMagnitude, ForceMode.Impulse);
            Debug.Log($"[Effect] Applied force to {targetRb.name}");
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