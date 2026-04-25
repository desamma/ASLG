using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

// ==========================================
// CẤU TRÚC JSON CHO OPENAI (CLOUDFLARE)
// ==========================================
[System.Serializable]
public class OpenAiRequest { public List<ChatMessage> messages = new List<ChatMessage>(); public int max_tokens = 150; public float temperature = 0.7f; }
[System.Serializable]
public class ChatMessage { public string role; public string content; }
[System.Serializable]
public class OpenAiResponse { public List<OpenAiChoice> choices; }
[System.Serializable]
public class OpenAiChoice { public ChatMessage message; }

// ==========================================
// CẤU TRÚC JSON CHO GOOGLE GEMINI 1.5
// ==========================================
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

    [Header("Network Fallback System")]
    public List<string> geminiApiKeys = new List<string>();
    public List<string> openAiUrls = new List<string>();

    [Header("UI References")]
    public GameObject chatCanvas;
    public TMP_Text npcTextDisplay;
    public TMP_InputField playerInputField;
    public Button sendButton;
    public Button exitButton;

    [Header("Game References")]
    public NPCCompanion aliciaScript;
    public PlayerMovement playerMovement;
    public PlayerAttack playerAttack;

    [SerializeField] private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isChatting = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (chatCanvas != null) chatCanvas.SetActive(false);
        if (sendButton != null) { Navigation n = sendButton.navigation; n.mode = Navigation.Mode.None; sendButton.navigation = n; sendButton.onClick.AddListener(OnSendClicked); }
        if (exitButton != null) { Navigation n = exitButton.navigation; n.mode = Navigation.Mode.None; exitButton.navigation = n; exitButton.onClick.AddListener(CloseChat); }
        if (playerInputField != null) { Navigation n = playerInputField.navigation; n.mode = Navigation.Mode.None; playerInputField.navigation = n; playerInputField.onSubmit.AddListener(delegate { OnSendClicked(); }); }
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
    }

    public void OpenChat()
    {
        if (chatCanvas == null) return;
        isChatting = true;
        chatCanvas.SetActive(true);

        if (playerMovement != null) playerMovement.SetMovementLock(true);
        if (playerAttack != null) playerAttack.enabled = false;

        if (chatHistory.Count == 0) npcTextDisplay.text = "Alicia: Cậu cần gì sao?";
        StartCoroutine(FocusInputDelay());
    }

    private IEnumerator FocusInputDelay()
    {
        yield return new WaitForEndOfFrame();
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(playerInputField.gameObject);
        if (playerInputField != null) playerInputField.ActivateInputField();
    }

    public void CloseChat()
    {
        if (chatCanvas == null) return;
        isChatting = false;
        chatCanvas.SetActive(false);

        if (playerMovement != null) playerMovement.SetMovementLock(false);
        if (playerAttack != null) playerAttack.enabled = true;
    }

    public void OnSendClicked()
    {
        string userText = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        playerInputField.text = "";
        StartCoroutine(FocusInputDelay());
        npcTextDisplay.text = "<i>Alicia is thinking...</i>";

        StartCoroutine(SendWithFallbackRoutine(userText));
    }

    private IEnumerator SendWithFallbackRoutine(string userText)
    {
        string systemPrompt = $"System: You are Alicia, a female adventurer traveling with {GameSession.PlayerName}. Current Relationship Score: {aliciaScript.relationshipScore}. You MUST reply strictly in English (1-3 short sentences). You MUST evaluate the player's attitude to change the relationship score.";
    
    string oocCommand = "\n\n(OOC: Based on the player's message, you MUST append [REL: X] at the exact end. X is the score INCREASE (e.g., +2, +5) or DECREASE (e.g., -2, -5). You MUST NOT return [REL: 0]. Force a positive or negative reaction based on their tone.)";
    
    string finalUserText = userText + oocCommand;

        chatHistory.Add(new ChatMessage { role = "user", content = finalUserText });
        string aiRawResponse = null;

        // 1. Quét Gemini
        foreach (string key in geminiApiKeys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            Debug.Log($"<color=yellow>[Network]</color> Đang thử Gemini API...");
            yield return StartCoroutine(CallGeminiAPI(key.Trim(), systemPrompt, (result) => aiRawResponse = result));
            if (!string.IsNullOrEmpty(aiRawResponse)) break; 
        }

        // 2. Quét Cloudflare
        if (string.IsNullOrEmpty(aiRawResponse))
        {
            foreach (string url in openAiUrls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                Debug.Log($"<color=yellow>[Network]</color> Đang thử Local Cloudflare...");
                yield return StartCoroutine(CallOpenAiAPI(url.Trim(), systemPrompt, (result) => aiRawResponse = result));
                if (!string.IsNullOrEmpty(aiRawResponse)) break; 
            }
        }

        // 3. Xử lý kết quả
        if (!string.IsNullOrEmpty(aiRawResponse))
        {
            ProcessAIResponse(aiRawResponse);
            chatHistory[chatHistory.Count - 1].content = userText; 
        }
        else
        {
            npcTextDisplay.text = "<color=red>Lỗi kết nối toàn tập. Không có API nào hoạt động!</color>";
            chatHistory.RemoveAt(chatHistory.Count - 1); 
        }
    }

    private IEnumerator CallGeminiAPI(string apiKey, string sysPrompt, System.Action<string> onComplete)
    {
        // DÒNG MỚI:
string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        
        GeminiRequest requestData = new GeminiRequest();
        requestData.systemInstruction = new GeminiSystemInstruction { parts = new List<GeminiPart> { new GeminiPart { text = sysPrompt } } };
        
        foreach (var msg in chatHistory)
        {
            requestData.contents.Add(new GeminiContent { 
                role = msg.role == "assistant" ? "model" : "user", 
                parts = new List<GeminiPart> { new GeminiPart { text = msg.content } } 
            });
        }

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            string rawResponse = request.downloadHandler.text;

            Debug.Log($"<color=cyan>[Gemini Response RAW]</color> {rawResponse}"); // SOi RỌI NỘI DUNG TỪ GOOGLE

            if (request.result == UnityWebRequest.Result.Success)
            {
                GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(rawResponse);
                if (res != null && res.candidates != null && res.candidates.Count > 0)
                {
                    onComplete?.Invoke(res.candidates[0].content.parts[0].text.Trim());
                }
                else
                {
                    Debug.LogWarning("<color=orange>[Gemini Warning]</color> API OK nhưng bị Safety chặt hoặc JSON lỗi. Xem [Gemini Response RAW] bên trên.");
                    onComplete?.Invoke(null);
                }
            }
            else 
            {
                Debug.LogError($"<color=red>[Gemini LỖI]</color> {request.error}");
                onComplete?.Invoke(null);
            }
        }
    }

    private IEnumerator CallOpenAiAPI(string url, string sysPrompt, System.Action<string> onComplete)
    {
        OpenAiRequest requestData = new OpenAiRequest();
        requestData.messages.Add(new ChatMessage { role = "system", content = sysPrompt });
        requestData.messages.AddRange(chatHistory);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            string rawResponse = request.downloadHandler.text;
            
            Debug.Log($"<color=cyan>[OpenAI Response RAW]</color> {rawResponse}"); // SOi RỌI NỘI DUNG TỪ CLOUDFLARE

            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAiResponse res = JsonUtility.FromJson<OpenAiResponse>(rawResponse);
                if (res != null && res.choices != null && res.choices.Count > 0)
                    onComplete?.Invoke(res.choices[0].message.content.Trim());
                else
                    onComplete?.Invoke(null);
            }
            else 
            {
                Debug.LogError($"<color=red>[OpenAI LỖI]</color> {request.error}");
                onComplete?.Invoke(null);
            }
        }
    }

    private void ProcessAIResponse(string aiRawText)
    {
        Match match = Regex.Match(aiRawText, @"\[(?:REL|rel|Rel).*?([+-]?\d+)\]");
        string displayString = aiRawText;
        string historyString = aiRawText;

        if (match.Success)
        {
            int relChange = int.Parse(match.Groups[1].Value);
            aliciaScript.relationshipScore += relChange;
            if (relChange < 0 && aliciaScript.relationshipScore <= -500)
            {
                aliciaScript.TriggerAngryState(); 
                Debug.LogWarning("<color=red>[System]</color> Alicia nổi giận vì lời nói của bạn và đang tấn công!");
            }
            displayString = aiRawText.Replace(match.Value, "").Trim();
        }
        else
        {
            historyString = aiRawText + " [REL: 0]";
        }

        chatHistory.Add(new ChatMessage { role = "assistant", content = historyString });
        npcTextDisplay.text = "Alicia: " + displayString;
        aliciaScript.UpdateRelationshipUI();
    }

    public List<ChatMessage> GetChatHistory() => chatHistory;
    public void SetChatHistory(List<ChatMessage> history) { chatHistory = history ?? new List<ChatMessage>(); }
}