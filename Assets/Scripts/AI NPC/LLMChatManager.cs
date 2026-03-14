using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.IO;

// JSON Structure for OpenAI API
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
    [Header("API Connection")]
    public string apiUrl = "https://your-cloudflare-link.trycloudflare.com/v1/chat/completions";

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

    void Start()
    {
        if (chatCanvas != null) chatCanvas.SetActive(false);

        // Bind button events
        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
        if (exitButton != null) exitButton.onClick.AddListener(CloseChat);

        // Bind Enter key event
        if (playerInputField != null) playerInputField.onSubmit.AddListener(delegate { OnSendClicked(); });
    }

    void Update()
    {
        // Check for Escape key to close chat
        if (isChatting && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
            return;
        }

        // Ignore inputs if input field is focused
        if (playerInputField != null && playerInputField.isFocused)
        {
            return;
        }

        // Check for interaction key to open chat
        if (!isChatting && Input.GetKeyDown(KeyCode.E) && aliciaScript != null)
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
        isChatting = true;
        chatCanvas.SetActive(true);

        // Disable player movement
        if (playerMovement != null) playerMovement.enabled = false;

        // Disable player attack to prevent accidental clicks
        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }
        else
        {
            Debug.LogError("<color=red>Missing PlayerAttack reference in the Inspector.</color>");
        }

        playerInputField.ActivateInputField();
        if (chatHistory.Count == 0) npcTextDisplay.text = "Alicia: Cậu cần gì sao?";
    }

    public void CloseChat()
    {
        isChatting = false;
        chatCanvas.SetActive(false);

        // Re-enable player controls
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
        // Add user message to history
        chatHistory.Add(new ChatMessage { role = "user", content = userText });
        OpenAiRequest requestData = new OpenAiRequest();

        // System prompt configuration
        string systemPrompt =
            $"You are Alicia, a female adventurer. Current Relationship Score: {aliciaScript.relationshipScore} (-1000 to +1000). " +
            "Reply in 1-3 short sentences. You MUST include a tag [REL: X] at the exact end of your message.";

        requestData.messages.Add(new ChatMessage { role = "system", content = systemPrompt });
        requestData.messages.AddRange(chatHistory);

        // Inject OOC command to enforce formatting
        string oocCommand = "\n\n(OOC: Respond in character. You MUST end your message with the exact tag [REL: X]. X is the relationship point change from -50 to 50. Even if nothing changes, write [REL: 0].)";
        requestData.messages[requestData.messages.Count - 1].content = userText + oocCommand;

        // Serialize request to JSON
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

                Debug.Log($"<color=cyan>[RAW AI RESPONSE]</color> {aiRawText}");

                string displayString = aiRawText;
                string historyString = aiRawText;

                // Parse relationship tag
                Match match = Regex.Match(aiRawText, @"\[(?:REL|rel|Rel).*?([+-]?\d+)\]");

                if (match.Success)
                {
                    int relChange = int.Parse(match.Groups[1].Value);
                    aliciaScript.relationshipScore += relChange;
                    Debug.Log($"<color=green>[System]</color> Relationship updated: {relChange}. Current score: {aliciaScript.relationshipScore}");

                    displayString = aiRawText.Replace(match.Value, "").Trim();
                }
                else
                {
                    // Memory injection for missing tag
                    Debug.LogWarning("<color=orange>[System]</color> Missing tag detected. Injecting [REL: 0] into history.");
                    displayString = aiRawText;
                    historyString = aiRawText + " [REL: 0]";
                }

                // Save to history and update UI
                chatHistory.Add(new ChatMessage { role = "assistant", content = historyString });
                npcTextDisplay.text = "Alicia: " + displayString;
            }
            else
            {
                npcTextDisplay.text = "<color=red>API Connection Error.</color>";
                Debug.LogError(request.error);
            }
        }
    }
}