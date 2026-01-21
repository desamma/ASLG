/// <summary>
/// Define movement, behavior, and interaction logic for enemy characters.
/// </summary>
public interface IEnemy_Movement
{
    /// <summary>
    /// Define a behavior profile for this enemy's movement
    /// </summary>
    void InitializeBehavior();

    /// <summary>
    /// method to subscribe when the game's difficulty changes
    /// </summary>
    /// <param name="newModifier">The new difficulty modifier applied to the game</param>
    void OnDifficultyChanged(DifficultyModifier newModifier);

    /// <summary>
    /// Reverses the orientation or state of the object.
    /// </summary>
    void Flip();

    /// <summary>
    /// Checks for the presence of a player in the relevant context.
    /// </summary>
    void CheckForPlayer();

    /// <summary>
    /// Causes the enemy to chase the player.
    /// </summary>
    void Chase();
}
