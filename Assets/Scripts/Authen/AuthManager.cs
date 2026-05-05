using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public int mainGameSceneIndex = 0;
    [SerializeField] private bool enableOnlineActivity = true;

    [Header("Common UI")]
    public TMP_Text notificationText;

    [Header("Login UI")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register UI")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;

    private Coroutine notificationCoroutine;
    private readonly string baseUrl = "https://aslbe-gsetazbeg8f2g9b0.indonesiacentral-01.azurewebsites.net/api/auth";
    private bool IsOnlineActivityEnabled => enableOnlineActivity && !GameSettings.IsOfflineMode;

    private void Awake()
    {
        if (notificationText != null)
        {
            notificationText.text = string.Empty;
        }
    }

    private void Start()
    {
        SetupPasswordFields();
    }

    private void SetupPasswordFields()
    {
        if (loginPasswordInput != null) loginPasswordInput.contentType = TMP_InputField.ContentType.Password;
        if (registerPasswordInput != null) registerPasswordInput.contentType = TMP_InputField.ContentType.Password;
        if (registerConfirmPasswordInput != null) registerConfirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
    }

    private void ShowNotification(string message, Color color)
    {
        if (notificationText == null)
        {
            return;
        }

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        notificationText.text = message;
        notificationText.color = color;
        notificationCoroutine = StartCoroutine(ClearNotificationAfterDelay(3f));
    }

    private IEnumerator ClearNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (notificationText != null)
        {
            notificationText.text = string.Empty;
        }
    }

    #region Scene Transitions
    public void OnClick_GoToRegisterBtn() => SceneManager.LoadScene("Register");
    public void OnClick_GoToLoginBtn() => SceneManager.LoadScene("Login");
    #endregion

    #region Login API
    public void OnLoginButtonClicked()
    {
        string email = loginUsernameInput != null ? loginUsernameInput.text : string.Empty;
        string password = loginPasswordInput != null ? loginPasswordInput.text : string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowNotification("Vui lòng điền đầy đủ thông tin!", Color.yellow);
            return;
        }

        PrepareOnlineSession();

        LoginRequestData requestData = new LoginRequestData { email = email, password = password };
        string jsonData = JsonUtility.ToJson(requestData);

        Debug.Log($"[Login] Dữ liệu chuẩn bị gửi: {jsonData}");

        StartCoroutine(SendApiRequest(baseUrl + "/login", jsonData, OnLoginSuccess));
    }

    private void OnLoginSuccess(string responseText)
    {
        LoginResponseData responseData = JsonUtility.FromJson<LoginResponseData>(responseText);

        if (responseData != null && !string.IsNullOrEmpty(responseData.token))
        {
            TokenManager.SaveSession(responseData.token, responseData.userId);
            ShowNotification("Đăng nhập thành công!", Color.green);
            //PlayerPrefs.SetString("CurrentUser", loginUsernameInput.text);
            //PlayerPrefs.Save();

            GameSettings.BeginOnlineSession(loginUsernameInput != null ? loginUsernameInput.text : null);
            SetOnlineActivityEnabled(true);

            SaveManager.OnLogin();

            SceneManager.LoadScene("Start");
            return;
        }
        else
        {
            Debug.LogError($"[Lỗi Token] Server trả về thành công nhưng không thấy Token. Response: {responseText}");
            ShowNotification("Lỗi: Không nhận được mã xác thực từ Server!", Color.red);
        }
    }
    #endregion

    #region Register API
    public void OnRegisterButtonClicked()
    {
        string username = registerUsernameInput != null ? registerUsernameInput.text : string.Empty;
        string email = registerEmailInput != null ? registerEmailInput.text : string.Empty;
        string password = registerPasswordInput != null ? registerPasswordInput.text : string.Empty;
        string confirmPassword = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowNotification("Vui lòng nhập đủ các trường!", Color.yellow);
            return;
        }

        if (password != confirmPassword)
        {
            ShowNotification("Mật khẩu xác nhận không khớp!", Color.red);
            return;
        }

        PrepareOnlineSession();
        RegisterRequestData requestData = new RegisterRequestData
        {
            userName = username,
            email = email,
            password = password
        };
        string jsonData = JsonUtility.ToJson(requestData);

        Debug.Log($"[Register] Dữ liệu chuẩn bị gửi: {jsonData}");

        StartCoroutine(SendApiRequest(baseUrl + "/register", jsonData, OnRegisterSuccess));
    }

    private void OnRegisterSuccess(string responseText)
    {
        ShowNotification("Đăng ký thành công!", Color.green);
        Invoke(nameof(OnClick_GoToLoginBtn), 1.0f);
    }
    #endregion

    #region UnityWebRequest Core
    private IEnumerator SendApiRequest(string url, string jsonData, System.Action<string> onSuccess)
    {
        ShowNotification("Đang xử lý...", Color.white);
        Debug.Log($"[API] Bắt đầu gửi POST tới: {url}");
        if (!IsOnlineActivityEnabled)
        {
            ShowNotification("Offline mode dang bat, khong the goi API.", Color.yellow);
            yield break;
        }

        ShowNotification("Dang xu ly...", Color.white);
        Debug.Log($"[API] POST {url}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            Debug.Log($"[API] Mã phản hồi từ Server (Response Code): {request.responseCode}");

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[API LỖI KẾT NỐI] {request.error}");

                string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : "Không có dữ liệu trả về";
                Debug.LogError($"[API CHI TIẾT LỖI TỪ SERVER] {errorDetail}");

                ShowNotification("Lỗi từ server: " + errorDetail, Color.red);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[API THÀNH CÔNG] Dữ liệu Server trả về: {responseText}");

                onSuccess?.Invoke(responseText);
            }
        }
    }
    #endregion

    public void OnPlayOfflineButtonClicked()
    {
        GameSettings.BeginGuestSession();
        SetOnlineActivityEnabled(false);
        SceneManager.LoadScene("Start");
    }

    private void PrepareOnlineSession()
    {
        GameSettings.BeginOnlineSession();
        SetOnlineActivityEnabled(true);
    }

    private void SetOnlineActivityEnabled(bool enabled)
    {
        enableOnlineActivity = enabled;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetOnlineActivityEnabled(enabled);
        }
    }
}

[System.Serializable]
public class LoginRequestData
{
    public string email;
    public string password;
}

[System.Serializable]
public class RegisterRequestData
{
    public string userName;
    public string email;
    public string password;
}

[System.Serializable]
public class LoginResponseData
{
    public string token;
    public string userId;
}