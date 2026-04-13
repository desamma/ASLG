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

    [Header("AI Special Features")]
    public Button aiQuestButton;
    public Button giftButton;
    public ItemData healthPotionItem;
    
    public Color defaultBtnColor = Color.white;
    public Color inProgressBtnColor = Color.red;
    public Color completedBtnColor = Color.green;

    private enum AIQuestState { Idle, Available, InProgress, Completed }
    [SerializeField] private AIQuestState currentQuestState = AIQuestState.Available;
    private float actionCooldownTimer = 0f;
    private int questMessagesSent = 0;
    private float questWalkTimer = 0f;
    private int questAttackCount = 0;

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

        if (aiQuestButton != null) aiQuestButton.onClick.AddListener(OnAIQuestButtonClicked);
        if (giftButton != null) giftButton.onClick.AddListener(OnGiftButtonClicked);
        UpdateButtonVisuals();
    }

    void Update()
    {
        // Check for Escape key to close chat
        if (isChatting && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
            return;
        }

        // Update Cooldowns and Quest Trackers
        if (actionCooldownTimer > 0)
        {
            actionCooldownTimer -= Time.deltaTime;
            if (actionCooldownTimer <= 0)
            {
                actionCooldownTimer = 0;
                // Hồi xong, nếu nhiệm vụ đang nằm chờ thì mở lại
                if (currentQuestState == AIQuestState.Idle)
                {
                    currentQuestState = AIQuestState.Available;
                }
                UpdateButtonVisuals();
            }
        }

        // Tracking tiến độ AI Quest ngầm (kể cả khi đã đóng UI Chat)
        if (currentQuestState == AIQuestState.InProgress)
        {
            if (playerMovement != null && playerMovement.GetStateManager() != null && playerMovement.GetStateManager().IsInState(PlayerState.Move))
            {
                questWalkTimer += Time.deltaTime;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetButtonDown("Slash"))
            {
                questAttackCount++;
            }

            if (questMessagesSent >= 3 && questWalkTimer >= 3f && questAttackCount >= 1)
            {
                currentQuestState = AIQuestState.Completed;
                UpdateButtonVisuals();
            }
        }

        // Ignore inputs if input field is focused
        if (playerInputField != null && playerInputField.isFocused)
        {
            return;
        }

        // Tự động tìm Player nếu được spawn động (chưa được gán vào Inspector)
        if (playerMovement == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerMovement = playerObj.GetComponent<PlayerMovement>();
                playerAttack = playerObj.GetComponent<PlayerAttack>();
            }
        }

        // Check for interaction key to open chat
        if (!isChatting && Input.GetKeyDown(KeyCode.E) && aliciaScript != null)
        {
            if (playerMovement != null)
            {
                float dist = Vector2.Distance(playerMovement.transform.position, aliciaScript.transform.position);
                if (dist <= 2.5f)
                {
                    OpenChat();
                }
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

        // Track số tin nhắn cho AI Quest
        if (currentQuestState == AIQuestState.InProgress)
        {
            questMessagesSent++;
        }

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
        // Chèn tên Player từ GameSession vào Prompt để AI nhận diện
        string systemPrompt =
            $"You are Alicia, a female adventurer traveling with a player named {GameSession.PlayerName}. " +
            $"Current Relationship Score: {aliciaScript.relationshipScore} (-1000 to +1000). " +
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

                    // Nếu bị trừ hảo cảm VÀ tổng điểm rớt xuống <= -500 (Hoặc vốn dĩ đã dỗi)
                    if (relChange < 0 && aliciaScript.relationshipScore <= -500)
                    {
                        aliciaScript.TriggerAngryState(); // Gọi AI phản công Player 5 giây
                        Debug.LogWarning("<color=red>[System]</color> Alicia nổi giận vì lời nói của bạn và đang tấn công!");
                    }

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

    // ==========================================
    // AI QUEST & GIFT LOGIC
    // ==========================================
    public void OnAIQuestButtonClicked()
    {
        if (actionCooldownTimer > 0) return;

        if (currentQuestState == AIQuestState.Available)
        {
            currentQuestState = AIQuestState.InProgress;
            questMessagesSent = 0;
            questWalkTimer = 0f;
            questAttackCount = 0;
            UpdateButtonVisuals();
            Debug.Log("<color=yellow>[AI Quest] Đã nhận nhiệm vụ AI: Nhắn 3 tin, Đi bộ 3s, Tấn công 1 lần.</color>");
        }
        else if (currentQuestState == AIQuestState.Completed)
        {
            int potions = Random.Range(1, 4);
            int relReward = Random.Range(100, 301);

            if (InventoryUI.instance != null && healthPotionItem != null)
            {
                InventoryUI.instance.AddItems(healthPotionItem, potions);
            }
            if (aliciaScript != null)
            {
                aliciaScript.relationshipScore += relReward;
            }

            Debug.Log($"<color=green>[AI Quest] Hoàn thành! Nhận {potions} bình máu và {relReward} hảo cảm.</color>");
            
            currentQuestState = AIQuestState.Idle;
            StartSharedCooldown(15f);
        }
    }

    public void OnGiftButtonClicked()
    {
        if (actionCooldownTimer > 0) return;

        int potions = Random.Range(1, 4);
        if (InventoryUI.instance != null && healthPotionItem != null)
        {
            InventoryUI.instance.AddItems(healthPotionItem, potions);
        }

        Debug.Log($"<color=green>[Gift] Đã nhận {potions} bình máu.</color>");
        StartSharedCooldown(15f);
    }

    private void StartSharedCooldown(float time)
    {
        actionCooldownTimer = time;
        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        bool isReady = (actionCooldownTimer <= 0);

        if (aiQuestButton != null)
        {
            aiQuestButton.interactable = isReady;
            Image img = aiQuestButton.GetComponent<Image>();
            if (img != null)
            {
                if (currentQuestState == AIQuestState.InProgress) img.color = inProgressBtnColor;
                else if (currentQuestState == AIQuestState.Completed) img.color = completedBtnColor;
                else img.color = defaultBtnColor;
            }
        }

        if (giftButton != null)
        {
            giftButton.interactable = isReady;
        }
    }
}