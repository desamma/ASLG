﻿using Cinemachine;
using UnityEngine;

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
    [SerializeField] private GameObject johnsonPrefab; // Kéo thả Prefab của Johnson vào đây

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

    private void ClearStatusEffectHud()
    {
        var huds = FindObjectsOfType<StatusEffectHUD>();
        for (int i = 0; i < huds.Length; i++)
        {
            if (huds[i] != null)
                huds[i].Clear();
        }
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

        ClearStatusEffectHud();

        if (WorldMapManager.Instance != null && !string.IsNullOrWhiteSpace(arrivalZoneName))
            WorldMapManager.Instance.SetCurrentZone(arrivalZoneName);

        // 2. Tự động tìm Camera và gán Player vào ô Follow
        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null) vcam.Follow = player.transform;
        
        // Load persisted skill upgrade tier
        var playerSkill = player.GetComponent<PlayerSkill>();
        if (playerSkill != null && SaveManager.Instance != null)
        {
            playerSkill.LoadSkillTier(SaveManager.Instance.currentSaveData.stats.skillUpgradeTier);
        }

        LLMChatManager llmManager = FindObjectOfType<LLMChatManager>();

        // 1. Của Class Summoner - Spawn Alicia
        if (ClassManager.Instance.SelectedClass == PlayerClass.Summoner && aliciaPrefab != null)
        {
            Vector3 aliciaPos = player.transform.position + new Vector3(2f, 0, 0);
            GameObject alicia = Instantiate(aliciaPrefab, aliciaPos, Quaternion.identity);
            if (llmManager != null)
            {
                NPCCompanion companionScript = alicia.GetComponent<NPCCompanion>();
                if (companionScript != null) {
                    llmManager.RegisterCompanion(companionScript);
                    companionScript.playerTransform = player.transform;
                }
            }
        }

        // 2. Spawn Johnson nếu đã từng được mua bằng Vàng
        if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData.unlockedCompanions != null && SaveManager.Instance.currentSaveData.unlockedCompanions.Contains("npc_johnson") && johnsonPrefab != null)
        {
            Vector3 johnsonPos = player.transform.position + new Vector3(-2f, 0, 0); // Đứng ở bên trái Player
            GameObject johnson = Instantiate(johnsonPrefab, johnsonPos, Quaternion.identity);
            if (llmManager != null)
            {
                NPCCompanion companionScript = johnson.GetComponent<NPCCompanion>();
                if (companionScript != null) {
                    llmManager.RegisterCompanion(companionScript);
                    companionScript.playerTransform = player.transform;
                }
            }
        }
    }
}