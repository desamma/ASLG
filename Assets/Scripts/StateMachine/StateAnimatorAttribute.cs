using System;

/// <summary>
/// Attribute to map enum states to animator parameter names
/// </summary>
/// <remarks>
/// See <see cref="StateManager{TState}"/> for details
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class StateAnimatorAttribute : Attribute
{
    public string AnimatorParameterName { get; }

    public StateAnimatorAttribute(string animatorParameterName)
    {
        AnimatorParameterName = animatorParameterName;
    }
}
