using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

[System.Serializable]
public class OpenAiRequest
{
    public List<ChatMessage> messages = new List<ChatMessage>();
    public int max_tokens = 150;
    public float temperature = 0.7f;
}

[System.Serializable]
public class ChatMessage { public string role; public string content; }

[System.Serializable]
public class OpenAiResponse { public List<Choice> choices; }

[System.Serializable]
public class Choice { public ChatMessage message; }

public class LLMChatManager : MonoBehaviour
{
    public static LLMChatManager Instance { get; private set; }

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

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        Debug.Log("<color=cyan>[SUPER DEBUG]</color> Script LLMChatManager ĐÃ ĐƯỢC LOAD THÀNH CÔNG!");
        
        if (chatCanvas != null) chatCanvas.SetActive(false);

        if (sendButton != null) 
        { 
            Navigation n = sendButton.navigation; n.mode = Navigation.Mode.None; sendButton.navigation = n;
            sendButton.onClick.AddListener(OnSendClicked); 
            Debug.Log("<color=cyan>[SUPER DEBUG]</color> Đã nối dây nút SEND.");
        }
        if (exitButton != null) 
        { 
            Navigation n = exitButton.navigation; n.mode = Navigation.Mode.None; exitButton.navigation = n;
            exitButton.onClick.AddListener(CloseChat); 
            Debug.Log("<color=cyan>[SUPER DEBUG]</color> Đã nối dây nút EXIT (X).");
        }
        if (playerInputField != null) 
        { 
            Navigation n = playerInputField.navigation; n.mode = Navigation.Mode.None; playerInputField.navigation = n;
            playerInputField.onSubmit.AddListener(delegate { OnSendClicked(); }); 
            Debug.Log("<color=cyan>[SUPER DEBUG]</color> Đã nối dây Input Field.");
        }
    }

    void Update()
    {
        // =========================================================================
        // MÁY QUÉT RAYCAST: KIỂM TRA XEM THẰNG NÀO ĐANG CHẶN CLICK CHUỘT CỦA BẠN
        // =========================================================================
        if (Input.GetMouseButtonDown(0)) 
        {
            Debug.Log("<color=yellow>[RAYCAST SCAN]</color> BẠN VỪA CLICK CHUỘT TRÁI!");
            
            if (EventSystem.current == null)
            {
                Debug.LogError("<color=red>🚨 LỖI CHÍNH MẠNG:</color> KHÔNG CÓ EVENT SYSTEM TRONG SCENE! Mọi UI sẽ bị liệt!");
                return;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                Debug.Log($"🎯 CLICK TRÚNG: <b><color=green>{results[0].gameObject.name}</color></b> (Thuộc: {results[0].gameObject.transform.root.name})");
                foreach(var res in results)
                {
                    Debug.Log($"   🔍 Chi tiết các lớp đè lên nhau (Từ trên xuống): {res.gameObject.name} | Sort Order: {res.sortingOrder}");
                }
            }
            else
            {
                Debug.LogWarning("👻 CLICK VÀO KHOẢNG KHÔNG! (Không trúng bất kỳ ô UI nào, hãy kiểm tra Graphic Raycaster của ChatCanvas).");
            }
        }

        // Tự động gán biến
        if (aliciaScript == null) aliciaScript = FindObjectOfType<NPCCompanion>();
        if (aliciaScript != null && (playerMovement == null || playerAttack == null))
        {
            if (aliciaScript.playerTransform != null)
            {
                playerMovement = aliciaScript.playerTransform.GetComponent<PlayerMovement>();
                playerAttack = aliciaScript.playerTransform.GetComponent<PlayerAttack>();
            }
        }

        if (isChatting && Input.GetKeyDown(KeyCode.Escape)) 
        { 
            Debug.Log("<color=cyan>[SUPER DEBUG]</color> Đã bấm phím ESC để thoát Chat.");
            CloseChat(); 
            return; 
        }

        if (playerInputField != null && playerInputField.isFocused) return;

        // Bấm E để mở Chat
        if (!isChatting && Input.GetKeyDown(KeyCode.E) && aliciaScript != null && playerMovement != null)
        {
            float dist = Vector2.Distance(playerMovement.transform.position, aliciaScript.transform.position);
            Debug.Log($"<color=cyan>[SUPER DEBUG]</color> Bấm E. Khoảng cách: {dist} (Yêu cầu <= 2.5)");
            if (dist <= 2.5f) OpenChat();
        }
    }

    public void OpenChat()
    {
        Debug.Log("<color=magenta>[ACTION]</color> Đang mở hộp thoại Chat...");

        // =====================================================================
        // AUTO-DOCTOR: TỰ ĐỘNG CHỮA BỆNH "LIỆT UI" KHI MỞ CHAT
        // =====================================================================
        EventSystem[] systems = FindObjectsOfType<EventSystem>();
        if (systems.Length > 1)
        {
            Debug.LogWarning($"<color=orange>[Auto Fix]</color> Phát hiện {systems.Length} EventSystem đang đánh nhau! Đang tiêu diệt bớt...");
            for (int i = 1; i < systems.Length; i++) Destroy(systems[i].gameObject);
        }

        if (EventSystem.current != null)
        {
            var inputModule = EventSystem.current.GetComponent<BaseInputModule>();
            if (inputModule == null)
            {
                Debug.LogWarning("<color=orange>[Auto Fix]</color> EventSystem bị mất StandaloneInputModule (Não bộ)! Đang tự động gắn lại...");
                EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            }
        }
        else
        {
            Debug.LogError("<color=red>[CRITICAL]</color> Không có EventSystem! Vui lòng tạo 1 cái trong Scene!");
        }
        // =====================================================================

        if (chatCanvas == null) { Debug.LogError("Chat Canvas bị NULL!"); return; }
        
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
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playerInputField.gameObject);
            Debug.Log("<color=green>[SUCCESS]</color> Đã ép Focus vào InputField thành công!");
        }
        playerInputField.ActivateInputField();
    }

    public void CloseChat()
    {
        Debug.Log("<color=magenta>[ACTION]</color> Đang đóng hộp thoại Chat...");
        if (chatCanvas == null) return;
        isChatting = false;
        chatCanvas.SetActive(false);

        if (playerMovement != null) playerMovement.SetMovementLock(false);
        if (playerAttack != null) playerAttack.enabled = true;
    }

    public void OnSendClicked()
    {
        Debug.Log("<color=magenta>[ACTION]</color> Nút SEND vừa được kích hoạt (Bằng chuột hoặc Enter)!");
        string userText = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) 
        {
            Debug.LogWarning("Ô chữ đang trống, không gửi đi.");
            return;
        }

        playerInputField.text = "";
        StartCoroutine(FocusInputDelay());
        npcTextDisplay.text = "<i>Alicia đang suy nghĩ...</i>";

        StartCoroutine(SendToLLM(userText));
    }

    IEnumerator SendToLLM(string userText)
    {
        Debug.Log($"<color=yellow>[API]</color> Bắt đầu gửi dữ liệu lên Cloudflare: {userText}");
        chatHistory.Add(new ChatMessage { role = "user", content = userText });
        OpenAiRequest requestData = new OpenAiRequest();

        string systemPrompt = $"You are Alicia, a female adventurer traveling with a player named {GameSession.PlayerName}. Current Relationship Score: {aliciaScript.relationshipScore} (-1000 to +1000). Reply in 1-3 short sentences. You MUST include a tag [REL: X] at the exact end of your message.";
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
                Debug.Log("<color=green>[API]</color> Nhận phản hồi thành công!");
                OpenAiResponse response = JsonUtility.FromJson<OpenAiResponse>(request.downloadHandler.text);
                string aiRawText = response.choices[0].message.content.Trim();

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
            else
            {
                npcTextDisplay.text = "<color=red>API Connection Error.</color>";
                Debug.LogError($"[API LỖI] {request.error}");
            }
        }
    }

    public List<ChatMessage> GetChatHistory() => chatHistory;
    public void SetChatHistory(List<ChatMessage> history) { chatHistory = history ?? new List<ChatMessage>(); }
}