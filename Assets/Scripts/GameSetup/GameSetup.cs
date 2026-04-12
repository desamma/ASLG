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

        // 2. Lấy thông tin Class hiện tại từ ClassManager (thay vì GameSession)
        if (ClassManager.Instance == null) return;
        
        PlayerClass currentClass = ClassManager.Instance.SelectedClass;

        // 3. XỬ LÝ: SUMMONER (Triệu hồi đồng đội Alicia)
        if (currentClass == PlayerClass.Summoner)
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