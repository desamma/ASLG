using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;

// Biến Vector3 thành dạng an toàn cho JSON
[System.Serializable]
public class SVector3
{
    public float x, y, z;
    public SVector3() {}
    public SVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 Get() => new Vector3(x, y, z);
}

// ==================================================
// CÁC LỚP DỮ LIỆU TRUNG GIAN ĐỂ LƯU THÀNH JSON
// ==================================================
[System.Serializable]
public class CompanionSaveData
{
    public string npcID;
    public bool isActive;
    public SVector3 position;
    public int relationshipScore;
    public List<ChatMessage> chatHistory; // Ký ức LLM
}

[System.Serializable]
public class QuestSaveData
{
    public List<string> activeQuestIDs = new List<string>();
    public List<string> completedQuestIDs = new List<string>();
    public Dictionary<string, List<int>> questProgress = new Dictionary<string, List<int>>();
}

[System.Serializable]
public class InventorySaveData
{
    public List<ItemStack> bagItems = new List<ItemStack>();
    public string equippedWeapon = "";
    public Dictionary<string, string> equippedArmor = new Dictionary<string, string>();
    public List<string> equippedAccessories = new List<string>();
}

[System.Serializable]
public class StatsSaveData
{
    public int level;
    public int currentExp;
    public int upgradePoints;
    public float currentHealth;
    public float currentMana;
    public float currentStamina;
}

[System.Serializable]
public class GameSaveData
{
    public string userId;
    public string playerName;
    public string sceneName;
    public SVector3 playerPosition;

    public StatsSaveData stats = new StatsSaveData();
    public InventorySaveData inventory = new InventorySaveData();
    public QuestSaveData quests = new QuestSaveData();
    public List<CompanionSaveData> companions = new List<CompanionSaveData>();
}

// ==================================================
// SAVE MANAGER CORE
// ==================================================
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public GameSaveData currentSaveData = new GameSaveData();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreateSaveManager()
    {
        GameObject go = new GameObject("SaveManager_System");
        Instance = go.AddComponent<SaveManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static string GetSaveFilePath()
    {
        string uid = TokenManager.GetUserId();
        if (string.IsNullOrEmpty(uid)) uid = "guest";
        return Application.persistentDataPath + "/" + uid + ".json";
    }

    public static bool HasSaveFile()
    {
        return File.Exists(GetSaveFilePath());
    }

    // --- LƯU GAME ---
    public void SaveGame()
    {
        // BỨC TƯỜNG BẢO VỆ: CHỈ LƯU KHI ĐANG Ở TRONG GAME (CÓ PLAYER)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Tắt dòng log này nếu thấy phiền, nhưng nó giúp chặn lỗi lưu nhầm ở Scene Creation
            // Debug.LogWarning("[SaveManager] Không thấy Player. Hủy lệnh lưu để tránh hỏng file Save.");
            return; 
        }

        // 1. Dữ liệu Cơ bản
        currentSaveData.userId = string.IsNullOrEmpty(TokenManager.GetUserId()) ? "guest" : TokenManager.GetUserId();
        currentSaveData.sceneName = SceneManager.GetActiveScene().name;
        currentSaveData.playerPosition = new SVector3(player.transform.position);

        // 2. Dữ liệu Stats
        if (StatsManager.instance != null)
        {
            currentSaveData.playerName = StatsManager.instance.playerName;
            currentSaveData.stats.level = StatsManager.instance.level;
            currentSaveData.stats.currentExp = StatsManager.instance.currentExp;
            currentSaveData.stats.upgradePoints = StatsManager.instance.upgradePoints;
            currentSaveData.stats.currentHealth = StatsManager.instance.currentHealth;
            currentSaveData.stats.currentMana = StatsManager.instance.currentMana;
            currentSaveData.stats.currentStamina = StatsManager.instance.currentStamina;
        }

        // 3. Dữ liệu Inventory
        if (InventoryManager.instance != null)
        {
            currentSaveData.inventory.bagItems = new List<ItemStack>(InventoryManager.instance.bagItems);
            currentSaveData.inventory.equippedWeapon = InventoryManager.instance.equippedWeapon;
            currentSaveData.inventory.equippedArmor = new Dictionary<string, string>(InventoryManager.instance.equippedArmor);
            currentSaveData.inventory.equippedAccessories = new List<string>(InventoryManager.instance.equippedAccessories);
        }

        // 4. Dữ liệu Quests
        if (QuestManager.instance != null)
        {
            currentSaveData.quests = QuestManager.instance.ExportSaveData();
        }

        // 5. Dữ liệu NPC & AI
        currentSaveData.companions.Clear();
        NPCCompanion[] allNPCs = FindObjectsOfType<NPCCompanion>();
        LLMChatManager chatManager = FindObjectOfType<LLMChatManager>();

        foreach (var npc in allNPCs)
        {
            CompanionSaveData compData = new CompanionSaveData
            {
                npcID = npc.npcID,
                isActive = npc.gameObject.activeInHierarchy,
                position = new SVector3(npc.transform.position),
                relationshipScore = npc.relationshipScore
            };

            if (chatManager != null && chatManager.aliciaScript == npc)
            {
                compData.chatHistory = new List<ChatMessage>(chatManager.GetChatHistory());
            }
            currentSaveData.companions.Add(compData);
        }

        // Ghi file JSON
        string json = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);
        File.WriteAllText(GetSaveFilePath(), json);
        Debug.Log($"<color=cyan>[SaveManager] Auto-Saved to: {GetSaveFilePath()}</color>");
    }

    // --- NẠP GAME ---
    public void LoadGame()
    {
        if (HasSaveFile())
        {
            string json = File.ReadAllText(GetSaveFilePath());
            currentSaveData = JsonConvert.DeserializeObject<GameSaveData>(json);

            if (StatsManager.instance != null)
            {
                StatsManager.instance.LoadSavedStats(
                    currentSaveData.stats.level, currentSaveData.stats.currentExp, currentSaveData.stats.upgradePoints,
                    currentSaveData.stats.currentHealth, currentSaveData.stats.currentMana, currentSaveData.stats.currentStamina,
                    currentSaveData.playerName
                );
            }

            if (InventoryManager.instance != null)
            {
                InventoryManager.instance.bagItems = currentSaveData.inventory.bagItems;
                InventoryManager.instance.equippedWeapon = currentSaveData.inventory.equippedWeapon;
                InventoryManager.instance.equippedArmor = currentSaveData.inventory.equippedArmor;
                InventoryManager.instance.equippedAccessories = currentSaveData.inventory.equippedAccessories;
                InventoryManager.instance.ForceUIUpdate();
            }

            if (QuestManager.instance != null)
            {
                QuestManager.instance.ImportSaveData(currentSaveData.quests);
            }

            Debug.Log("<color=cyan>[SaveManager] Loaded file, transitioning scene...</color>");
            SceneManager.LoadScene(currentSaveData.sceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Luôn bật Coroutine AutoSave, nhưng bên trong hàm SaveGame() đã có check Player == null để tự chặn
        StopCoroutine(nameof(AutoSaveRoutine));
        StartCoroutine(nameof(AutoSaveRoutine));
        
        StartCoroutine(ApplySceneDataRoutine());
    }

    private IEnumerator ApplySceneDataRoutine()
    {
        // Đợi 2 frames để chắc chắn hệ thống PlayerSpawner của bạn đã đẻ ra Player
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); 

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && currentSaveData.playerPosition != null)
        {
            player.transform.position = currentSaveData.playerPosition.Get();
        }

        NPCCompanion[] allNPCs = FindObjectsOfType<NPCCompanion>();
        LLMChatManager chatManager = FindObjectOfType<LLMChatManager>();

        foreach (var npc in allNPCs)
        {
            var savedNpc = currentSaveData.companions.Find(c => c.npcID == npc.npcID);
            if (savedNpc != null)
            {
                npc.gameObject.SetActive(savedNpc.isActive);
                npc.transform.position = savedNpc.position.Get();
                npc.relationshipScore = savedNpc.relationshipScore;
                npc.UpdateRelationshipUI();

                if (chatManager != null && chatManager.aliciaScript == npc)
                {
                    chatManager.SetChatHistory(savedNpc.chatHistory);
                }
            }
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f); // Lưu mỗi 10 giây
            SaveGame();
        }
    }
}