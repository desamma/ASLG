using UnityEngine;

/// <summary>
/// Attached to enemies spawned by EnemyAreaSpawner to handle drops when they die.
/// This script detects when the enemy GameObject is destroyed and triggers loot drops.
/// For phased bosses, it only drops loot when the final phase is defeated.
/// </summary>
[DisallowMultipleComponent]
public class SpawnerEnemyDropHandler : MonoBehaviour
{
    private EnemyAreaSpawner _spawner;
    private bool _hasDropped = false;
    private bool _suppressDrop;

    public void Initialize(EnemyAreaSpawner spawner)
    {
        _spawner = spawner;
    }

    public void SuppressDrop()
    {
        _suppressDrop = true;
    }

    public void ResumeDrop()
    {
        _suppressDrop = false;
    }

    public void TryDrop()
    {
        if (_hasDropped || _spawner == null || _suppressDrop) return;

        _hasDropped = true;
        EnemyAreaSpawner.DropFarmLoot(_spawner);
    }

    public SpawnerEnemyDropHandler TransferTo(GameObject nextPhase)
    {
        if (nextPhase == null || _spawner == null) return null;

        if (!nextPhase.TryGetComponent<SpawnerEnemyDropHandler>(out var nextHandler))
        {
            nextHandler = nextPhase.AddComponent<SpawnerEnemyDropHandler>();
        }

        nextHandler.Initialize(_spawner);
        nextHandler.ResumeDrop();
        SuppressDrop();
        return nextHandler;
    }

    private void OnDestroy()
    {
        TryDrop();
    }
}
