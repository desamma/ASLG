using UnityEngine;
using TMPro;

public class GameSetup : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform;
    public TMP_Text playerNameText; // Kéo Text trên đầu Player vào đây

    [Header("Summoner Settings")]
    public GameObject aliciaPrefab;

    void Start()
    {
        // 1. Cập nhật tên lên đầu Player
        if (playerNameText != null)
        {
            playerNameText.text = GameSession.PlayerName;
        }

        if (StatsManager.instance == null) return;

        // 2. XỬ LÝ: ĐẤU SĨ (Nhân đôi Máu và Sát thương)
        if (GameSession.PlayerClass == 1)
        {
            StatsManager.instance.maxHealth *= 2;
            StatsManager.instance.currentHealth = StatsManager.instance.maxHealth;
            StatsManager.instance.damage *= 2;
            Debug.Log("<color=red>CLASS ĐẤU SĨ: Đã nhân đôi chỉ số!</color>");
        }
        // 3. XỬ LÝ: SUMMONER (Triệu hồi Alicia)
        else if (GameSession.PlayerClass == 2)
        {
            if (aliciaPrefab != null && playerTransform != null)
            {
                // Spawn Alicia cạnh Player
                Vector3 spawnPos = playerTransform.position + new Vector3(2f, 0, 0);
                GameObject alicia = Instantiate(aliciaPrefab, spawnPos, Quaternion.identity);

                // Nối Alicia với hệ thống Chat LLM
                LLMChatManager llmManager = FindObjectOfType<LLMChatManager>();
                if (llmManager != null)
                {
                    llmManager.aliciaScript = alicia.GetComponent<NPCCompanion>();
                }
                Debug.Log("<color=cyan>CLASS SUMMONER: Alicia đã xuất hiện!</color>");
            }
        }
    }
}