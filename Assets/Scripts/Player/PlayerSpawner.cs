using UnityEngine;
using Cinemachine; // 1. Thêm thư viện Cinemachine
using TMPro;

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
        if (data == null)
        {
            Debug.LogWarning("[PlayerSpawner] No class data found - did ClassManager persist?");
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

        // Cập nhật tên Player lên TMP_Text trên đầu nhân vật
        TMP_Text nameText = player.GetComponentInChildren<TMP_Text>();
        if (nameText != null && StatsManager.instance != null)
        {
            nameText.text = StatsManager.instance.playerName;
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawner] Không tìm thấy TMP_Text trên prefab {data.playerClass}. Bạn cần mở Prefab này và thêm UI -> TextMeshPro nhé!");
        }

        // 2. Tự động tìm Camera và gán Player vào ô Follow
        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null)
        {
            vcam.Follow = player.transform;
            Debug.Log("[PlayerSpawner] Đã gán Camera bám theo Player.");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] Không tìm thấy CinemachineVirtualCamera trong Scene!");
        }

        // 3. Xử lý logic spawn Alicia nếu là Class Summoner
        if (ClassManager.Instance.SelectedClass == PlayerClass.Summoner)
        {
            if (aliciaPrefab != null)
            {
                Vector3 spawnPos = player.transform.position + new Vector3(2f, 0, 0);
                GameObject alicia = Instantiate(aliciaPrefab, spawnPos, Quaternion.identity);

                // Gắn tự động Alicia vào LLMChatManager
                LLMChatManager llmManager = FindObjectOfType<LLMChatManager>();
                if (llmManager != null)
                {
                    NPCCompanion companionScript = alicia.GetComponent<NPCCompanion>();
                    llmManager.aliciaScript = companionScript;
                    if (companionScript != null) companionScript.playerTransform = player.transform;
                }
                Debug.Log("<color=cyan>[PlayerSpawner] CLASS SUMMONER: Alicia đã xuất hiện cùng Player!</color>");
            }
        }
    }
}