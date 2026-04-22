using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AIDifficultyManager : MonoBehaviour
{
    public static AIDifficultyManager Instance { get; private set; }

    [Header("API Config")]
    public string apiUrl = "https://your-cloudflare-link.trycloudflare.com/v1/chat/completions";
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
        if (Input.GetKeyDown(KeyCode.F5) && !isEvaluating) StartCoroutine(EvaluateDifficultyRoutine());

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

        string systemPrompt = "You are a Game Director. Analyze player stats and return ONLY one tag: [DIFF: EASY], [DIFF: NORMAL], or [DIFF: HARD]. " +
                             "EASY if player struggles (many deaths, high damage taken). HARD if player is too strong (many kills, low damage taken).";

        string userPrompt = $"Stats: Deaths={totalDeaths}, Kills={totalKills}, DamageTaken={totalDamageTaken}, DamageDealt={totalDamageDealt}";

        OpenAiRequest requestData = new OpenAiRequest();
        requestData.messages.Add(new ChatMessage { role = "system", content = systemPrompt });
        requestData.messages.Add(new ChatMessage { role = "user", content = userPrompt });

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
                string response = JsonUtility.FromJson<OpenAiResponse>(request.downloadHandler.text).choices[0].message.content;
                ApplyDifficulty(response);
            }
        }
        ResetCycle();
    }

    private void ApplyDifficulty(string tag)
    {
        var stats = StatsManager.instance;
        if (stats == null) return;

        if (tag.Contains("EASY"))
        {
            stats.damageDealtMultiplier = 1.25f; // Tăng 25% sát thương Player
            stats.damageTakenMultiplier = 0.75f; // Giảm 25% sát thương nhận vào
            Debug.Log("<color=green>[AI Director] Chế độ DỄ: Player được Buff.</color>");
        }
        else if (tag.Contains("HARD"))
        {
            stats.damageDealtMultiplier = 0.75f; // Giảm 25% sát thương Player
            stats.damageTakenMultiplier = 1.25f; // Tăng 25% sát thương nhận vào
            Debug.Log("<color=red>[AI Director] Chế độ KHÓ: Player bị Debuff.</color>");
        }
        else
        {
            stats.damageDealtMultiplier = 1.0f;
            stats.damageTakenMultiplier = 1.0f;
            Debug.Log("<color=yellow>[AI Director] Chế độ BÌNH THƯỜNG.</color>");
        }
    }
}