using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ApiSettingsManager : MonoBehaviour
{
    public static ApiSettingsManager Instance { get; private set; }

    [Header("Backend Config")]
    public string backendUrl = "https://aslfe.azurewebsites.net";
    public string apiPath = "api/admin/settings/api-keys";

    // Danh sách lưu trữ trên RAM sau khi tải từ Web
    public List<string> WebGeminiKeys { get; private set; } = new List<string>();
    public List<string> WebColabUrls { get; private set; } = new List<string>();

    public bool IsReady { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(FetchSettingsRoutine());
    }

    private IEnumerator FetchSettingsRoutine()
    {
        string url = $"{backendUrl.TrimEnd('/')}/{apiPath.TrimStart('/')}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Lấy Token từ TokenManager của bạn
            string token = TokenManager.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<ApiSettingResponse>(request.downloadHandler.text);
                    if (response != null && response.Success && response.Data != null)
                    {
                        foreach (var item in response.Data)
                        {
                            if (!string.IsNullOrWhiteSpace(item.GeminiApiKey)) WebGeminiKeys.Add(item.GeminiApiKey);
                            if (!string.IsNullOrWhiteSpace(item.ColabApiUrl)) WebColabUrls.Add(item.ColabApiUrl);
                        }
                        Debug.Log($"<color=green>[API Manager]</color> Đã tải {WebGeminiKeys.Count} Gemini Keys và {WebColabUrls.Count} Colab URLs từ Web.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"<color=orange>[API Manager]</color> Lỗi Parse JSON từ Web: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"<color=orange>[API Manager]</color> Không lấy được Key từ Web (Do 403 Không phải Admin hoặc lỗi mạng). Sẽ dùng Fallback nội bộ! Lỗi: {request.error}");
            }
        }
        IsReady = true;
    }

    // Các class con dùng để Parse JSON trả về từ backend của bạn
    public class ApiSettingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<ApiSettingDto> Data { get; set; }
    }

    public class ApiSettingDto
    {
        public string GeminiApiKey { get; set; }
        public string ColabApiUrl { get; set; }
    }
}