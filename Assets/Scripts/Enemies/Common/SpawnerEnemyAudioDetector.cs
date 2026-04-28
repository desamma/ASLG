using UnityEngine;

[DisallowMultipleComponent]
public class SpawnerEnemyAudioDetector : MonoBehaviour
{
    private EnemyAreaSpawner _spawner;
    private bool _hasNotifiedSpawnerOfDetection;

    public void Initialize(EnemyAreaSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Update()
    {
        if (_hasNotifiedSpawnerOfDetection) return;
        if (_spawner == null) return;

        if (IsEnemyInCombatState())
        {
            _hasNotifiedSpawnerOfDetection = true;
            _spawner.OnEnemyDetectedPlayer();
        }
    }

    private bool IsEnemyInCombatState()
    {
        var components = GetComponents<MonoBehaviour>();

        foreach (var behaviour in components)
        {
            if (behaviour is IEnemyMovementContext component)
                return component.IsInAnyAttackState();
        }

        return false;
    }
} 