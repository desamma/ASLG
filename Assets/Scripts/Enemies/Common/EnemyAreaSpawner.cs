using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAreaSpawner : MonoBehaviour
{
    [System.Serializable]
    private class FarmDropEntry
    {
        public string itemID = "";
        [Range(0f, 1f)] public float dropRate = 1f;
    }

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 12f;

    [Header("Spawn Border")]
    [SerializeField, Min(0.1f)] private float spawnRadius = 8f;

    [Header("Spawn Config")]
    [SerializeField, Min(1)] private int maxAliveEnemies = 6;
    [SerializeField] private float spawnIntervalSeconds = 3f;
    [SerializeField] private List<GameObject> enemyPrefabs = new();

    [Header("Farm Drop Pool (Item ID + Drop Rate)")]
    [SerializeField] private List<FarmDropEntry> farmDropEntries = new();

    [Header("Enemy Level Scaling")]
    [SerializeField] private bool scaleEnemyLevelByPlayerLevel = true;
    [SerializeField] private int minLevelOffsetFromPlayer = -5;
    [SerializeField] private int maxLevelOffsetFromPlayer = 2;

    private readonly List<GameObject> _aliveEnemies = new();
    private Coroutine _spawnRoutine;
    private bool _spawnedAllPrefabsOnce;

    private void OnEnable()
    {
        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    public static void DropFarmLoot(EnemyAreaSpawner spawner)
    {
        if (spawner == null || InventoryManager.instance == null) return;

        string itemID = spawner.GetRandomFarmDropItemID();
        if (string.IsNullOrEmpty(itemID)) return;

        InventoryManager.instance.AddItem(itemID, 1);
    }

    private IEnumerator SpawnRoutine()
    {
        if (spawnIntervalSeconds < 0f)
        {
            while (true)
            {
                SpawnAllPrefabsOnce();
                yield return new WaitForSeconds(0.1f);
            }
        }

        while (true)
        {
            TrySpawnEnemy();
            yield return new WaitForSeconds(spawnIntervalSeconds);
        }
    }

    private void SpawnAllPrefabsOnce()
    {
        if (_spawnedAllPrefabsOnce) return;

        CleanupDeadReferences();
        if (!IsPlayerInDetectionRange()) return;
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            var prefab = enemyPrefabs[i];
            if (prefab == null) continue;

            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + (Vector3)randomOffset;
            GameObject spawnedEnemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            ApplyEnemySpawnLevel(spawnedEnemy);
            AttachDropHandler(spawnedEnemy);
            _aliveEnemies.Add(spawnedEnemy);
        }

        _spawnedAllPrefabsOnce = true;
    }

    private void TrySpawnEnemy()
    {
        CleanupDeadReferences();

        if (!IsPlayerInDetectionRange()) return;
        if (_aliveEnemies.Count >= maxAliveEnemies) return;
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (prefab == null) return;

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + (Vector3)randomOffset;

        GameObject spawnedEnemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        ApplyEnemySpawnLevel(spawnedEnemy);
        AttachDropHandler(spawnedEnemy);
        _aliveEnemies.Add(spawnedEnemy);
    }

    private void ApplyEnemySpawnLevel(GameObject enemy)
    {
        if (!scaleEnemyLevelByPlayerLevel || enemy == null) return;

        int spawnLevel = GetSpawnLevelFromPlayer();
        var components = enemy.GetComponents<MonoBehaviour>();

        foreach (var component in components)
        {
            if (component is IEnemy_Health enemyHealth)
            {
                TryAssignCurrentLevel(component, spawnLevel);
                enemyHealth.InitializeStats();
            }
        }
    }

    private void AttachDropHandler(GameObject enemy)
    {
        if (enemy == null) return;

        var handler = enemy.AddComponent<SpawnerEnemyDropHandler>();
        handler.Initialize(this);
    }

    private int GetSpawnLevelFromPlayer()
    {
        int playerLevel = StatsManager.instance != null ? StatsManager.instance.level : 1;

        int minLevel = Mathf.Max(1, playerLevel + minLevelOffsetFromPlayer);
        int maxLevel = Mathf.Max(minLevel, playerLevel + maxLevelOffsetFromPlayer);

        return Random.Range(minLevel, maxLevel + 1);
    }

    private static void TryAssignCurrentLevel(MonoBehaviour component, int level)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = component.GetType();

        var field = type.GetField("currentLevel", flags);
        if (field != null && field.FieldType == typeof(int))
        {
            field.SetValue(component, level);
            return;
        }

        var property = type.GetProperty("currentLevel", flags);
        if (property != null && property.CanWrite && property.PropertyType == typeof(int))
        {
            property.SetValue(component, level, null);
        }
    }

    private bool IsPlayerInDetectionRange()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        return distance <= detectionRadius;
    }

    private void CleanupDeadReferences()
    {
        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (_aliveEnemies[i] == null)
            {
                _aliveEnemies.RemoveAt(i);
            }
        }
    }

    public string GetRandomFarmDropItemID()
    {
        if (farmDropEntries == null || farmDropEntries.Count == 0) return null;

        EnsureFarmDropEntries();

        List<string> passedRollItemIDs = new();
        for (int i = 0; i < farmDropEntries.Count; i++)
        {
            var entry = farmDropEntries[i];
            if (entry == null || string.IsNullOrEmpty(entry.itemID)) continue;

            float dropRate = Mathf.Clamp01(entry.dropRate);
            if (Random.value <= dropRate)
            {
                passedRollItemIDs.Add(entry.itemID);
            }
        }

        if (passedRollItemIDs.Count == 0) return null;

        int passedIndex = Random.Range(0, passedRollItemIDs.Count);
        return passedRollItemIDs[passedIndex];
    }

    private void EnsureFarmDropEntries()
    {
        if (farmDropEntries == null)
        {
            farmDropEntries = new List<FarmDropEntry>();
        }

        for (int i = 0; i < farmDropEntries.Count; i++)
        {
            if (farmDropEntries[i] == null)
            {
                farmDropEntries[i] = new FarmDropEntry();
            }

            farmDropEntries[i].dropRate = Mathf.Clamp01(farmDropEntries[i].dropRate);
        }
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0.1f, detectionRadius);
        spawnRadius = Mathf.Max(0.1f, spawnRadius);
        maxAliveEnemies = Mathf.Max(1, maxAliveEnemies);

        if (maxLevelOffsetFromPlayer < minLevelOffsetFromPlayer)
            maxLevelOffsetFromPlayer = minLevelOffsetFromPlayer;
        EnsureFarmDropEntries();

        if (spawnIntervalSeconds >= 0f)
            spawnIntervalSeconds = Mathf.Max(0.2f, spawnIntervalSeconds);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(0.1f, 1f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
