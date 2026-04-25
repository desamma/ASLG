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

    [Header("Common UI")]
    public TMP_Text notificationText;

    [Header("Login UI")]
    public TMP_InputField loginUsernameInput; // Giả sử dùng Email để đăng nhập
    public TMP_InputField loginPasswordInput;

    [Header("Register UI")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;

    private Coroutine notificationCoroutine;

    // TODO: BẠN CẦN THAY ĐỔI ĐƯỜNG DẪN NÀY CHO KHỚP VỚI API CỦA BẠN
    private readonly string baseUrl = "https://aslbe-apapajdug3ege4cm.eastasia-01.azurewebsites.net/api/auth";

    private void Awake()
    {
        if (notificationText != null) notificationText.text = "";
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
        if (notificationText == null) return;
        if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);

        notificationText.text = message;
        notificationText.color = color;
        notificationCoroutine = StartCoroutine(ClearNotificationAfterDelay(3f));
    }

    private IEnumerator ClearNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        notificationText.text = "";
    }

    #region Scene Transitions
    public void OnClick_GoToRegisterBtn() => SceneManager.LoadScene("Register");
    public void OnClick_GoToLoginBtn() => SceneManager.LoadScene("Login");
    #endregion

    #region Login API
    public void OnLoginButtonClicked()
    {
        string email = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowNotification("Vui lòng điền đầy đủ thông tin!", Color.yellow);
            return;
        }

        // Tạo dữ liệu JSON để gửi đi
        LoginRequestData requestData = new LoginRequestData { email = email, password = password };
        string jsonData = JsonUtility.ToJson(requestData);

        Debug.Log($"[Login] Dữ liệu chuẩn bị gửi: {jsonData}");

        // Bắt đầu gửi API
        StartCoroutine(SendApiRequest(baseUrl + "/login", jsonData, OnLoginSuccess));
    }

    // ĐÃ SỬA: Chuyển dữ liệu cho StatsManager
    private void OnLoginSuccess(string responseText)
    {
        // 1. Dịch JSON từ Server trả về thành Object C#
        LoginResponseData responseData = JsonUtility.FromJson<LoginResponseData>(responseText);

        // 2. Kiểm tra xem có token hay không
        if (responseData != null && !string.IsNullOrEmpty(responseData.token))
        {
            // 3. LƯU TOKEN VÀ USER ID VÀO TOKEN MANAGER (Gọn gàng và an toàn tuyệt đối)
            TokenManager.SaveSession(responseData.token, responseData.userId);

            ShowNotification("Đăng nhập thành công!", Color.green);
            PlayerPrefs.SetString("CurrentUser", loginUsernameInput.text);
            PlayerPrefs.Save();


            SaveManager.OnLogin();
            

            // Chuyển Scene vào Game
            SceneManager.LoadScene(mainGameSceneIndex);
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
        string username = registerUsernameInput.text;
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;

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

        // Tạo dữ liệu JSON
        RegisterRequestData requestData = new RegisterRequestData
        {
            userName = username,
            email = email,
            password = password
        };
        string jsonData = JsonUtility.ToJson(requestData);

        Debug.Log($"[Register] Dữ liệu chuẩn bị gửi: {jsonData}");

        // Bắt đầu gửi API
        StartCoroutine(SendApiRequest(baseUrl + "/register", jsonData, OnRegisterSuccess));
    }

    private void OnRegisterSuccess(string responseText)
    {
        ShowNotification("Đăng ký thành công!", Color.green);
        Invoke(nameof(OnClick_GoToLoginBtn), 1.0f);
    }
    #endregion

    #region UnityWebRequest Core
    // Hàm dùng chung để gửi API POST
    private IEnumerator SendApiRequest(string url, string jsonData, System.Action<string> onSuccess)
    {
        ShowNotification("Đang xử lý...", Color.white);
        Debug.Log($"[API] Bắt đầu gửi POST tới: {url}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // Chuyển string JSON thành mảng byte
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Chờ phản hồi từ Server
            yield return request.SendWebRequest();

            Debug.Log($"[API] Mã phản hồi từ Server (Response Code): {request.responseCode}");

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[API LỖI KẾT NỐI] {request.error}");

                // Trích xuất chi tiết nội dung lỗi từ Backend trả về
                string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : "Không có dữ liệu trả về";
                Debug.LogError($"[API CHI TIẾT LỖI TỪ SERVER] {errorDetail}");

                ShowNotification("Lỗi từ server: " + errorDetail, Color.red);
            }
            else
            {
                // Thành công gọi hàm callback
                string responseText = request.downloadHandler.text;
                Debug.Log($"[API THÀNH CÔNG] Dữ liệu Server trả về: {responseText}");

                onSuccess?.Invoke(responseText);
            }
        }
    }
    #endregion
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