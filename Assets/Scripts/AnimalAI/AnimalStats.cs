using System;
using UnityEngine;

public enum ActionType
{
    None, 
    Walk,
    Sprint,
    Jump,
    Interact,
    Swim // ADDED
}

[Serializable]
public struct AnimalStats
{
    [Min(0f)] public float walkSpeed;
    [Min(0f)] public float sprintSpeed;
    [Min(0f)] public float swimSpeed; // ADDED
    
    [Min(0f)] public float jumpHeight;
    [Min(0f)] public float jumpDuration;

    public bool isDead;
}

public static class AnimHash
{
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Grounded = Animator.StringToHash("Grounded");
    public static readonly int Die = Animator.StringToHash("Die");
    public static readonly int Interact = Animator.StringToHash("Interact");
    public static readonly int InteractionType = Animator.StringToHash("InteractionType");
    public static readonly int Swim = Animator.StringToHash("Swim"); // ADDED
}