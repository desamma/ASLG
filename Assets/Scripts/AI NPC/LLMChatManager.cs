using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.IO;

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
        if (aliciaScript == null)
        {
            aliciaScript = FindObjectOfType<NPCCompanion>();
        }

        if (aliciaScript != null && (playerMovement == null || playerAttack == null))
        {
            if (aliciaScript.playerTransform != null)
            {
                playerMovement = aliciaScript.playerTransform.GetComponent<PlayerMovement>();
                playerAttack = aliciaScript.playerTransform.GetComponent<PlayerAttack>();
            }
            else 
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerMovement = playerObj.GetComponent<PlayerMovement>();
                    playerAttack = playerObj.GetComponent<PlayerAttack>();
                }
            }
        }

        if (isChatting && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
            return;
        }

        if (playerInputField != null && playerInputField.isFocused) return;

        if (!isChatting && Input.GetKeyDown(KeyCode.E) && aliciaScript != null && playerMovement != null)
        {
            float dist = Vector2.Distance(playerMovement.transform.position, aliciaScript.transform.position);
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

        // ĐÃ FIX: Dùng SetMovementLock thay vì enabled = false để tránh đơ Animator Physics
        if (playerMovement != null) playerMovement.SetMovementLock(true);
        if (playerAttack != null) playerAttack.enabled = false;

        if (chatHistory.Count == 0) npcTextDisplay.text = "Alicia: Cậu cần gì sao?";
        
        // ĐÃ FIX: Gọi InputField qua Coroutine để tránh bị lỗi Unity chặn gõ chữ
        StartCoroutine(FocusInputDelay());
    }

    private IEnumerator FocusInputDelay()
    {
        yield return new WaitForEndOfFrame();
        playerInputField.ActivateInputField();
        playerInputField.Select();
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
        
        StartCoroutine(FocusInputDelay()); // Tự focus lại sau khi gửi
        
        npcTextDisplay.text = "<i>Alicia đang suy nghĩ...</i>";

        StartCoroutine(SendToLLM(userText));
    }

    IEnumerator SendToLLM(string userText)
    {
        chatHistory.Add(new ChatMessage { role = "user", content = userText });
        OpenAiRequest requestData = new OpenAiRequest();

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