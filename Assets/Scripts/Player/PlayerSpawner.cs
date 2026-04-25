using UnityEngine;
using Cinemachine;

[System.Serializable]
public class SpawnEntry
{
    [Tooltip("The zoneName of the MapZoneSetter that brought the player HERE")]
    public string fromZoneName;
    [Tooltip("Where to spawn the player when arriving from that zone")]
    public Transform spawnPoint;
}

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private SpawnEntry[] spawnEntries;

    [Header("Fallback Spawn")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Header("World Map")]
    [Tooltip("The WorldMapManager zoneName for this scene, clears fog and updates pin on arrival")]
    [SerializeField] private string arrivalZoneName;

    [Header("Summoner Settings")]
    [SerializeField] private GameObject aliciaPrefab;

    private void Start()
    {
        SpawnPlayer();
    }

    private Transform ResolveSpawnPoint()
    {
        string origin = MapSceneTransitionState.OriginZoneName;
        if (!string.IsNullOrEmpty(origin) && spawnEntries != null)
        {
            foreach (var entry in spawnEntries)
            {
                if (entry.fromZoneName == origin && entry.spawnPoint != null)
                    return entry.spawnPoint;
            }
        }
        return defaultSpawnPoint;
    }

    private void SpawnPlayer()
    {
        var data = ClassManager.Instance.CurrentClassData;
        if (data == null) { Debug.LogWarning("[PlayerSpawner] No class data."); return; }
        if (data.playerPrefab == null) { Debug.LogWarning("[PlayerSpawner] No prefab."); return; }

        Transform spawn = ResolveSpawnPoint();
        Vector3 pos = spawn != null ? spawn.position : Vector3.zero;

        var player = Instantiate(data.playerPrefab, pos, Quaternion.identity);

        Debug.Log($"[PlayerSpawner] Spawned {data.playerClass} at {pos}.");

        if (WorldMapManager.Instance != null && !string.IsNullOrWhiteSpace(arrivalZoneName))
            WorldMapManager.Instance.SetCurrentZone(arrivalZoneName);

        // 2. Tự động tìm Camera và gán Player vào ô Follow
        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null) vcam.Follow = player.transform;

        if (ClassManager.Instance.SelectedClass == PlayerClass.Summoner && aliciaPrefab != null)
        {
            Vector3 aliciaPos = player.transform.position + new Vector3(2f, 0, 0);
            GameObject alicia = Instantiate(aliciaPrefab, aliciaPos, Quaternion.identity);
            LLMChatManager llmManager = FindObjectOfType<LLMChatManager>();
            if (llmManager != null)
            {
                NPCCompanion companionScript = alicia.GetComponent<NPCCompanion>();
                llmManager.aliciaScript = companionScript;
                if (companionScript != null) companionScript.playerTransform = player.transform;
            }
        }
    }
}