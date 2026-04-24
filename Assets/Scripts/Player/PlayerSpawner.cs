using UnityEngine;
using Cinemachine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint;

    [Header("Summoner Settings")]
    [SerializeField] private GameObject aliciaPrefab;

    private void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        var data = ClassManager.Instance?.CurrentClassData;
        if (data == null) return;

        if (data.playerPrefab == null)
        {
            Debug.LogWarning($"[PlayerSpawner] No prefab assigned on {data.playerClass} data.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        pos.z = 0; // Đảm bảo luôn nằm đúng mặt phẳng 2D

        // Đẻ ĐÚNG 1 cục Prefab duy nhất, đéo có cha con gì hết!
        var player = Instantiate(data.playerPrefab, pos, Quaternion.identity);

        // Gán Camera
        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null) vcam.Follow = player.transform;

        // Summoner logic
        if (ClassManager.Instance.SelectedClass == PlayerClass.Summoner && aliciaPrefab != null)
        {
            Vector3 spawnPos = player.transform.position + new Vector3(2f, 0, 0);
            GameObject alicia = Instantiate(aliciaPrefab, spawnPos, Quaternion.identity);
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