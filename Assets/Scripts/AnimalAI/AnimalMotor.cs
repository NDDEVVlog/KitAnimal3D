using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class AnimalMotor : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private AnimalStats _stats;



    public void Initialize(AnimalStats stats)
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
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
        _agent.enabled = true;
        _agent.isStopped = false;
        _agent.speed = speed; // Use the speed provided by MoveData
        _agent.SetDestination(target);

        var token = this.GetCancellationTokenOnDestroy();
        await UniTask.WaitUntil(() => !_agent.pathPending, cancellationToken: token);
        await UniTask.WaitUntil(() => _agent.remainingDistance <= _agent.stoppingDistance, cancellationToken: token);

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

        // Await the interaction logic directly
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