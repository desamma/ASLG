using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AIDifficultyManager : MonoBehaviour
{
    public static AIDifficultyManager Instance { get; private set; }

    [Header("Network Fallback System")]
    public List<string> geminiApiKeys = new List<string>();
    public List<string> openAiUrls = new List<string>();

    [Header("API Config")]
    public float cycleDurationMinutes = 15f;
    private float timer;

    [Header("Tracking Data")]
    public float totalDamageTaken = 0f;
    public float totalDamageDealt = 0f;
    public int totalKills = 0;
    public int totalDeaths = 0;

    private bool isEvaluating = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start() => ResetCycle();

    private void Update()
    {
        // ĐÃ THÊM: Phím Backslash (\) để ép AI quét thủ công lập tức
        if ((Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.Backslash)) && !isEvaluating) 
        {
            Debug.Log("<color=cyan>[AI Director]</color> Người chơi kích hoạt quét quét thủ công bằng phím tắt!");
            StartCoroutine(EvaluateDifficultyRoutine());
        }

        if (!isEvaluating)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) StartCoroutine(EvaluateDifficultyRoutine());
        }
    }

    public void LogDamageTaken(float amt) => totalDamageTaken += amt;
    public void LogDamageDealt(float amt) => totalDamageDealt += amt;
    public void LogKill() => totalKills++;
    public void LogDeath() => totalDeaths++;

    private void ResetCycle()
    {
        timer = cycleDurationMinutes * 60f;
        totalDamageTaken = totalDamageDealt = 0;
        totalKills = totalDeaths = 0;
        isEvaluating = false;
    }

    private IEnumerator EvaluateDifficultyRoutine()
    {
        isEvaluating = true;
        Debug.Log("<color=cyan>[AI Director]</color> Đang phân tích hiệu suất người chơi...");

        string systemPrompt = "You are a Game Director. Analyze player stats and return ONLY one tag: [DIFF: EASY], [DIFF: NORMAL], or [DIFF: HARD]. EASY if player struggles. HARD if player is too strong.";
        string userPrompt = $"Stats: Deaths={totalDeaths}, Kills={totalKills}, DamageTaken={totalDamageTaken}, DamageDealt={totalDamageDealt}";
        
        string aiResponse = null;

        foreach (string key in geminiApiKeys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            yield return StartCoroutine(CallGeminiAPI(key.Trim(), systemPrompt, userPrompt, (res) => aiResponse = res));
            if (!string.IsNullOrEmpty(aiResponse)) break;
        }

        if (string.IsNullOrEmpty(aiResponse))
        {
            foreach (string url in openAiUrls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                yield return StartCoroutine(CallOpenAiAPI(url.Trim(), systemPrompt, userPrompt, (res) => aiResponse = res));
                if (!string.IsNullOrEmpty(aiResponse)) break;
            }
        }

        if (!string.IsNullOrEmpty(aiResponse)) 
        {
            // Báo log RAW để bắt lỗi xem AI có trả lời tào lao không
            Debug.Log($"<color=magenta>[AI Director RAW]</color> Quyết định của AI: {aiResponse}");
            ApplyDifficulty(aiResponse);
        }
        else Debug.LogError("<color=red>[AI Director]</color> Tất cả API đều thất bại. Bỏ qua chu kỳ này.");
        
        ResetCycle();
    }

    private IEnumerator CallGeminiAPI(string apiKey, string sysPrompt, string userPrompt, System.Action<string> onComplete)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        
        GeminiRequest requestData = new GeminiRequest();
        requestData.systemInstruction = new GeminiSystemInstruction { parts = new List<GeminiPart> { new GeminiPart { text = sysPrompt } } };
        requestData.contents.Add(new GeminiContent { role = "user", parts = new List<GeminiPart> { new GeminiPart { text = userPrompt } } });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            string rawResponse = request.downloadHandler.text;

            if (request.result == UnityWebRequest.Result.Success)
            {
                GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(rawResponse);
                if (res != null && res.candidates != null && res.candidates.Count > 0) 
                    onComplete?.Invoke(res.candidates[0].content.parts[0].text);
                else 
                {
                    Debug.LogWarning($"<color=orange>[AI Director Gemini Warning]</color> API OK nhưng không có nội dung. RAW: {rawResponse}");
                    onComplete?.Invoke(null);
                }
            }
            else 
            {
                Debug.LogError($"<color=red>[AI Director Gemini LỖI]</color> {request.error}. RAW: {rawResponse}");
                onComplete?.Invoke(null);
            }
        }
    }

    private IEnumerator CallOpenAiAPI(string url, string sysPrompt, string userPrompt, System.Action<string> onComplete)
    {
        OpenAiRequest requestData = new OpenAiRequest();
        requestData.messages.Add(new ChatMessage { role = "system", content = sysPrompt });
        requestData.messages.Add(new ChatMessage { role = "user", content = userPrompt });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            
            string rawResponse = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAiResponse res = JsonUtility.FromJson<OpenAiResponse>(rawResponse);
                if (res != null && res.choices != null && res.choices.Count > 0)
                    onComplete?.Invoke(res.choices[0].message.content);
                else onComplete?.Invoke(null);
            }
            else 
            {
                Debug.LogError($"<color=red>[AI Director OpenAI LỖI]</color> {request.error}. RAW: {rawResponse}");
                onComplete?.Invoke(null);
            }
        }
    }

    private void ApplyDifficulty(string rawTag)
    {
        string tag = rawTag.ToUpper(); // Ép viết hoa toàn bộ để hàm Contains không bị bắt hụt lỗi chính tả
        var stats = StatsManager.instance;
        DifficultyModifier newMod = new DifficultyModifier();

        if (tag.Contains("EASY")) 
        { 
            newMod.Overall = 0.7f; // Giảm chỉ số quái đi 30%
            if (stats != null) { stats.damageDealtMultiplier = 1.25f; stats.damageTakenMultiplier = 0.75f; }
            if (DifficultyManager.Instance != null) DifficultyManager.Instance.SetDifficulty(newMod); // Báo cho Enemy biết
            
            Debug.Log("<color=green>[AI Director] Kích hoạt chế độ DỄ. Đã giảm sức mạnh quái.</color>"); 
        }
        else if (tag.Contains("HARD")) 
        { 
            newMod.Overall = 1.3f; // Tăng chỉ số quái thêm 30%
            if (stats != null) { stats.damageDealtMultiplier = 0.75f; stats.damageTakenMultiplier = 1.25f; }
            if (DifficultyManager.Instance != null) DifficultyManager.Instance.SetDifficulty(newMod); // Báo cho Enemy biết
            
            Debug.Log("<color=red>[AI Director] Kích hoạt chế độ KHÓ. Quái vật đã được cường hóa!</color>"); 
        }
        else 
        { 
            newMod.Overall = 1.0f; 
            if (stats != null) { stats.damageDealtMultiplier = 1.0f; stats.damageTakenMultiplier = 1.0f; }
            if (DifficultyManager.Instance != null) DifficultyManager.Instance.SetDifficulty(newMod);
            
            Debug.Log("<color=yellow>[AI Director] Kích hoạt chế độ BÌNH THƯỜNG. Không đổi.</color>"); 
        }
    }
}