using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

// --- CẤU TRÚC JSON CHO OPENAI API ---
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
// ------------------------------------

public class LLMChatManager : MonoBehaviour
{
    [Header("API Connection")]
    public string apiUrl = "https://your-cloudflare-link.trycloudflare.com/v1/chat/completions";

    [Header("UI References")]
    public GameObject chatCanvas;
    public TMP_Text npcTextDisplay;
    public TMP_InputField playerInputField;
    public Button sendButton;

    [Header("Game References")]
    public NPCCompanion aliciaScript;
    public PlayerMovement playerMovement;
    public PlayerAttack playerAttack;

    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isChatting = false;

    void Start()
    {
        if (chatCanvas != null) chatCanvas.SetActive(false);
        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
        if (playerInputField != null) playerInputField.onSubmit.AddListener(delegate { OnSendClicked(); });
    }

    void Update()
    {
        // Khóa input nếu đang gõ
        if (playerInputField != null && playerInputField.isFocused) return;

        if (Input.GetKeyDown(KeyCode.E) && aliciaScript != null)
        {
            float dist = Vector2.Distance(playerMovement.transform.position, aliciaScript.transform.position);
            if (dist <= 2.5f)
            {
                ToggleChat();
            }
        }
    }

    void ToggleChat()
    {
        isChatting = !isChatting;
        chatCanvas.SetActive(isChatting);

        if (isChatting)
        {
            if (playerMovement != null) playerMovement.enabled = false;
            if (playerAttack != null) playerAttack.enabled = false;
            playerInputField.ActivateInputField();
            if (chatHistory.Count == 0) npcTextDisplay.text = "Alicia: Hello my Adventurer, it's a good time to see you around!";
        }
        else
        {
            if (playerMovement != null) playerMovement.enabled = true;
            if (playerAttack != null) playerAttack.enabled = true;
        }
    }

    public void OnSendClicked()
    {
        string userText = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        playerInputField.text = "";
        playerInputField.ActivateInputField();
        npcTextDisplay.text = "<i>Alicia is thinking...</i>";

        StartCoroutine(SendToLLM(userText));
    }

    IEnumerator SendToLLM(string userText)
    {
        // 1. Lưu câu của người chơi vào lịch sử nội bộ
        chatHistory.Add(new ChatMessage { role = "user", content = userText });

        OpenAiRequest requestData = new OpenAiRequest();

        // 2. SYSTEM PROMPT (Ngắn gọn, vì ta đã dùng thủ thuật ép khuôn ở dưới)
        string systemPrompt =
            $"You are Alicia, a female adventurer. You are very friendly and easily raise relationship with all the friends. You are cheerful, cute, brave and helpful. Current Relationship Score: {aliciaScript.relationshipScore} (-1000 to +1000). " +
            "Reply in 1-3 short sentences. You MUST include a tag [REL: X] at the exact end of your message.";

        requestData.messages.Add(new ChatMessage { role = "system", content = systemPrompt });
        requestData.messages.AddRange(chatHistory);

        // 3. TUYỆT CHIÊU ÉP KHUÔN (User-Prompt Injection)
        string oocCommand = "\n\n(OOC: Respond in character. You MUST end your message with the exact tag [REL: X]. X is the relationship point change from -50 to 50. Even if nothing changes, write [REL: 0].)";
        requestData.messages[requestData.messages.Count - 1].content = userText + oocCommand;

        // 4. Gói và Gửi JSON
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
                string historyString = aiRawText; // Chuỗi dùng để lưu vào não AI

                // 5. MỔ XẺ VÀ THUẬT TOÁN "CHỈNH SỬA KÝ ỨC"
                Match match = Regex.Match(aiRawText, @"\[(?:REL|rel|Rel).*?([+-]?\d+)\]");

                if (match.Success)
                {
                    // Trường hợp 1: AI ngoan ngoãn ghi Tag
                    int relChange = int.Parse(match.Groups[1].Value);
                    aliciaScript.relationshipScore += relChange;
                    Debug.Log($"<color=green>[Hệ thống]</color> Đã cập nhật: {relChange}. Điểm hiện tại: {aliciaScript.relationshipScore}");

                    displayString = aiRawText.Replace(match.Value, "").Trim(); // UI: Cắt bỏ tag
                    // historyString giữ nguyên bản có chứa tag để AI học hỏi
                }
                else
                {
                    // Trường hợp 2: AI quên Tag -> Tự động nhét [REL: 0] vào não nó!
                    Debug.LogWarning("<color=orange>[Hệ thống]</color> AI quên Tag! Đang tự động tiêm [REL: 0] vào bộ nhớ lịch sử.");
                    displayString = aiRawText; // UI: In nguyên câu của AI ra
                    historyString = aiRawText + " [REL: 0]"; // Bộ nhớ: Nhét thêm thẻ vào đuôi để lần sau nó nhớ
                }

                // 6. Lưu vào Ký Ức (ĐÃ CÓ THẺ TAG) và Hiển thị lên UI (KHÔNG CÓ TAG)
                chatHistory.Add(new ChatMessage { role = "assistant", content = historyString });
                npcTextDisplay.text = "Alicia: " + displayString;
            }
            else
            {
                npcTextDisplay.text = "<color=red>Lỗi kết nối API.</color>";
                Debug.LogError(request.error);
            }
        }
    }
}