using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AIDifficultyManager : MonoBehaviour
{
    public static AIDifficultyManager Instance { get; private set; }

    [Header("Network Fallback System (Inspector)")]
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
    private const string ULTIMATE_GEMINI_KEY = "AIzaSyCP-sVakxDa3dlNnST4Frl-dVEtxQAxEmI";

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start() => ResetCycle();

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.Backslash)) && !isEvaluating) 
        {
            Debug.Log("<color=cyan>[AI Director]</color> Quét thủ công!");
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
    private void ResetCycle() { timer = cycleDurationMinutes * 60f; totalDamageTaken = totalDamageDealt = 0; totalKills = totalDeaths = 0; isEvaluating = false; }

    private List<string> GetGeminiKeys()
    {
        List<string> keys = new List<string>();
        if (ApiSettingsManager.Instance != null) keys.AddRange(ApiSettingsManager.Instance.WebGeminiKeys); // Lớp 1
        keys.AddRange(geminiApiKeys); // Lớp 2
        keys.Add(ULTIMATE_GEMINI_KEY); // Lớp 3
        return keys;
    }

    private List<string> GetColabUrls()
    {
        List<string> urls = new List<string>();
        if (ApiSettingsManager.Instance != null) urls.AddRange(ApiSettingsManager.Instance.WebColabUrls); // Lớp 1
        urls.AddRange(openAiUrls); // Lớp 2
        return urls;
    }

    private IEnumerator EvaluateDifficultyRoutine()
    {
        isEvaluating = true;
        string systemPrompt = "You are a Game Director. Analyze player stats and return ONLY one tag: [DIFF: EASY], [DIFF: NORMAL], or [DIFF: HARD]. EASY if player struggles. HARD if player is too strong.";
        string userPrompt = $"Stats: Deaths={totalDeaths}, Kills={totalKills}, DamageTaken={totalDamageTaken}, DamageDealt={totalDamageDealt}";
        string aiResponse = null;

        foreach (string key in GetGeminiKeys())
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            yield return StartCoroutine(CallGeminiAPI(key.Trim(), systemPrompt, userPrompt, (res) => aiResponse = res));
            if (!string.IsNullOrEmpty(aiResponse)) break;
        }

        if (string.IsNullOrEmpty(aiResponse))
        {
            foreach (string url in GetColabUrls())
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                yield return StartCoroutine(CallOpenAiAPI(url.Trim(), systemPrompt, userPrompt, (res) => aiResponse = res));
                if (!string.IsNullOrEmpty(aiResponse)) break;
            }
        }

        if (!string.IsNullOrEmpty(aiResponse)) 
        {
            Debug.Log($"<color=magenta>[AI Director RAW]</color> {aiResponse}");
            ApplyDifficulty(aiResponse);
        }
        
        ResetCycle();
    }

    private IEnumerator CallGeminiAPI(string apiKey, string sysPrompt, string userPrompt, System.Action<string> onComplete)
    {
        // DÒNG MỚI:
string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        GeminiRequest requestData = new GeminiRequest();
        requestData.systemInstruction = new GeminiSystemInstruction { parts = new List<GeminiPart> { new GeminiPart { text = sysPrompt } } };
        requestData.contents.Add(new GeminiContent { role = "user", parts = new List<GeminiPart> { new GeminiPart { text = userPrompt } } });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                if (res != null && res.candidates != null && res.candidates.Count > 0) onComplete?.Invoke(res.candidates[0].content.parts[0].text);
                else onComplete?.Invoke(null);
            }
            else onComplete?.Invoke(null);
        }
    }

    private IEnumerator CallOpenAiAPI(string url, string sysPrompt, string userPrompt, System.Action<string> onComplete)
    {
        OpenAiRequest requestData = new OpenAiRequest();
        requestData.messages.Add(new ChatMessage { role = "system", content = sysPrompt });
        requestData.messages.Add(new ChatMessage { role = "user", content = userPrompt });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAiResponse res = JsonUtility.FromJson<OpenAiResponse>(request.downloadHandler.text);
                if (res != null && res.choices != null && res.choices.Count > 0) onComplete?.Invoke(res.choices[0].message.content);
                else onComplete?.Invoke(null);
            }
            else onComplete?.Invoke(null);
        }
    }

    private void ApplyDifficulty(string rawTag)
    {
        string tag = rawTag.ToUpper(); 
        var stats = StatsManager.instance;
        DifficultyModifier newMod = new DifficultyModifier();

        if (tag.Contains("EASY")) 
        { 
            newMod.Overall = 0.7f; 
            if (stats != null) { stats.damageDealtMultiplier = 1.25f; stats.damageTakenMultiplier = 0.75f; }
            if (DifficultyManager.Instance != null) DifficultyManager.Instance.SetDifficulty(newMod); 
            Debug.Log("<color=green>[AI Director] DỄ.</color>"); 
        }
        else if (tag.Contains("HARD")) 
        { 
            newMod.Overall = 1.3f; 
            if (stats != null) { stats.damageDealtMultiplier = 0.75f; stats.damageTakenMultiplier = 1.25f; }
            if (DifficultyManager.Instance != null) DifficultyManager.Instance.SetDifficulty(newMod); 
            Debug.Log("<color=red>[AI Director] KHÓ.</color>"); 
        }
        else 
        { 
            newMod.Overall = 1.0f; 
            if (stats != null) { stats.damageDealtMultiplier = 1.0f; stats.damageTakenMultiplier = 1.0f; }
            if (DifficultyManager.Instance != null) DifficultyManager.Instance.SetDifficulty(newMod);
            Debug.Log("<color=yellow>[AI Director] BÌNH THƯỜNG.</color>"); 
        }
    }
}