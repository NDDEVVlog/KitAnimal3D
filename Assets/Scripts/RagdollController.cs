using UnityEngine;
using UnityEngine.AI;

public class RagdollController : MonoBehaviour
{
    private Animator animator;
    public Rigidbody[] ragdollRigidbodies;
    public Collider[] ragdollColliders;
    
    // Collider chính dùng để di chuyển (cái Capsule to ở ngoài)
    private Collider mainCollider; 
    private Rigidbody mainRigidbody;

    void Awake()
    {
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();
        mainRigidbody = GetComponent<Rigidbody>();

        // Tự động tìm tất cả xương có Rigidbody/Collider ở bên trong
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        // Tắt Ragdoll khi mới bắt đầu game
        ToggleRagdoll(false);
    }

    public void Die()
    {   GetComponent<NavMeshAgent>().enabled = false;
        ToggleRagdoll(true);
    }

    void ToggleRagdoll(bool isDead)
    {
        // 1. Tắt/Bật Animator
        if(animator) animator.enabled = !isDead;

        // 2. Xử lý Collider và Rigidbody TỔNG (cái to ở ngoài)
        // Khi chết phải tắt cái này để nó không đè lên xương bên trong
        if (mainCollider) mainCollider.enabled = !isDead;
        if (mainRigidbody) mainRigidbody.isKinematic = isDead; 

        // 3. Xử lý các xương Ragdoll
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            // Tránh tác động vào Rigidbody tổng ở ngoài cùng
            if (rb == mainRigidbody) continue;

            rb.isKinematic = !isDead; // Khi chết thì tắt Kinematic để rơi tự nhiên
            rb.useGravity = isDead;
            
            // Chống xuyên đất: Đổi sang Continuous khi chết
            rb.collisionDetectionMode = isDead ? 
                CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col == mainCollider) continue;
            col.enabled = isDead; // Chỉ bật va chạm xương khi đã chết
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) Die();
    }
}