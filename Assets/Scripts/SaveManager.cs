using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Biến Vector3 thành dạng an toàn cho JSON
[Serializable]
public class SVector3
{
    public float x, y, z;
    public SVector3() { }
    public SVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 Get() => new Vector3(x, y, z);
}

// ==================================================
// CÁC LỚP DỮ LIỆU TRUNG GIAN ĐỂ LƯU THÀNH JSON
// ==================================================
[Serializable]
public class CompanionSaveData
{
    public string npcID;
    public bool isActive;
    public SVector3 position;
    public int relationshipScore;
    public List<ChatMessage> chatHistory; // Ký ức LLM
}

[Serializable]
public class QuestSaveData
{
    public List<string> activeQuestIDs = new List<string>();
    public List<string> completedQuestIDs = new List<string>();
    public Dictionary<string, List<int>> questProgress = new Dictionary<string, List<int>>();
}

[Serializable]
public class InventorySaveData
{
    public List<ItemStack> bagItems = new List<ItemStack>();
    public string equippedWeapon = "";
    public Dictionary<string, string> equippedArmor = new Dictionary<string, string>();
    public List<string> equippedAccessories = new List<string>();
}

[Serializable]
public class StatsSaveData
{
    public int level;
    public int currentExp;
    public int upgradePoints;
    public float currentHealth;
    public float currentMana;
    public float currentStamina;
}

[Serializable]
public class GameSaveData
{
    public string userId;
    public string playerName;
    public int playerClassIndex;
    public string sceneName;
    public SVector3 playerPosition;

    public StatsSaveData stats = new StatsSaveData();
    public InventorySaveData inventory = new InventorySaveData();
    public QuestSaveData quests = new QuestSaveData();
    public List<CompanionSaveData> companions = new List<CompanionSaveData>();

    public List<string> discoveredZones = new List<string>();
}

// ==================================================
// SAVE MANAGER CORE
// ==================================================
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public GameSaveData currentSaveData = new GameSaveData();

    [Header("Cloud Save")]
    [SerializeField] private bool enableOnlineActivity = true;
    [SerializeField] private string backendUrl = "https://localhost:7206";
    [SerializeField] private string firebaseStorageUploadApi = "/api/FirebaseStorage/upload";
    [SerializeField] private string firebaseStorageDownloadApi = "/api/FirebaseStorage/download";
    [SerializeField] private string firebaseStorageListApi = "/api/FirebaseStorage/list";
    [SerializeField] private string cloudSaveFolderPath = "userSaveFile";
    [SerializeField] private string userItemPendingDeliveryApi = "/api/UserItem/pending-delivery";
    [SerializeField] private string userItemAcknowledgeDeliveryApi = "/api/UserItem/acknowledge-delivery";

    private bool _isCloudSaving;
    private bool _isSaving;
    private bool _isSyncingWebItems;
    private bool _pendingPositionRestore = false;
    [Serializable]
    private class CloudListResponse
    {
        public List<string> files;
        public int count;
    }

    [Serializable]
    private class ApiResponse<T>
    {
        public string message;
        public T data;
    }

    [Serializable]
    private class PendingDeliveryItemInfo
    {
        public string dictionaryKey;
        public string name;
    }

    [Serializable]
    private class PendingDeliveryItem
    {
        public Guid itemId;
        public int quantity;
        public int quantityDelivered;
        public int quantityPending;
        public PendingDeliveryItemInfo item;
    }

    [Serializable]
    private class AcknowledgeDeliveryPayload
    {
        public List<AcknowledgeDeliveryItem> items = new List<AcknowledgeDeliveryItem>();
    }

    [Serializable]
    private class AcknowledgeDeliveryItem
    {
        public string itemDictionaryKey;
        public int quantity;
    }

    private string BuildApiUrl(string apiPath)
    {
        var baseUrl = (backendUrl ?? string.Empty).TrimEnd('/');
        var path = (apiPath ?? string.Empty).TrimStart('/');
        return baseUrl + "/" + path;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreateSaveManager()
    {
        GameObject go = new GameObject("SaveManager_System");
        Instance = go.AddComponent<SaveManager>();
        Instance.enableOnlineActivity = true;
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

    public static void OnLogin()
    {
        if (Instance._isSaving) return;
        if (!HasSaveFile() && Instance.enableOnlineActivity)
        {
            Instance.StartCoroutine(Instance.GetSaveFileFromWeb());
        }
    }

    public IEnumerator GetSaveFileFromWeb()
    {
        _isSaving = true;

        if (!HasSaveFile() && enableOnlineActivity)
        {
            yield return TryRestoreSaveFromCloudRoutine();
        }

        yield return SyncPendingWebItemsRoutine();
        _isSaving = false;
    }

    // --- LƯU GAME ---
    public void SaveGame()
    {
        if (_isSaving) return;
        StartCoroutine(SaveGameRoutine());
    }

    private IEnumerator SaveGameRoutine()
    {
        _isSaving = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            _isSaving = false;
            yield break;
        }

        if (!HasSaveFile() && enableOnlineActivity)
        {
            yield return TryRestoreSaveFromCloudRoutine();
        }

        yield return SyncPendingWebItemsRoutine();
        SaveGameToLocalFile();
        _isSaving = false;
    }

    private void SaveGameToLocalFile()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return; // Chặn lưu nếu chưa sinh ra Player

        // 1. Dữ liệu Cơ bản
        currentSaveData.userId = string.IsNullOrEmpty(TokenManager.GetUserId()) ? "guest" : TokenManager.GetUserId();
        currentSaveData.sceneName = SceneManager.GetActiveScene().name;
        currentSaveData.playerPosition = new SVector3(player.transform.position);

        // ĐÃ FIX: Báo cho Save File biết bạn đang chơi Class nào
        if (ClassManager.Instance != null)
        {
            currentSaveData.playerClassIndex = (int)ClassManager.Instance.SelectedClass;
        }

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

        // 6. Discovered Map Zones
        if (WorldMapManager.Instance != null)
        {
            currentSaveData.discoveredZones.Clear();
            foreach (var zone in WorldMapManager.Instance.zones)
            {
                if (zone.discovered)
                    currentSaveData.discoveredZones.Add(zone.zoneName);
            }
        }

        string json = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);
        File.WriteAllText(GetSaveFilePath(), json);
        Debug.Log($"<color=cyan>[SaveManager] Auto-Saved to: {GetSaveFilePath()}</color>");
    }

    private void SaveToCloud()
    {
        if (!enableOnlineActivity) return;
        if (_isCloudSaving) return;
        StartCoroutine(SaveToCloudRoutine());
    }

    private IEnumerator SaveToCloudRoutine()
    {
        string savePath = GetSaveFilePath();
        if (!File.Exists(savePath))
        {
            yield break;
        }

        string token = TokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[SaveManager] Skip cloud save: missing auth token.");
            yield break;
        }

        byte[] fileBytes = File.ReadAllBytes(savePath);
        if (fileBytes == null || fileBytes.Length == 0)
        {
            yield break;
        }

        _isCloudSaving = true;

        var form = new WWWForm();
        form.AddField("FolderPath", cloudSaveFolderPath);
        form.AddBinaryData("File", fileBytes, Path.GetFileName(savePath), "application/json");

        using (UnityWebRequest request = UnityWebRequest.Post(BuildApiUrl(firebaseStorageUploadApi), form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SaveManager] Cloud save failed ({request.responseCode}): {request.error}\n{request.downloadHandler.text}");
            }
        }

        _isCloudSaving = false;
    }

    public void LoadGame()
    {
        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        if (!HasSaveFile() && enableOnlineActivity)
        {
            yield return TryRestoreSaveFromCloudRoutine();
        }

        if (HasSaveFile())
        {
            LoadFromLocalFile();
            yield return SyncPendingWebItemsRoutine();
            ApplyCurrentSaveDataAndLoadScene();
        }
        else
        {
            Debug.Log("[SaveManager] No local or cloud save found. Treat as new player.");
        }
    }

    private void LoadFromLocalFile()
    {
        string json = File.ReadAllText(GetSaveFilePath());
        currentSaveData = JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    private void ApplyCurrentSaveDataAndLoadScene()
    {

        if (currentSaveData == null)
        {
            Debug.LogWarning("[SaveManager] Save data is null.");
            return;
        }

        // ĐÃ FIX: Nhét thông tin Class vào ClassManager TRƯỚC KHI chuyển Scene
        if (ClassManager.Instance != null)
        {
            ClassManager.Instance.SelectClass((PlayerClass)currentSaveData.playerClassIndex);
        }

        // Backup phòng hờ cho GameSession cũ của bạn
        GameSession.PlayerName = currentSaveData.playerName;
        GameSession.PlayerClass = currentSaveData.playerClassIndex;

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
        _pendingPositionRestore = true;
        SceneManager.LoadScene(currentSaveData.sceneName);
    }

    private IEnumerator SyncPendingWebItemsRoutine()
    {
        if (!enableOnlineActivity)
        {
            yield break;
        }

        if (_isSyncingWebItems)
        {
            yield break;
        }

        string token = TokenManager.GetToken();
        string userId = TokenManager.GetUserId();

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[SaveManager] Web item sync skipped: missing token or userId.");
            yield break;
        }

        if (ItemDatabase.Items == null)
        {
            ItemDatabase.Initialize();
        }

        _isSyncingWebItems = true;

        string pendingUrl = BuildApiUrl(userItemPendingDeliveryApi) + "/" + UnityWebRequest.EscapeURL(userId);

        using (UnityWebRequest pendingRequest = UnityWebRequest.Get(pendingUrl))
        {
            pendingRequest.SetRequestHeader("Authorization", "Bearer " + token);
            yield return pendingRequest.SendWebRequest();

            if (pendingRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SaveManager] Pending delivery request failed ({pendingRequest.responseCode}): {pendingRequest.error}");
                _isSyncingWebItems = false;
                yield break;
            }

            ApiResponse<List<PendingDeliveryItem>> response;
            try
            {
                response = DeserializePendingDeliveryResponse(pendingRequest.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Invalid pending delivery response: {ex.Message}");
                _isSyncingWebItems = false;
                yield break;
            }

            if (response == null || response.data == null || response.data.Count == 0)
            {
                _isSyncingWebItems = false;
                yield break;
            }

            var acknowledgedItems = new List<AcknowledgeDeliveryItem>();

            foreach (var webItem in response.data)
            {
                if (webItem == null)
                {
                    continue;
                }

                string webItemKey = GetWebItemDictionaryKey(webItem);
                if (string.IsNullOrEmpty(webItemKey))
                {
                    continue;
                }

                int pendingAmount = webItem.quantityPending > 0 ? webItem.quantityPending : (webItem.quantity - webItem.quantityDelivered);
                if (pendingAmount <= 0)
                {
                    continue;
                }

                string localItemId = ResolveLocalItemId(webItem);
                if (string.IsNullOrEmpty(localItemId))
                {
                    Debug.LogWarning($"[SaveManager] Cannot map web dictionaryKey '{webItemKey}' to local ItemDatabase item.");
                    continue;
                }

                if (InventoryManager.instance != null)
                {
                    InventoryManager.instance.AddItem(localItemId, pendingAmount);
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Cannot apply pending item '{webItemKey}': InventoryManager.instance is null.");
                    continue;
                }

                acknowledgedItems.Add(new AcknowledgeDeliveryItem
                {
                    itemDictionaryKey = webItemKey,
                    quantity = pendingAmount
                });
            }

            if (acknowledgedItems.Count > 0)
            {
                yield return AcknowledgeWebDeliveryRoutine(userId, token, acknowledgedItems);
            }
        }

        _isSyncingWebItems = false;
    }

    private IEnumerator AcknowledgeWebDeliveryRoutine(string userId, string token, List<AcknowledgeDeliveryItem> acknowledgedItems)
    {
        var payload = new AcknowledgeDeliveryPayload { items = acknowledgedItems };
        string body = JsonConvert.SerializeObject(payload);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

        string acknowledgeUrl = BuildApiUrl(userItemAcknowledgeDeliveryApi) + "/" + UnityWebRequest.EscapeURL(userId);

        using var acknowledgeRequest = new UnityWebRequest(acknowledgeUrl, UnityWebRequest.kHttpVerbPOST);
        acknowledgeRequest.uploadHandler = new UploadHandlerRaw(bodyBytes);
        acknowledgeRequest.downloadHandler = new DownloadHandlerBuffer();
        acknowledgeRequest.SetRequestHeader("Content-Type", "application/json");
        acknowledgeRequest.SetRequestHeader("Authorization", "Bearer " + token);

        yield return acknowledgeRequest.SendWebRequest();

        if (acknowledgeRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[SaveManager] Acknowledge delivery failed ({acknowledgeRequest.responseCode}): {acknowledgeRequest.error}");
        }
    }

    /// <summary>
    /// Check if id exists on local resource
    /// </summary>
    private string ResolveLocalItemId(PendingDeliveryItem webItem)
    {
        string webItemKey = GetWebItemDictionaryKey(webItem);

        if (!string.IsNullOrEmpty(webItemKey) && ItemDatabase.GetItem(webItemKey) != null)
        {
            return webItemKey;
        }

        //try matching by item name if failed to find by dictionaryKey
        if (webItem.item != null && !string.IsNullOrEmpty(webItem.item.name))
        {
            if (ItemDatabase.GetItem(webItem.item.name) != null)
            {
                return webItem.item.name;
            }

            foreach (var kv in ItemDatabase.Items)
            {
                if (string.Equals(kv.Value.name, webItem.item.name, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Key;
                }
            }
        }

        return null;
    }

    private string GetWebItemDictionaryKey(PendingDeliveryItem webItem)
    {
        if (webItem == null || webItem.item == null)
        {
            return null;
        }

        return !string.IsNullOrWhiteSpace(webItem.item.dictionaryKey) ? webItem.item.dictionaryKey : null;
    }

    private ApiResponse<List<PendingDeliveryItem>> DeserializePendingDeliveryResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<ApiResponse<List<PendingDeliveryItem>>>(json);
    }

    private IEnumerator TryRestoreSaveFromCloudRoutine()
    {
        if (!enableOnlineActivity)
        {
            yield break;
        }

        string token = TokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[SaveManager] Skip cloud restore: missing auth token.");
            yield break;
        }

        string localFileName = Path.GetFileName(GetSaveFilePath());
        string listUrl = BuildApiUrl(firebaseStorageListApi) + "?folderPath=" + UnityWebRequest.EscapeURL(cloudSaveFolderPath);

        using (UnityWebRequest listRequest = UnityWebRequest.Get(listUrl))
        {
            listRequest.SetRequestHeader("Authorization", "Bearer " + token);
            yield return listRequest.SendWebRequest();

            if (listRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SaveManager] Cloud list failed ({listRequest.responseCode}): {listRequest.error}");
                yield break;
            }

            CloudListResponse listResponse;
            try
            {
                listResponse = JsonConvert.DeserializeObject<CloudListResponse>(listRequest.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Invalid cloud list response: {ex.Message}");
                yield break;
            }

            if (listResponse == null || listResponse.files == null || listResponse.files.Count == 0)
            {
                yield break;
            }

            string cloudFilePath = null;
            foreach (var filePath in listResponse.files)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                if (string.Equals(Path.GetFileName(filePath), localFileName, StringComparison.OrdinalIgnoreCase))
                {
                    cloudFilePath = filePath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(cloudFilePath))
            {
                yield break;
            }

            if (!cloudFilePath.Contains("/"))
            {
                cloudFilePath = cloudSaveFolderPath.TrimEnd('/') + "/" + cloudFilePath;
            }

            string downloadUrl = BuildApiUrl(firebaseStorageDownloadApi) + "?filePath=" + UnityWebRequest.EscapeURL(cloudFilePath);

            using UnityWebRequest downloadRequest = UnityWebRequest.Get(downloadUrl);
            downloadRequest.SetRequestHeader("Authorization", "Bearer " + token);
            yield return downloadRequest.SendWebRequest();

            if (downloadRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SaveManager] Cloud download failed ({downloadRequest.responseCode}): {downloadRequest.error}");
                yield break;
            }

            byte[] bytes = downloadRequest.downloadHandler.data;
            if (bytes == null || bytes.Length == 0)
            {
                yield break;
            }

            File.WriteAllBytes(GetSaveFilePath(), bytes);
            Debug.Log($"<color=green>[SaveManager] Restored local save from cloud: {cloudFilePath}</color>");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopCoroutine(nameof(AutoSaveRoutine));
        StartCoroutine(nameof(AutoSaveRoutine));

        StartCoroutine(ApplySceneDataRoutine());
    }

    private IEnumerator ApplySceneDataRoutine()
    {
        yield return new WaitForEndOfFrame();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && _pendingPositionRestore && currentSaveData.playerPosition != null)
        {
            player.transform.position = currentSaveData.playerPosition.Get();
            _pendingPositionRestore = false;  // consume the flag
        }

        NPCCompanion[] allNPCs = FindObjectsOfType<NPCCompanion>();
        LLMChatManager chatManager = FindObjectOfType<LLMChatManager>();

        // Restore discovered map zones
        if (WorldMapManager.Instance != null && currentSaveData.discoveredZones != null)
        {
            foreach (var zoneName in currentSaveData.discoveredZones)
            {
                foreach (var zone in WorldMapManager.Instance.zones)
                {
                    if (zone.zoneName == zoneName && !zone.discovered)
                    {
                        zone.discovered = true;

                        // Instantly hide fog (no fade — already explored)
                        if (zone.fogCloud != null)
                        {
                            zone.fogCloud.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

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
        const float saveIntervalSeconds = 10f; // 10s
        const float cloudSaveIntervalSeconds = 600f; // 10 mins

        var elapsed = 0f;
        while (true)
        {
            yield return new WaitForSeconds(saveIntervalSeconds);
            SaveGame();

            elapsed += saveIntervalSeconds;
            if (elapsed >= cloudSaveIntervalSeconds)
            {
                SaveToCloud();
                elapsed -= cloudSaveIntervalSeconds;
            }
        }
    }
}