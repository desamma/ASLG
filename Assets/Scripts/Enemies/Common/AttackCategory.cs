using System;

public class AttackCategory<TState> where TState : Enum
{
    public float Frequency;
    public AttackConfig<TState>[] Attacks;
}

public class AttackConfig<TState> where TState : Enum
{
    public TState State;
    public float Range;
    public int MoveCountCooldown;
}