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

    [Header("Game References")]
    public NPCCompanion aliciaScript;
    public PlayerMovement playerMovement;
    public PlayerAttack playerAttack;

    [SerializeField] private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isChatting = false;
    private const string ULTIMATE_GEMINI_KEY = "AIzaSyDYF3fqeTVOf-BXBFV5zSv70au5sJ1yKaI"; 
    private bool UseOfflineConversation => GameSettings.IsOfflineMode || !enableOnlineActivity;

    private bool hasActiveAiQuest = false;
    private bool isGeneratingQuest = false;
    private string currentQuestLore = "";
    private float questTimer = 0f;
    private float questCooldownTimer = 0f;

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
    }

    void Update()
    {
        if (aliciaScript == null) aliciaScript = FindObjectOfType<NPCCompanion>();
        if (aliciaScript != null && (playerMovement == null || playerAttack == null))
        {
            if (aliciaScript.playerTransform != null)
            {
                playerMovement = aliciaScript.playerTransform.GetComponent<PlayerMovement>();
                playerAttack = aliciaScript.playerTransform.GetComponent<PlayerAttack>();
            }
        }

        if (isChatting && Input.GetKeyDown(KeyCode.Escape)) { CloseChat(); return; }
        if (playerInputField != null && playerInputField.isFocused) return;

        if (!isChatting && Input.GetKeyDown(KeyCode.E) && aliciaScript != null && playerMovement != null)
        {
            float dist = Vector2.Distance(playerMovement.transform.position, aliciaScript.transform.position);
            if (dist <= 2.5f) OpenChat();
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

    public void OpenChat() { if (chatCanvas == null) return; isChatting = true; chatCanvas.SetActive(true); if (playerMovement != null) playerMovement.SetMovementLock(true); if (playerAttack != null) playerAttack.enabled = false; if (chatHistory.Count == 0) npcTextDisplay.text = "Alicia: Cậu cần gì sao?"; StartCoroutine(FocusInputDelay()); }
    private IEnumerator FocusInputDelay() { yield return new WaitForEndOfFrame(); if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(playerInputField.gameObject); if (playerInputField != null) playerInputField.ActivateInputField(); }
    public void CloseChat() { if (chatCanvas == null) return; isChatting = false; chatCanvas.SetActive(false); if (playerMovement != null) playerMovement.SetMovementLock(false); if (playerAttack != null) playerAttack.enabled = true; }

    public void OnSendClicked()
    {
        string userText = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;
        playerInputField.text = "";
        StartCoroutine(FocusInputDelay());
        npcTextDisplay.text = "<i>Alicia is thinking...</i>";

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
        if (hasActiveAiQuest) { npcTextDisplay.text = "Alicia: Cậu đang làm dở nhiệm vụ tôi giao mà!"; return; }
        if (questCooldownTimer > 0) { npcTextDisplay.text = $"Alicia: Quay lại sau {Mathf.CeilToInt(questCooldownTimer)} giây nữa nhé."; return; }
        StartCoroutine(GenerateAiQuestRoutine());
    }

    private IEnumerator GenerateAiQuestRoutine()
    {
        isGeneratingQuest = true;
        npcTextDisplay.text = "<i>Alicia đang nghĩ ra thử thách...</i>";

        targetChatCount = Random.Range(3, 9);      
        targetDistance = Random.Range(5f, 16f);    
        rewardExp = Random.Range(50, 150);
        rewardGold = Random.Range(10, 50);
        rewardRel = Random.Range(1, 5);
        currentChatCount = 0;
        currentDistance = 0f;
        
        string loreText = null;

        // KIỂM TRA CHẾ ĐỘ OFFLINE
        if (UseOfflineConversation)
        {
            string[] offlineLores = {
                "Hey, let's stretch our legs. Walk with me and chat a bit!",
                "I'm feeling adventurous. Can you handle a quick patrol?",
                "I want to see more of this area. Follow me and keep me company.",
                "Let's move! Exercise is essential for mages like us."
            };
            loreText = offlineLores[Random.Range(0, offlineLores.Length)];
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            string questStats = $"Target: Chat {targetChatCount} times, Walk {targetDistance:F0} meters. Rewards: {rewardExp} EXP, {rewardGold} Gold, {rewardRel} Relationship.";
            string sysPrompt = $"You are Alicia, an adventurer. Give the player a mini-quest based on these exact stats: {questStats}. Speak directly in English (2-3 sentences).";

            foreach (string key in GetGeminiKeys())
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                yield return StartCoroutine(CallGeminiAPI_OneOff(key.Trim(), sysPrompt, (res) => loreText = res));
                if (!string.IsNullOrEmpty(loreText)) break;
            }
        }

        isGeneratingQuest = false;

        if (!string.IsNullOrEmpty(loreText))
        {
            currentQuestLore = loreText;
            npcTextDisplay.text = "Alicia: " + loreText;
            hasActiveAiQuest = true;
            questTimer = 60f; 
            questCooldownTimer = 60f; 
            lastPlayerPos = playerMovement != null ? playerMovement.transform.position : Vector2.zero;
            if (aiQuestPanel != null) aiQuestPanel.SetActive(true);
            UpdateQuestUI();
            chatHistory.Add(new ChatMessage { role = "assistant", content = loreText });
        }
    }

    private void UpdateQuestUI()
    {
        if (aiQuestText == null || !hasActiveAiQuest) return;
        string distColor = currentDistance >= targetDistance ? "green" : "white";
        string chatColor = currentChatCount >= targetChatCount ? "green" : "white";
        aiQuestText.text = $"<b><color=yellow>Alicia's Request</color></b>\n<i>{currentQuestLore}</i>\n\n" +
                           $"<color={distColor}>Walk: {currentDistance:F1} / {targetDistance:F0} m</color>\n" +
                           $"<color={chatColor}>Chat: {currentChatCount} / {targetChatCount}</color>\n" +
                           $"<color=red>Time: {Mathf.CeilToInt(questTimer)}s</color>";
    }

    private void CheckQuestCompletion()
    {
        if (!hasActiveAiQuest) return;
        if (currentDistance >= targetDistance && currentChatCount >= targetChatCount)
        {
            hasActiveAiQuest = false;
            if (aiQuestPanel != null) aiQuestPanel.SetActive(false);
            if (StatsManager.instance != null) { StatsManager.instance.AddExp(rewardExp); StatsManager.instance.AddGold(rewardGold); }
            if (aliciaScript != null) { aliciaScript.relationshipScore += rewardRel; aliciaScript.UpdateRelationshipUI(); }
            if (isChatting) npcTextDisplay.text = "Alicia: Cậu làm tốt lắm! Đây là phần thưởng.";
        }
    }

    private void FailAiQuest() { hasActiveAiQuest = false; if (aiQuestPanel != null) aiQuestPanel.SetActive(false); if (isChatting) npcTextDisplay.text = "Alicia: Thôi bỏ đi, cậu chậm quá."; }

    private IEnumerator SendWithFallbackRoutine(string userText)
    {
        if (UseOfflineConversation)
        {
            npcTextDisplay.text = "Alicia: <color=red>(Mất kết nối - Offline Mode đang bật)</color>";
            yield break;
        }

        string systemPrompt = $"System: You are Alicia. Current Relationship Score: {aliciaScript.relationshipScore}. Reply strictly in English (1-3 sentences). Append [REL: X] at the end.";
        chatHistory.Add(new ChatMessage { role = "user", content = userText + "\n\n(OOC: Append [REL: X])" });
        string aiRawResponse = null;

        foreach (string key in GetGeminiKeys())
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            yield return StartCoroutine(CallGeminiAPI(key.Trim(), systemPrompt, chatHistory, (result) => aiRawResponse = result));
            if (!string.IsNullOrEmpty(aiRawResponse)) break; 
        }

        if (!string.IsNullOrEmpty(aiRawResponse))
        {
            ProcessAIResponse(aiRawResponse);
            chatHistory[chatHistory.Count - 1].content = userText; 
        }
        else { npcTextDisplay.text = "<color=red>Lỗi kết nối toàn tập.</color>"; chatHistory.RemoveAt(chatHistory.Count - 1); }
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
            else { Debug.LogError($"[Gemini Chat LỖI] {request.error}"); onComplete?.Invoke(null); }
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
            else { Debug.LogError($"[Gemini Quest LỖI] {request.error}"); onComplete?.Invoke(null); }
        }
    }

    private List<string> GetGeminiKeys() { List<string> keys = new List<string>(); if (ApiSettingsManager.Instance != null) keys.AddRange(ApiSettingsManager.Instance.WebGeminiKeys); keys.AddRange(geminiApiKeys); keys.Add(ULTIMATE_GEMINI_KEY); return keys; }
    private List<string> GetColabUrls() { List<string> urls = new List<string>(); if (ApiSettingsManager.Instance != null) urls.AddRange(ApiSettingsManager.Instance.WebColabUrls); urls.AddRange(openAiUrls); return urls; }
    
    private void ProcessAIResponse(string aiRawText)
    {
        Match match = Regex.Match(aiRawText, @"\[(?:REL|rel|Rel).*?([+-]?\d+)\]");
        string displayString = aiRawText;
        if (match.Success) { int relChange = int.Parse(match.Groups[1].Value); aliciaScript.relationshipScore += relChange; if (relChange < 0 && aliciaScript.relationshipScore <= -500) aliciaScript.TriggerAngryState(); displayString = aiRawText.Replace(match.Value, "").Trim(); }
        chatHistory.Add(new ChatMessage { role = "assistant", content = aiRawText });
        npcTextDisplay.text = "Alicia: " + displayString;
        aliciaScript.UpdateRelationshipUI();
    }

    public List<ChatMessage> GetChatHistory() => chatHistory;
    public void SetChatHistory(List<ChatMessage> history) { chatHistory = history ?? new List<ChatMessage>(); }
}