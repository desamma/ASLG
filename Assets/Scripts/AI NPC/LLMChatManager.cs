using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

[System.Serializable]
public class OpenAiRequest { public List<ChatMessage> messages = new List<ChatMessage>(); public int max_tokens = 300; public float temperature = 0.7f; }
[System.Serializable]
public class ChatMessage { public string role; public string content; }
[System.Serializable]
public class OpenAiResponse { public List<OpenAiChoice> choices; }
[System.Serializable]
public class OpenAiChoice { public ChatMessage message; }

[System.Serializable]
public class GeminiRequest { public GeminiSystemInstruction systemInstruction; public List<GeminiContent> contents = new List<GeminiContent>(); public GeminiConfig generationConfig = new GeminiConfig(); }
[System.Serializable]
public class GeminiSystemInstruction { public string role = "system"; public List<GeminiPart> parts = new List<GeminiPart>(); }
[System.Serializable]
public class GeminiContent { public string role; public List<GeminiPart> parts = new List<GeminiPart>(); }
[System.Serializable]
public class GeminiPart { public string text; }
[System.Serializable]
public class GeminiConfig { public int maxOutputTokens = 2048; public float temperature = 0.5f; }
[System.Serializable]
public class GeminiResponse { public List<GeminiCandidate> candidates; }
[System.Serializable]
public class GeminiCandidate { public GeminiContent content; }

public class LLMChatManager : MonoBehaviour
{
    public static LLMChatManager Instance { get; private set; }
    [SerializeField] private bool enableOnlineActivity = true;

    [Header("Network Fallback System (Inspector)")]
    public List<string> geminiApiKeys = new List<string>();
    public List<string> openAiUrls = new List<string>();

    [Header("UI References")]
    public GameObject chatCanvas;
    public TMP_Text npcTextDisplay;
    public TMP_InputField playerInputField;
    public Button sendButton;
    public Button exitButton;

    [Header("AI Quest UI")]
    public Button questButton;        
    public GameObject aiQuestPanel;   
    public TMP_Text aiQuestText;      

    [Header("Relationship UI")]
    public TMP_Text relationshipTextDisplay;

    [Header("Game References")]
    public NPCCompanion activeNPC;
    private List<NPCCompanion> allCompanions = new List<NPCCompanion>();
    
    public PlayerMovement playerMovement;
    public PlayerAttack playerAttack;

    private Dictionary<string, List<ChatMessage>> allChatHistories = new Dictionary<string, List<ChatMessage>>();
    private List<ChatMessage> CurrentChatHistory => GetChatHistory(activeNPC != null ? activeNPC.npcID : "");

    private bool isChatting = false;
    public bool IsChatting => isChatting;
    private const string ULTIMATE_GEMINI_KEY = "AIzaSyDYF3fqeTVOf-BXBFV5zSv70au5sJ1yKaI";
    private bool UseOfflineConversation => GameSettings.IsOfflineMode || !enableOnlineActivity;

    private bool hasActiveAiQuest = false;
    private bool isGeneratingQuest = false;
    private string currentQuestLore = "";
    private float questTimer = 0f;
    private float questCooldownTimer = 0f;

    private int currentQuestType = 0; // 0: Chat, 1: Walk
    private int targetChatCount, currentChatCount;
    private float targetDistance, currentDistance;
    private int rewardExp, rewardGold, rewardRel;
    private Vector2 lastPlayerPos;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (chatCanvas != null) chatCanvas.SetActive(false);
        if (aiQuestPanel != null) aiQuestPanel.SetActive(false);

        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
        if (exitButton != null) exitButton.onClick.AddListener(CloseChat);
        if (questButton != null) questButton.onClick.AddListener(OnQuestButtonClicked);
        if (playerInputField != null) playerInputField.onSubmit.AddListener(delegate { OnSendClicked(); });

        // Fallback: Đảm bảo activeNPC (ví dụ: Alicia) đã gán trên Inspector được thêm vào danh sách
        if (activeNPC != null)
        {
            RegisterCompanion(activeNPC);
        }
    }

    void Update()
    {
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerAttack == null) playerAttack = FindObjectOfType<PlayerAttack>();

        if (isChatting && Input.GetKeyDown(KeyCode.Escape)) { CloseChat(); return; }

        // Cheat code hỗ trợ test game: Ctrl + { để trừ, Ctrl + } để cộng 100 điểm Relationship
        if (isChatting && activeNPC != null)
        {
            bool isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (isCtrlPressed && Input.GetKeyDown(KeyCode.LeftBracket)) // Phím [ hoặc {
            {
                activeNPC.relationshipScore -= 100;
                if (activeNPC.relationshipScore <= -500) activeNPC.TriggerAngryState();
                UpdateRelationshipUI();
            }
            else if (isCtrlPressed && Input.GetKeyDown(KeyCode.RightBracket)) // Phím ] hoặc }
            {
                activeNPC.relationshipScore += 100;
                UpdateRelationshipUI();
            }
        }

        if (playerInputField != null && playerInputField.isFocused) return;

        if (!isChatting && Input.GetKeyDown(KeyCode.E) && playerMovement != null)
        {
            NPCCompanion closestNPC = null;
            float minDistance = 2.5f;
            
            allCompanions.RemoveAll(npc => npc == null);
            foreach (var npc in allCompanions)
            {
                float dist = Vector2.Distance(playerMovement.transform.position, npc.transform.position);
                if (dist <= minDistance)
                {
                    minDistance = dist;
                    closestNPC = npc;
                }
            }
            if (closestNPC != null) {
                activeNPC = closestNPC;
                OpenChat();
            }
        }

        if (questCooldownTimer > 0) questCooldownTimer -= Time.deltaTime;

        if (hasActiveAiQuest)
        {
            questTimer -= Time.deltaTime;
            if (playerMovement != null)
            {
                float moved = Vector2.Distance(playerMovement.transform.position, lastPlayerPos);
                if (moved > 0.05f) 
                {
                    currentDistance += moved;
                    lastPlayerPos = playerMovement.transform.position;
                }
            }
            UpdateQuestUI();
            CheckQuestCompletion();
            if (questTimer <= 0) FailAiQuest();
        }
    }

    public void OpenChat() 
    { 
        if (chatCanvas == null || activeNPC == null) return; 
        isChatting = true; 
        chatCanvas.SetActive(true); 
        
        if (playerMovement != null) playerMovement.SetMovementLock(true); 
        if (playerAttack != null) playerAttack.enabled = false; 
        
        if (CurrentChatHistory.Count == 0) 
        {
            npcTextDisplay.text = $"{activeNPC.npcName}: Hello dear Adventurer! What brings you here?"; 
        }
        else
        {
            string lastMsg = $"{activeNPC.npcName}: Hello dear Adventurer! What brings you here?";
            for (int i = CurrentChatHistory.Count - 1; i >= 0; i--)
            {
                if (CurrentChatHistory[i].role == "assistant")
                {
                    string rawText = CurrentChatHistory[i].content;
                    Match match = Regex.Match(rawText, @"\[(?:REL|rel|Rel).*?([+-]?\d+)\]");
                    if (match.Success) rawText = rawText.Replace(match.Value, "").Trim();
                    lastMsg = $"{activeNPC.npcName}: " + rawText;
                    break;
                }
            }
            npcTextDisplay.text = lastMsg;
        }
        
        StartCoroutine(FocusInputDelay()); 
        
        UpdateRelationshipUI();
    }
    
    private IEnumerator FocusInputDelay() { yield return new WaitForEndOfFrame(); if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(playerInputField.gameObject); if (playerInputField != null) playerInputField.ActivateInputField(); }
    
    public void CloseChat() { if (chatCanvas == null) return; isChatting = false; chatCanvas.SetActive(false); if (playerMovement != null) playerMovement.SetMovementLock(false); if (playerAttack != null) playerAttack.enabled = true; }

    public void UpdateRelationshipUI()
    {
        if (relationshipTextDisplay == null || activeNPC == null) return;
        
        relationshipTextDisplay.text = "Relationship:\n" + activeNPC.relationshipScore.ToString();
    }

    public void OnSendClicked()
    {
        string userText = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;
        playerInputField.text = "";
        StartCoroutine(FocusInputDelay());

        // Cheat/Debug: Thêm URL OpenAI/Colab thủ công qua khung chat để phòng API web lỗi
        if (userText.StartsWith("api-url:", System.StringComparison.OrdinalIgnoreCase))
        {
            string newUrl = userText.Substring(8).Trim();
            if (!string.IsNullOrEmpty(newUrl) && !openAiUrls.Contains(newUrl))
            {
                openAiUrls.Insert(0, newUrl); // Nhét lên vị trí ưu tiên (index 0)
            }
            
            // Nạp luôn cho hệ thống đánh giá độ khó (AI Director)
            if (AIDifficultyManager.Instance != null && !string.IsNullOrEmpty(newUrl) && !AIDifficultyManager.Instance.openAiUrls.Contains(newUrl))
            {
                AIDifficultyManager.Instance.openAiUrls.Insert(0, newUrl);
            }

            npcTextDisplay.text = $"<color=green>[System]</color> Đã nạp thành công API URL dự phòng:\n{newUrl}";
            return;
        }
        
        // Cheat/Debug: Thêm Gemini API Key thủ công qua khung chat
        if (userText.StartsWith("api-gemini:", System.StringComparison.OrdinalIgnoreCase))
        {
            string newKey = userText.Substring(11).Trim();
            if (!string.IsNullOrEmpty(newKey) && !geminiApiKeys.Contains(newKey))
            {
                geminiApiKeys.Insert(0, newKey); // Nhét lên vị trí ưu tiên (index 0)
            }
            
            // Nạp luôn cho hệ thống đánh giá độ khó (AI Director)
            if (AIDifficultyManager.Instance != null && !string.IsNullOrEmpty(newKey) && !AIDifficultyManager.Instance.geminiApiKeys.Contains(newKey))
            {
                AIDifficultyManager.Instance.geminiApiKeys.Insert(0, newKey);
            }

            npcTextDisplay.text = $"<color=green>[System]</color> Đã nạp thành công Gemini API Key dự phòng:\n{newKey}";
            return;
        }

        npcTextDisplay.text = $"<i>{activeNPC?.npcName} is thinking...</i>";

        if (hasActiveAiQuest && currentChatCount < targetChatCount) 
        {
            currentChatCount++;
            CheckQuestCompletion();
        }
        StartCoroutine(SendWithFallbackRoutine(userText));
    }

    private void OnQuestButtonClicked()
    {
        if (isGeneratingQuest) return;
        if (hasActiveAiQuest) { npcTextDisplay.text = $"{activeNPC?.npcName}: You still not finished the quest!"; return; }
        if (questCooldownTimer > 0) { npcTextDisplay.text = $"{activeNPC?.npcName}: Comeback after {Mathf.CeilToInt(questCooldownTimer)} seconds."; return; }
        StartCoroutine(GenerateAiQuestRoutine());
    }

    private IEnumerator GenerateAiQuestRoutine()
    {
        isGeneratingQuest = true;
        npcTextDisplay.text = $"<i>{activeNPC?.npcName} is thinking...</i>";

        currentQuestType = Random.Range(0, 2); // Random 0 (Chat) hoặc 1 (Walk)
        
        targetChatCount = (currentQuestType == 0) ? Random.Range(3, 9) : 0;      
        targetDistance = (currentQuestType == 1) ? Random.Range(10f, 30f) : 0f;    
        rewardExp = Random.Range(50, 150);
        rewardGold = Random.Range(10, 50);
        rewardRel = Random.Range(1, 5);
        currentChatCount = 0;
        currentDistance = 0f;
        
        string loreText = null;

        if (UseOfflineConversation)
        {
            string[] offlineLores;
            if (currentQuestType == 0) {
                offlineLores = new string[] {
                    "Let's stay here and talk for a bit.",
                    "I want to know more about you. Tell me a story!"
                };
            } else {
                offlineLores = new string[] {
                    "Hey, let's stretch our legs. Walk with me!",
                    "I'm feeling adventurous. Can you handle a quick patrol?"
                };
            }
            loreText = offlineLores[Random.Range(0, offlineLores.Length)];
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            string questStats = "";
            if (currentQuestType == 0) {
                questStats = $"Target: Chat {targetChatCount} times. Rewards: {rewardExp} EXP, {rewardGold} Gold, {rewardRel} Relationship.";
            } else {
                questStats = $"Target: Walk {targetDistance:F0} meters. Rewards: {rewardExp} EXP, {rewardGold} Gold, {rewardRel} Relationship.";
            }
            string sysPrompt = $"{activeNPC?.systemPrompt}. Give the player a mini-quest based on these exact stats: {questStats}. Speak directly in English (2-3 sentences).";

            List<string> geminiKeys = GetGeminiKeys();
            Debug.Log($"<color=cyan>[LLM Quest]</color> Quét thấy {geminiKeys.Count} Gemini API Keys. Bắt đầu gọi...");
            foreach (string key in geminiKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                string maskedKey = key.Length > 10 ? key.Substring(0, 6) + "..." + key.Substring(key.Length - 4) : "***";
                Debug.Log($"<color=cyan>[LLM Quest]</color> Đang thử Gemini Key: {maskedKey}");
                yield return StartCoroutine(CallGeminiAPI_OneOff(key.Trim(), sysPrompt, (res) => loreText = res));
                if (!string.IsNullOrEmpty(loreText)) 
                {
                    Debug.Log($"<color=green>[LLM Quest]</color> Gọi thành công với Key {maskedKey}!");
                    break;
                }
            }

            if (string.IsNullOrEmpty(loreText))
            {
                List<string> colabUrls = GetColabUrls();
                Debug.LogWarning($"<color=orange>[LLM Quest Fallback]</color> Gemini thất bại. Quét thấy {colabUrls.Count} Colab/OpenAI URLs để tạo Quest.");
                foreach (string url in colabUrls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    Debug.Log($"<color=orange>[LLM Quest Fallback]</color> Đang thử kết nối URL: {url}");
                    List<ChatMessage> dummyHistory = new List<ChatMessage> { new ChatMessage { role = "user", content = sysPrompt } };
                    yield return StartCoroutine(CallOpenAiAPI(url.Trim(), "System Admin", dummyHistory, (res) => loreText = res));
                    if (!string.IsNullOrEmpty(loreText)) { Debug.Log($"<color=green>[LLM Quest Fallback]</color> Gọi thành công với URL: {url}"); break; }
                }
            }
        }

        isGeneratingQuest = false;

        if (!string.IsNullOrEmpty(loreText))
{
    currentQuestLore = loreText;
    // Sửa dòng này thành một câu mồi cố định hoặc ghép chuỗi ngắn gọn
    npcTextDisplay.text = $"{activeNPC?.npcName}: I just gave you a quest, check the notification panel!";
    
    hasActiveAiQuest = true;
    questTimer = 60f; 
    questCooldownTimer = 60f; 
    lastPlayerPos = playerMovement != null ? playerMovement.transform.position : Vector2.zero;
    
    if (aiQuestPanel != null) aiQuestPanel.SetActive(true);
    UpdateQuestUI();
    CurrentChatHistory.Add(new ChatMessage { role = "assistant", content = loreText });
}
    }

    private void UpdateQuestUI()
    {
        if (aiQuestText == null || !hasActiveAiQuest) return;
        
        string objectiveText = "";
        if (currentQuestType == 0) 
        {
            string chatColor = currentChatCount >= targetChatCount ? "green" : "white";
            objectiveText = $"<color={chatColor}>Chat: {currentChatCount} / {targetChatCount}</color>\n";
        }
        else 
        {
            string distColor = currentDistance >= targetDistance ? "green" : "white";
            objectiveText = $"<color={distColor}>Walk: {currentDistance:F1} / {targetDistance:F0} m</color>\n";
        }

        aiQuestText.text = $"<b><color=yellow>{activeNPC?.npcName}'s Request</color></b>\n<i>{currentQuestLore}</i>\n\n" +
                           objectiveText +
                           $"<color=red>Time: {Mathf.CeilToInt(questTimer)}s</color>";
    }

    private void CheckQuestCompletion()
{
    if (!hasActiveAiQuest) return;
    
    bool isComplete = (currentQuestType == 0 && currentChatCount >= targetChatCount) || 
                      (currentQuestType == 1 && currentDistance >= targetDistance);

    if (isComplete)
    {
        hasActiveAiQuest = false; // Đánh dấu false ngay để tránh trigger nhiều lần
        StartCoroutine(QuestCompleteRoutine());
    }
}

private IEnumerator QuestCompleteRoutine()
{
    // 1. Trả thưởng cho Player
    if (StatsManager.instance != null) { 
        StatsManager.instance.AddExp(rewardExp); 
        StatsManager.instance.AddGold(rewardGold); 
    }
    if (activeNPC != null) { 
        activeNPC.relationshipScore += rewardRel; 
        activeNPC.UpdateRelationshipUI(); 
    }

    // 2. Tạo chuỗi thông báo phần thưởng
    string rewardDetails = $"+{rewardExp} EXP, +{rewardGold} Gold, +{rewardRel} Relationship";

    // 3. Hiển thị lên Panel Quest thay vì đóng luôn
    if (aiQuestText != null) 
    {
        aiQuestText.text = $"<b><color=yellow>{activeNPC?.npcName}'s Request</color></b>\n\n" +
                           $"<color=green><b>Quest Completed!</b></color>\n" +
                           $"Reward:\n{rewardDetails}";
    }

    // 4. Nếu đang chat, NPC sẽ nói ra và lưu vào lịch sử
    if (isChatting && npcTextDisplay != null) 
    {
        string npcDialogue = $"{activeNPC?.npcName}: Excellent! You did a great job. Here is your reward: {rewardDetails}.";
        npcTextDisplay.text = npcDialogue;
        
        // Lưu câu khen ngợi này vào bộ nhớ để ngữ cảnh LLM tự nhiên hơn
        CurrentChatHistory.Add(new ChatMessage { role = "assistant", content = $"Excellent! You did a great job. Here is your reward: {rewardDetails}." });
    }

    // 5. Đợi 10 giây để Player kịp đọc
    yield return new WaitForSeconds(10f);

    // 6. Đóng Panel Quest
    if (aiQuestPanel != null) aiQuestPanel.SetActive(false);
}

    private void FailAiQuest() { hasActiveAiQuest = false; if (aiQuestPanel != null) aiQuestPanel.SetActive(false); if (isChatting) npcTextDisplay.text = $"{activeNPC?.npcName}: Thôi bỏ đi, cậu chậm quá."; }

    private IEnumerator SendWithFallbackRoutine(string userText)
    {
        if (ApiSettingsManager.Instance != null && !ApiSettingsManager.Instance.IsReady)
        {
            yield return new WaitUntil(() => ApiSettingsManager.Instance.IsReady);
        }

        if (UseOfflineConversation)
        {
            npcTextDisplay.text = $"{activeNPC?.npcName}: <color=red>(Offline Mode - No Connection)</color>";
            yield break;
        }

        //string systemPrompt = $"System: {activeNPC?.systemPrompt}. Current Relationship Score: {activeNPC?.relationshipScore}. Reply strictly in English (1-3 sentences). Append [REL: X] at the end.";
        string systemPrompt = $"System: {activeNPC?.systemPrompt}. Evaluate the user's latest message and append [REL: X] at the very end, where X is the relationship score CHANGE integer from -5 to +5 (e.g., [REL: -2] or [REL: +3]). Reply strictly in English (1-3 sentences).";
        CurrentChatHistory.Add(new ChatMessage { role = "user", content = userText + "\n\n(OOC: Append [REL: X])" });
        string aiRawResponse = null;

        List<string> geminiKeys = GetGeminiKeys();
        Debug.Log($"<color=cyan>[LLM Chat]</color> Quét thấy {geminiKeys.Count} Gemini API Keys. Bắt đầu gọi...");
        foreach (string key in geminiKeys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string maskedKey = key.Length > 10 ? key.Substring(0, 6) + "..." + key.Substring(key.Length - 4) : "***";
            Debug.Log($"<color=cyan>[LLM Chat]</color> Đang thử Gemini Key: {maskedKey}");
            yield return StartCoroutine(CallGeminiAPI(key.Trim(), systemPrompt, CurrentChatHistory, (result) => aiRawResponse = result));
            if (!string.IsNullOrEmpty(aiRawResponse)) 
            {
                Debug.Log($"<color=green>[LLM Chat]</color> Gọi thành công với Key {maskedKey}!");
                break; 
            }
        }

        // Nếu toàn bộ API Key Gemini chết (Lỗi 403), nhảy sang Fallback dùng URL Colab / Local AI
        if (string.IsNullOrEmpty(aiRawResponse))
        {
            List<string> colabUrls = GetColabUrls();
            Debug.LogWarning($"<color=orange>[LLM Chat Fallback]</color> Gemini sập. Quét thấy {colabUrls.Count} Colab/OpenAI URLs để chat.");
            foreach (string url in colabUrls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                Debug.Log($"<color=orange>[LLM Chat Fallback]</color> Đang thử kết nối URL: {url}");
                yield return StartCoroutine(CallOpenAiAPI(url.Trim(), systemPrompt, CurrentChatHistory, (result) => aiRawResponse = result));
                if (!string.IsNullOrEmpty(aiRawResponse)) { Debug.Log($"<color=green>[LLM Chat Fallback]</color> Gọi thành công với URL: {url}"); break; }
            }
        }

        if (!string.IsNullOrEmpty(aiRawResponse))
{
    // Cập nhật lại tin nhắn của User đang ở cuối mảng (trước khi AI xen vào)
    CurrentChatHistory[CurrentChatHistory.Count - 1].content = userText; 
    
    // Thêm tin nhắn của AI vào lịch sử
    ProcessAIResponse(aiRawResponse);
}
        else { npcTextDisplay.text = "<color=red>No Connection - Offline Mode</color>"; CurrentChatHistory.RemoveAt(CurrentChatHistory.Count - 1); }
    }

    private IEnumerator CallGeminiAPI(string apiKey, string sysPrompt, List<ChatMessage> history, System.Action<string> onComplete)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        GeminiRequest requestData = new GeminiRequest();
        requestData.systemInstruction = new GeminiSystemInstruction { parts = new List<GeminiPart> { new GeminiPart { text = sysPrompt } } };
        foreach (var msg in history) requestData.contents.Add(new GeminiContent { role = msg.role == "assistant" ? "model" : "user", parts = new List<GeminiPart> { new GeminiPart { text = msg.content } } });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) { GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text); if (res != null && res.candidates != null && res.candidates.Count > 0) onComplete?.Invoke(res.candidates[0].content.parts[0].text.Trim()); else onComplete?.Invoke(null); }
            else { string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : "No data"; Debug.LogError($"[Gemini Chat LỖI] Mã HTTP: {request.responseCode} - Lỗi: {request.error}\nChi tiết từ Server:\n{errorDetail}"); onComplete?.Invoke(null); }
        }
    }

    private IEnumerator CallGeminiAPI_OneOff(string apiKey, string sysPrompt, System.Action<string> onComplete)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        GeminiRequest requestData = new GeminiRequest();
        requestData.systemInstruction = new GeminiSystemInstruction { parts = new List<GeminiPart> { new GeminiPart { text = "System Admin" } } };
        requestData.contents.Add(new GeminiContent { role = "user", parts = new List<GeminiPart> { new GeminiPart { text = sysPrompt } } });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) { GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text); if (res != null && res.candidates != null && res.candidates.Count > 0) onComplete?.Invoke(res.candidates[0].content.parts[0].text.Trim()); else onComplete?.Invoke(null); }
            else { string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : "No data"; Debug.LogError($"[Gemini Quest LỖI] Mã HTTP: {request.responseCode} - Lỗi: {request.error}\nChi tiết từ Server:\n{errorDetail}"); onComplete?.Invoke(null); }
        }
    }

    private IEnumerator CallOpenAiAPI(string url, string sysPrompt, List<ChatMessage> history, System.Action<string> onComplete)
    {
        string endpoint = url.TrimEnd('/');
        // Tự động thêm hậu tố /v1/chat/completions nếu bạn nhập thiếu ở URL
        if (!endpoint.EndsWith("/v1/chat/completions")) endpoint += "/v1/chat/completions";

        OpenAiRequest requestData = new OpenAiRequest();
        requestData.messages.Add(new ChatMessage { role = "system", content = sysPrompt });
        foreach (var msg in history) 
        {
            requestData.messages.Add(new ChatMessage { role = msg.role, content = msg.content });
        }

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success) { 
                OpenAiResponse res = JsonUtility.FromJson<OpenAiResponse>(request.downloadHandler.text); 
                if (res != null && res.choices != null && res.choices.Count > 0 && res.choices[0].message != null) onComplete?.Invoke(res.choices[0].message.content.Trim()); else onComplete?.Invoke(null); 
            }
            else { string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : "No data"; Debug.LogError($"[OpenAI/Colab Chat LỖI] Tại URL: {endpoint}\nMã HTTP: {request.responseCode} - Lỗi: {request.error}\nChi tiết từ Server:\n{errorDetail}"); onComplete?.Invoke(null); }
        }
    }

    private List<string> GetGeminiKeys() { List<string> keys = new List<string>(); if (ApiSettingsManager.Instance != null) keys.AddRange(ApiSettingsManager.Instance.WebGeminiKeys); keys.AddRange(geminiApiKeys); keys.Add(ULTIMATE_GEMINI_KEY); return keys; }
    private List<string> GetColabUrls() { List<string> urls = new List<string>(); if (ApiSettingsManager.Instance != null) urls.AddRange(ApiSettingsManager.Instance.WebColabUrls); urls.AddRange(openAiUrls); return urls; }
    
    private void ProcessAIResponse(string aiRawText)
    {
        Match match = Regex.Match(aiRawText, @"\[(?:REL|rel|Rel).*?([+-]?\d+)\]");
        string displayString = aiRawText;
        if (match.Success && activeNPC != null) { int relChange = int.Parse(match.Groups[1].Value); activeNPC.relationshipScore += relChange; if (relChange < 0 && activeNPC.relationshipScore <= -500) activeNPC.TriggerAngryState(); displayString = aiRawText.Replace(match.Value, "").Trim(); }
        CurrentChatHistory.Add(new ChatMessage { role = "assistant", content = aiRawText });
        npcTextDisplay.text = $"{activeNPC?.npcName}: " + displayString;
        if (activeNPC != null) activeNPC.UpdateRelationshipUI();
    }

    public void RegisterCompanion(NPCCompanion npc)
    {
        if (!allCompanions.Contains(npc)) allCompanions.Add(npc);
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerAttack == null) playerAttack = FindObjectOfType<PlayerAttack>();
    }

    public List<ChatMessage> GetChatHistory(string npcID) { if (!allChatHistories.ContainsKey(npcID)) allChatHistories[npcID] = new List<ChatMessage>(); return allChatHistories[npcID]; }
    public void SetChatHistory(string npcID, List<ChatMessage> history) { allChatHistories[npcID] = history ?? new List<ChatMessage>(); }
}