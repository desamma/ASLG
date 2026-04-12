using System;
using UnityEngine;

public class EnemyQuestTarget : MonoBehaviour
{
    [Header("Quest Target Settings")]
    [Tooltip("Select the specific type of this enemy. The Quest System uses this to track required kills (e.g., set to 'Slime' for a Slime prefab).")]
    public EnemyType myType;

    // Global broadcast channel for enemy deaths
    public static event Action<EnemyType> OnEnemyDied;

    /// <summary>
    /// Call this method from the enemy's Health/Stats script exactly when its HP reaches 0.
    /// </summary>
    public void NotifyDeath()
    {
        if (myType == EnemyType.None)
        {
            Debug.LogError($"[Missing Setup] The enemy '{gameObject.name}' has no EnemyType assigned in the EnemyQuestTarget script!");
            return;
        }

        OnEnemyDied?.Invoke(myType);
    }
}