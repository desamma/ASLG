using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        var data = ClassManager.Instance?.CurrentClassData;
        if (data == null)
        {
            Debug.LogWarning("[PlayerSpawner] No class data found — did ClassManager persist?");
            return;
        }

        if (data.playerPrefab == null)
        {
            Debug.LogWarning($"[PlayerSpawner] No prefab assigned on {data.playerClass} data.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        var player = Instantiate(data.playerPrefab, pos, Quaternion.identity);

        Debug.Log($"[PlayerSpawner] Spawned {data.playerClass} at {pos}.");
    }
}