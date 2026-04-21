using UnityEngine;

/// <summary>
/// Attached to enemies spawned by EnemyAreaSpawner to handle drops when they die.
/// This script detects when the enemy GameObject is destroyed and triggers loot drops.
/// </summary>
[DisallowMultipleComponent]
public class SpawnerEnemyDropHandler : MonoBehaviour
{
    private EnemyAreaSpawner _spawner;
    private bool _hasDropped = false;

    public void Initialize(EnemyAreaSpawner spawner)
    {
        _spawner = spawner;
    }

    private void OnDestroy()
    {
        if (!_hasDropped && _spawner != null)
        {
            _hasDropped = true;
            EnemyAreaSpawner.DropFarmLoot(_spawner);
        }
    }
}
