using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalMotor : MonoBehaviour
{
    private NavMeshAgent _agent;
    [SerializeField]private Animator _animator;
    private AnimalStats _stats;

    public UnityEvent DieEvent;

    public float DebugSpeed;

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
        // Optimization: Only update animator if agent is actually active
        if (_agent.enabled)
        {
            // Velocity magnitude involves a SquareRoot calculation. 
            // It's generally fine, but good to be aware of in Update loops.
            _animator.SetFloat(AnimHash.Speed, _agent.velocity.magnitude);
            DebugSpeed = _agent.velocity.magnitude;
        }
    }

    public async UniTask MoveTo(Vector3 target, float speed)
    {   
        // Debug.Log("MoveTo"); // Removing Debug.Log in frequent actions helps FPS
        if(_stats.isDead) return;

        _agent.enabled = true;

        // --- Fix Logic: Ensure Agent is on NavMesh ---
        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"Agent {name} is off NavMesh! Attempting Warp...");
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError($"No NavMesh found near {name}!");
                return;
            }
        }

        _agent.isStopped = false; 
        _agent.speed = speed; 
        _agent.SetDestination(target);

        var token = this.GetCancellationTokenOnDestroy();

        // --- OPTIMIZATION START ---
        // Instead of UniTask.WaitUntil (which allocates a delegate and checks every frame with overhead),
        // we use a manual while loop. This is much lighter on the CPU and GC.

        // 1. Wait for Path Calculation (Prevent accessing remainingDistance while path is pending)
        while (_agent.pathPending && !_agent.isStopped)
        {
            if (token.IsCancellationRequested || _stats.isDead) return;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 2. Wait for Arrival
        while (true)
        {
            // A. Check Lifecycle/Cancellation
            if (token.IsCancellationRequested || _stats.isDead || this == null) return;
            
            // B. Check Agent Validity
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh) break;

            // C. Check Distance (Expensive native call, only do this if valid)
            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                // Double check: Ensure we aren't just stopped momentarily or lacking a path
                if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
                {
                    break; // Destination reached
                }
            }

            // Yield execution to the next frame
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        // --- OPTIMIZATION END ---

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

    public async UniTask PerformSwim(Vector3 target, float speed)
    {
        _animator.SetBool(AnimHash.Swim, true);
        await MoveTo(target, speed);
        _animator.SetBool(AnimHash.Swim, false);
    }
}