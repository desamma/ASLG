using UnityEngine;

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
    /// Checks for the presence of a player in the relevant context.
    /// </summary>
    void CheckForPlayer();

    /// <summary>
    /// Causes the enemy to chase the player.
    /// </summary>
    void Chase();

    /// <summary>
    /// Applys a knockback effect to the enemy, pushing them away from the player for a certain duration and applying a stun effect.
    /// </summary>
    /// <param name="player">The player causing the knockback.</param>
    /// <param name="knockbackForce">The force applied to the enemy.</param>
    /// <param name="knockbackTime">The duration of the knockback effect.</param>
    /// <param name="stunTime">The duration of the stun effect.</param>
    /// <param name="isKnockbackable">Indicates whether the enemy can be knocked back.</param>
    void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime, bool isKnockbackable);
}
