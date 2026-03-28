using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// To replace the uses of dictionary to a more generic class
/// </summary>
/// <remarks>
/// Old dictionary usage reference: <see cref="Enemy_ArgeonHighmayne_Movement"/>
/// <para/>
/// New usage reference: <seealso cref="Enemy_ArgeonHighmayneMK2_Movement"/>
/// </remarks>
/// <typeparam name="TState">Enemy's Enum State</typeparam>
public class EnemyMoveCooldownTracker<TState> where TState : Enum
{
    private readonly Dictionary<TState, int> counters = new();

    //Set Dictionary to 0 for all states
    public void Initialize(IEnumerable<TState> states)
    {
        counters.Clear();
        foreach (var state in states)
            counters[state] = 0;
    }

    // Tick down all cooldowns and reset the cooldown for the used attack
    public void Register(TState usedState, IEnumerable<(TState State, int Cooldown)> allAttacks)
    {
        var keys = counters.Keys.ToList();

        // Tick down counters for every OTHER attack
        foreach (var key in keys)
        {
            if (!EqualityComparer<TState>.Default.Equals(key, usedState) && counters[key] > 0)
                counters[key]--;
        }

        // Reset the cooldown counter for the attack that was just used
        foreach (var (state, cooldown) in allAttacks)
        {
            if (EqualityComparer<TState>.Default.Equals(state, usedState))
            {
                counters[usedState] = cooldown;
                return;
            }
        }
    }

    public bool IsOnCooldown(TState state) =>
        counters.TryGetValue(state, out int count) && count > 0;

}