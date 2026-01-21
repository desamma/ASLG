/// <summary>
///  Define on hit attack event logic for enemy characters.
/// </summary>
/// <typeparam name="State">The type representing the state of the enemy attack.</typeparam>
public interface IEnemy_Attack<State>
{
    // Execute the chosen attack. Movement will set a pending attack state and call this when the wind-up animation is complete.
    void ExecuteAttack(State attackState);
}
