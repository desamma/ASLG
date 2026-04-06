using UnityEngine;

public enum ModifierType
{
    /// <summary>
    /// Flat addition. e.g. -50 move speed.
    /// </summary>
    Additive,
    /// <summary>
    /// Multiplied together. e.g. 0.5 * 0.5 = 0.25x speed.
    /// </summary>
    Multiplicative,
    /// <summary>
    /// Strongest single value wins, others ignored.
    /// </summary>
    Override
}
public enum InstanceStackMode
{
    TakeStrongest,
    StackAll,
    StackAdditive
}

[System.Serializable]
public class StatModifier
{
    [Tooltip("Which stat this modifier targets. Must match the string your stat system uses.")]
    public string statName;         // e.g. "MoveSpeed", "Defense", "AttackSpeed"

    public ModifierType type = ModifierType.Multiplicative;

    public float value = 0.5f;

    [Tooltip("When multiple instances of the same effect are active,\n" +
             "how are their modifiers combined?\n\n" +
             "TakeStrongest — only the strongest instance applies (e.g. Slow).\n" +
             "StackAll      — all instances apply up to maxInstances (e.g. Burn damage).\n" +
             "StackAdditive — values are summed across instances.")]
    public InstanceStackMode instanceStackMode = InstanceStackMode.TakeStrongest;
}