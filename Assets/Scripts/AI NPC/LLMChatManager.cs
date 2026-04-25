using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.IO;

// Cấu trúc dữ liệu cho API
[System.Serializable]
public class OpenAiRequest
{
    public List<ChatMessage> messages = new List<ChatMessage>();
    public int max_tokens = 150;
    public float temperature = 0.7f;
}

[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class OpenAiResponse
{
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public ChatMessage message;
}

public class LLMChatManager : MonoBehaviour
{
    public static LLMChatManager Instance { get; private set; }

    [Header("API Connection")]
    public string apiUrl = "https://your-cloudflare-link.trycloudflare.com/v1/chat/completions";

    [Header("UI References (Must be child of this object)")]
    public GameObject chatCanvas;
    public TMP_Text npcTextDisplay;
    public TMP_InputField playerInputField;
    public Button sendButton;
    public Button exitButton;

    [Header("Game References (Auto-Assigned)")]
    public NPCCompanion aliciaScript;
    public PlayerMovement playerMovement;
    public PlayerAttack playerAttack;

    [SerializeField] private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isChatting = false;

    private void Awake()
    {
        // Cơ chế Singleton đảm bảo chỉ có 1 Manager tồn tại xuyên suốt các Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (chatCanvas != null) chatCanvas.SetActive(false);

        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
        if (exitButton != null) exitButton.onClick.AddListener(CloseChat);
        if (playerInputField != null) playerInputField.onSubmit.AddListener(delegate { OnSendClicked(); });
    }

    void Update()
    {
        // --- CƠ CHẾ AUTO-ASSIGN THÔNG MINH ---
        // 1. Tự động tìm lại Alicia nếu bị mất liên kết (ví dụ khi chuyển Scene)
        if (aliciaScript == null)
        {
            aliciaScript = FindObjectOfType<NPCCompanion>();
        }

        // 2. Tự động "bắt" Player từ Alicia (Cực kỳ chính xác vì Spawner đã nối dây sẵn cho Alicia)
        if (aliciaScript != null && (playerMovement == null || playerAttack == null))
        {
            if (aliciaScript.playerTransform != null)
            {
                playerMovement = aliciaScript.playerTransform.GetComponent<PlayerMovement>();
                playerAttack = aliciaScript.playerTransform.GetComponent<PlayerAttack>();
                
                if (playerMovement != null) 
                {
                    Debug.Log("<color=green>[LLM Manager]</color> Đã tự động nhận diện Player từ Alicia thành công!");
                }
            }
            else 
            {
                // Fallback: Nếu Alicia chưa có Transform của Player, thử tìm theo Tag "Player"
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerMovement = playerObj.GetComponent<PlayerMovement>();
                    playerAttack = playerObj.GetComponent<PlayerAttack>();
                }
            }
        }

        // --- LOGIC XỬ LÝ PHÍM BẤM ---
        if (isChatting && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
            return;
        }

        if (playerInputField != null && playerInputField.isFocused) return;

        // Chỉ kiểm tra nhấn E khi đã có đủ thông tin Alicia và Player để đo khoảng cách
        if (!isChatting && Input.GetKeyDown(KeyCode.E) && aliciaScript != null && playerMovement != null)
        {
            float dist = Vector2.Distance(playerMovement.transform.position, aliciaScript.transform.position);
            // Khoảng cách 2.5 khớp với thời điểm hiện Talk Icon trên đầu Alicia
            if (dist <= 2.5f)
            {
                OpenChat();
            }
        }
    }

    public void OpenChat()
    {
        if (chatCanvas == null) return;

        isChatting = true;
        chatCanvas.SetActive(true);

        // Khóa điều khiển nhân vật khi đang chat
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerAttack != null) playerAttack.enabled = false;

        playerInputField.ActivateInputField();
        if (chatHistory.Count == 0) npcTextDisplay.text = "Alicia: Cậu cần gì sao?";
    }

    public void CloseChat()
    {
        if (chatCanvas == null) return;

        isChatting = false;
        chatCanvas.SetActive(false);

        // Mở lại điều khiển nhân vật
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerAttack != null) playerAttack.enabled = true;
    }

    public void OnSendClicked()
    {
        string userText = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        playerInputField.text = "";
        playerInputField.ActivateInputField();
        npcTextDisplay.text = "<i>Alicia đang suy nghĩ...</i>";

        StartCoroutine(SendToLLM(userText));
    }

    IEnumerator SendToLLM(string userText)
    {
        chatHistory.Add(new ChatMessage { role = "user", content = userText });
        OpenAiRequest requestData = new OpenAiRequest();

        // Cấu hình tính cách cho AI
        string systemPrompt =
            $"You are Alicia, a female adventurer traveling with a player named {GameSession.PlayerName}. " +
            $"Current Relationship Score: {aliciaScript.relationshipScore} (-1000 to +1000). " +
            "Reply in 1-3 short sentences. You MUST include a tag [REL: X] at the exact end of your message.";

        requestData.messages.Add(new ChatMessage { role = "system", content = systemPrompt });
        requestData.messages.AddRange(chatHistory);

        string oocCommand = "\n\n(OOC: Respond in character. You MUST end your message with the exact tag [REL: X]. X is the relationship point change from -50 to 50. Even if nothing changes, write [REL: 0].)";
        requestData.messages[requestData.messages.Count - 1].content = userText + oocCommand;

        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAiResponse response = JsonUtility.FromJson<OpenAiResponse>(request.downloadHandler.text);
                string aiRawText = response.choices[0].message.content.Trim();

                string displayString = aiRawText;
                string historyString = aiRawText;

                // Xử lý điểm hảo cảm [REL: X]
                Match match = Regex.Match(aiRawText, @"\[(?:REL|rel|Rel).*?([+-]?\d+)\]");

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
                    displayString = aiRawText;
                    historyString = aiRawText + " [REL: 0]";
                }

                chatHistory.Add(new ChatMessage { role = "assistant", content = historyString });
                npcTextDisplay.text = "Alicia: " + displayString;
                
                // Cập nhật lại UI sau khi điểm hảo cảm thay đổi
                aliciaScript.UpdateRelationshipUI();
            }
            else
            {
                npcTextDisplay.text = "<color=red>API Connection Error.</color>";
                Debug.LogError(request.error);
            }
        }
    }

    public List<ChatMessage> GetChatHistory() => chatHistory;
    
    public void SetChatHistory(List<ChatMessage> history) 
    {
        chatHistory = history ?? new List<ChatMessage>();
    }
}