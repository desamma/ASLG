using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public int mainGameSceneIndex = 0;

    [Header("Common UI")]
    public TMP_Text notificationText; // Kéo thả một cái Text (TextMeshPro) vào đây để hiện thông báo

    [Header("Login UI")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register UI")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;

    private string saveFilePath;
    private Coroutine notificationCoroutine;

    private void Awake()
    {
        saveFilePath = Application.persistentDataPath + "/users.json";
        // Đảm bảo Text thông báo trống khi bắt đầu
        if (notificationText != null) notificationText.text = "";
    }

    private void Start()
    {
        SetupPasswordFields();
    }

    // Thiết lập các ô nhập password tự ẩn ký tự
    private void SetupPasswordFields()
    {
        if (loginPasswordInput != null) loginPasswordInput.contentType = TMP_InputField.ContentType.Password;
        if (registerPasswordInput != null) registerPasswordInput.contentType = TMP_InputField.ContentType.Password;
        if (registerConfirmPasswordInput != null) registerConfirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
    }

    // Hàm hiển thị thông báo lên UI thay vì Debug.Log
    private void ShowNotification(string message, Color color)
    {
        if (notificationText == null) return;

        if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);

        notificationText.text = message;
        notificationText.color = color;

        // Tự động xóa thông báo sau 3 giây
        notificationCoroutine = StartCoroutine(ClearNotificationAfterDelay(3f));
    }

    private IEnumerator ClearNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        notificationText.text = "";
    }

    #region File Handling
    private UserDatabase LoadDatabase()
    {
        if (File.Exists(saveFilePath))
        {
            string jsonData = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<UserDatabase>(jsonData);
        }
        return new UserDatabase();
    }

    private void SaveDatabase(UserDatabase database)
    {
        string jsonData = JsonUtility.ToJson(database, true);
        File.WriteAllText(saveFilePath, jsonData);
    }
    #endregion

    #region Scene Transitions
    public void OnClick_GoToRegisterBtn() => SceneManager.LoadScene("Register");
    public void OnClick_GoToLoginBtn() => SceneManager.LoadScene("Login");
    #endregion

    #region Login Logic
    public void OnLoginButtonClicked()
    {
        string email = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowNotification("Vui lòng điền đầy đủ thông tin!", Color.yellow);
            return;
        }

        UserDatabase database = LoadDatabase();
        LocalUserData foundUser = database.users.Find(u => u.email == email);

        if (foundUser != null)
        {
            if (foundUser.password == password)
            {
                ShowNotification("Đăng nhập thành công!", Color.green);
                PlayerPrefs.SetString("CurrentUser", email);
                PlayerPrefs.Save();
                SceneManager.LoadScene(mainGameSceneIndex);
            }
            else
            {
                ShowNotification("Sai mật khẩu!", Color.red);
            }
        }
        else
        {
            ShowNotification("Tài khoản không tồn tại!", Color.red);
        }
    }
    #endregion

    #region Register Logic
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

        UserDatabase database = LoadDatabase();

        if (database.users.Exists(u => u.email == email))
        {
            ShowNotification("Email này đã được đăng ký!", Color.red);
            return;
        }

        database.users.Add(new LocalUserData { userName = username, email = email, password = password });
        SaveDatabase(database);

        ShowNotification("Đăng ký thành công!", Color.green);
        Invoke(nameof(OnClick_GoToLoginBtn), 1.0f);
    }
    #endregion
}

[System.Serializable]
public class LocalUserData { public string userName; public string email; public string password; }

[System.Serializable]
public class UserDatabase { public List<LocalUserData> users = new List<LocalUserData>(); }