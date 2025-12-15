using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class AnimalMotor : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private AnimalStats _stats;

    public float CurrentSpeed; 
    public void Initialize(AnimalStats stats)
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _stats = stats;
        
        _agent.speed = _stats.walkSpeed;
        _agent.angularSpeed = 360f;
    }

    private void Update()
    {   
        CurrentSpeed = _agent.velocity.magnitude;
        _animator.SetFloat(AnimHash.Speed, _agent.velocity.magnitude);
    }

    public IEnumerator MoveTo(Vector3 target, bool sprint)
    {
        _agent.enabled = true;
        _agent.isStopped = false;
        _agent.speed = sprint ? _stats.sprintSpeed : _stats.walkSpeed;
        _agent.SetDestination(target);

        // Wait until path is calculated
        while (_agent.pathPending) yield return null;

        // Wait until reached
        while (_agent.remainingDistance > _agent.stoppingDistance)
        {
            yield return null;
        }

        _agent.velocity = Vector3.zero;
        _agent.isStopped = true;
    }

    public IEnumerator PerformJump(Vector3 target)
    {
        _agent.enabled = false;
        
        _animator.SetBool(AnimHash.Grounded, false);
        _animator.SetTrigger(AnimHash.Jump);

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < _stats.jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _stats.jumpDuration;

            // Parabolic movement
            float height = Mathf.Sin(Mathf.PI * t) * _stats.jumpHeight;
            Vector3 currentPos = Vector3.Lerp(startPos, target, t);
            currentPos.y += height;

            transform.position = currentPos;
            
            // Rotate towards target
            Vector3 dir = (target - startPos).normalized;
            if(dir != Vector3.zero) 
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 15f);

            yield return null;
        }

        transform.position = target;
        _animator.SetBool(AnimHash.Grounded, true);
        
        _agent.enabled = true;
    }
}