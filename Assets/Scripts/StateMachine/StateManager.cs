using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Generic state manager that works with any enum <typeparamref name="TState"/>. <para/>
/// Attribute usages: See <see cref="Enemy_DemonKing_State"/> <para/>
/// State Manager usages: See <see cref="Enemy_DemonKing_Movement"/>
/// </summary>
/// <typeparam name="TState">The enum type representing states</typeparam>
public class StateManager<TState> where TState : Enum
{
    //Private storage
    private TState currentState;
    private readonly Animator animator;
    private readonly Dictionary<TState, string> stateToAnimatorParam;

    public TState CurrentState => currentState;

    public StateManager(Animator animator, TState initialState)
    {
        this.animator = animator;
        stateToAnimatorParam = new Dictionary<TState, string>();

        BuildStateAnimatorMapping();
        currentState = initialState;
        SetAnimatorParameter(currentState, true);
    }

    /// <summary>
    /// Builds mapping between <typeparamref name="TState"/> and animator parameter names using attributes ( Idle - isIdle )
    /// </summary>
    private void BuildStateAnimatorMapping()
    {
        Type enumType = typeof(TState); //Type of TState (Enum type)
        foreach (TState state in Enum.GetValues(enumType)) //look up the enum's members, yeah that work..
        {
            FieldInfo fieldInfo = enumType.GetField(state.ToString());

            // Read the custom [StateAttribute("isIdle")]
            StateAnimatorAttribute attribute = (StateAnimatorAttribute)Attribute.GetCustomAttribute(
                fieldInfo, typeof(StateAnimatorAttribute));

            if (attribute != null)
            {
                stateToAnimatorParam[state] = attribute.AnimatorParameterName;
            }
            else
            {
                // Convert enum with "is" prefix
                // Chase -> isChase
                string defaultParamName = "is" + state.ToString();
                stateToAnimatorParam[state] = defaultParamName;
            }
        }
    }

    /// <summary>
    /// Change to a new <typeparamref name="TState"/>
    /// Order: Exit current state -> Change state -> Enter new state
    /// </summary>
    public void ChangeState(TState newState)
    {
        //if(currentState.Equals(newState))
        if (EqualityComparer<TState>.Default.Equals(currentState, newState))
            return;

        // Exit current state
        SetAnimatorParameter(currentState, false);
        OnStateExit?.Invoke(currentState);

        // Change state
        TState previousState = currentState;
        currentState = newState;

        // Enter new state
        SetAnimatorParameter(currentState, true);
        OnStateChanged?.Invoke(previousState, currentState);
        OnStateEnter?.Invoke(currentState);
    }

    /// <summary>
    /// Set animator parameter for a <typeparamref name="TState"/>
    /// </summary>
    private void SetAnimatorParameter(TState state, bool value)
    {
        if (animator == null) return;

        // Idle -> isIdle
        if (stateToAnimatorParam.TryGetValue(state, out string paramName))
        {
            if (animator.parameters != null)
            {
                foreach (var param in animator.parameters)
                {
                    if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                    {
                        animator.SetBool(paramName, value);
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check if currently in a specific <typeparamref name="TState"/>
    /// </summary>
    public bool IsInState(TState state)
    {
        return EqualityComparer<TState>.Default.Equals(currentState, state);
    }

    /// <summary>
    /// Get the current <typeparamref name="TState"/>
    /// </summary>
    public TState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Events for <typeparamref name="TState"/> changes
    /// Call after Exit Actions <para/>
    /// See Usage <see cref="ChangeState"/>
    /// </summary>
    public event Action<TState, TState> OnStateChanged;

    /// <summary>
    /// Event triggered when a <typeparamref name="TState"/> is entered
    /// Call When entering the new state  <para/>
    /// See Usage <see cref="ChangeState"/>
    /// </summary>
    public event Action<TState> OnStateEnter;

    /// <summary>
    /// Event triggered when a <typeparamref name="TState"/> is exited <para/>
    /// See Usage <see cref="ChangeState"/>
    /// </summary>
    public event Action<TState> OnStateExit;
}
