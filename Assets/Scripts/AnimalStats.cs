using System;
using UnityEngine;

public enum ActionType
{
    Walk,
    Sprint,
    Jump,
    Interact // Added this
}

[Serializable]
public struct AnimalStats
{
    [Min(0f)] public float walkSpeed;
    [Min(0f)] public float sprintSpeed;
    [Min(0f)] public float jumpHeight;
    [Min(0f)] public float jumpDuration;
}

[Serializable]
public struct PathConnection
{
    public string optionName;
    public ActionType actionType;
    public StageNode targetNode;
    public Interactable interactionObject; // Reference to the specific object (Slide, Bike, NPC)
}

public static class AnimHash
{
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Grounded = Animator.StringToHash("Grounded");
    public static readonly int Interact = Animator.StringToHash("Interact"); // Trigger
    public static readonly int InteractionType = Animator.StringToHash("InteractionType"); // Int for BlendTree (0=Slide, 1=Bike, etc)
}