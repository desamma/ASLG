/// <summary>
/// Define stats for enemy characters.
/// </summary>
public interface IEnemy_Health
{
    /// <summary>
    /// Define a stats profile for this enemy's movement
    /// </summary>
    void InitializeStats();

    /// <summary>
    /// method to subscribe when the game's difficulty changes
    /// </summary>
    /// <param name="newModifier">The new difficulty modifier applied to the game</param>
    void OnDifficultyChanged(DifficultyModifier newModifier);

    /// <summary>
    /// Change the enemy's health by the specified amount.
    /// </summary>
    /// <param name="amount">The amount of increase or decrease in health</param>
    void ChangeHealth(float amount);
}
