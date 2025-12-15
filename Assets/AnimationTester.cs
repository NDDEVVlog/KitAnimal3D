using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationTester : MonoBehaviour
{
    private Animator _animator;
    private readonly int _actionIdHash = Animator.StringToHash("ActionID");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) PlayAnimation(0); 
        if (Input.GetKeyDown(KeyCode.Alpha2)) PlayAnimation(1); 
        if (Input.GetKeyDown(KeyCode.Alpha3)) PlayAnimation(2); 
        if (Input.GetKeyDown(KeyCode.Alpha4)) PlayAnimation(3); 
        if (Input.GetKeyDown(KeyCode.Alpha5)) PlayAnimation(4); 
        if (Input.GetKeyDown(KeyCode.Alpha6)) PlayAnimation(5); 
        if (Input.GetKeyDown(KeyCode.Alpha7)) PlayAnimation(6); 
        if (Input.GetKeyDown(KeyCode.Alpha8)) PlayAnimation(7); 
    }

    private void PlayAnimation(int id)
    {
        if (_animator.GetInteger(_actionIdHash) != id)
        {
            _animator.SetInteger(_actionIdHash, id);
        }
    }
}