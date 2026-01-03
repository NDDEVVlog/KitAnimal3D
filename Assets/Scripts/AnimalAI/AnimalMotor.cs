using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalMotor : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private AnimalStats _stats;

    public UnityEvent DieEvent;
    public void Initialize(AnimalStats stats)
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        if(_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _stats = stats;
        _stats.isDead = false;
        
        _agent.speed = _stats.walkSpeed;
        _agent.angularSpeed = 360f;
    }

    private void Update()
    {
        if (_agent.enabled)
        {
            _animator.SetFloat(AnimHash.Speed, _agent.velocity.magnitude);
        }
    }

    public async UniTask MoveTo(Vector3 target, float speed)
    {   
        if(_stats.isDead) return;

        // 1. Đảm bảo Agent được bật
        _agent.enabled = true;

        // --- FIX BẮT ĐẦU: Xử lý trường hợp Agent bị mất NavMesh do Teleport/Slide ---
        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"Agent {name} không nằm trên NavMesh! Đang thử Warp...");
            
            // Tìm điểm NavMesh gần nhất trong bán kính 2m để gắn lại
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                _agent?.Warp(hit.position); // Gắn Agent vào vị trí hợp lệ
            }
            else
            {
                Debug.LogError($"Không tìm thấy NavMesh nào gần {name} để di chuyển!");
                return; // Thoát luôn để tránh lỗi crash "Resume"
            }
        }
        // --- FIX KẾT THÚC ---

        // 2. Setup di chuyển (Bây giờ gọi isStopped mới an toàn)
        _agent.isStopped = false; 
        _agent.speed = speed; 
        
        // 3. Set Destination

        _agent?.SetDestination(target);

        var token = this.GetCancellationTokenOnDestroy();

        // Chờ đường đi được tính toán (PathPending)
        await UniTask.WaitUntil(() => !_agent.isActiveAndEnabled || !_agent.pathPending, cancellationToken: token);

        // Chờ đến khi đến nơi
        await UniTask.WaitUntil(() => 
        {
            if (_stats.isDead || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return true;
            return _agent.remainingDistance <= _agent.stoppingDistance;
        }, cancellationToken: token);

        if (_stats.isDead || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

        _agent.velocity = Vector3.zero;
        _agent.isStopped = true;
    }

    public async UniTask PerformJump(Vector3 target)
    {
        _agent.enabled = false;
        _animator.SetBool(AnimHash.Grounded, false);
        _animator.SetTrigger(AnimHash.Jump);

        Vector3 startPos = transform.position;
        float elapsed = 0f;
        var token = this.GetCancellationTokenOnDestroy();

        while (elapsed < _stats.jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _stats.jumpDuration;
            
            float height = Mathf.Sin(Mathf.PI * t) * _stats.jumpHeight;
            Vector3 currentPos = Vector3.Lerp(startPos, target, t);
            currentPos.y += height;

            transform.position = currentPos;
            await UniTask.NextFrame(token);
        }

        transform.position = target;
        _animator.SetBool(AnimHash.Grounded, true);
        _agent.enabled = true;
    }

    public async UniTask PerformInteraction(Interactable interactable)
    {
        _agent.enabled = false;
        _animator.SetInteger(AnimHash.InteractionType, interactable.AnimationTypeID);
        _animator.SetTrigger(AnimHash.Interact);

        await interactable.ExecuteInteraction(this);

        _animator.SetTrigger(AnimHash.Grounded); 
        _agent.enabled = true;
    }

    public void Die()
    {
        _agent.enabled = false;
        _animator.SetBool(AnimHash.Die, true);
    }
}