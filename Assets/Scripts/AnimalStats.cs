using System;
using UnityEngine;

public enum ActionType
{
    Walk,
    Sprint,
    Jump
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
}

public static class AnimHash
{
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Grounded = Animator.StringToHash("Grounded");
}