using System.Collections;
using System.Collections.Generic; // Thêm thư viện này để sử dụng List
using System.IO; // Thêm thư viện này để thao tác với File
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public int mainGameSceneIndex = 0;

    [Header("Login UI (Assign only in Login Scene)")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register UI (Assign only in Register Scene)")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;

    // Đường dẫn tới tệp lưu trữ dữ liệu
    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Application.persistentDataPath + "/users.json";
    }

    private void Start()
    {
        if (loginPasswordInput != null)
        {
            loginPasswordInput.contentType = TMP_InputField.ContentType.Password;
            loginPasswordInput.ForceLabelUpdate();
        }

        if (registerPasswordInput != null)
        {
            registerPasswordInput.contentType = TMP_InputField.ContentType.Password;
            registerPasswordInput.ForceLabelUpdate();
        }

        if (registerConfirmPasswordInput != null)
        {
            registerConfirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
            registerConfirmPasswordInput.ForceLabelUpdate();
        }
    }

    #region File Handling Methods
    // Hàm tải dữ liệu từ tệp
    private UserDatabase LoadDatabase()
    {
        if (File.Exists(saveFilePath))
        {
            string jsonData = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<UserDatabase>(jsonData);
        }
        // Nếu tệp chưa tồn tại, trả về một database trống mới
        return new UserDatabase();
    }

    // Hàm lưu dữ liệu xuống tệp
    private void SaveDatabase(UserDatabase database)
    {
        string jsonData = JsonUtility.ToJson(database, true); // true để format JSON dễ nhìn hơn
        File.WriteAllText(saveFilePath, jsonData);
    }
    #endregion

    #region Scene Transitions
    public void OnClick_GoToRegisterBtn()
    {
        SceneManager.LoadScene("Register");
    }

    public void OnClick_GoToLoginBtn()
    {
        SceneManager.LoadScene("Login");
    }

    public void OnClick_ForgotPasswordBtn()
    {
    }
    #endregion

    #region Login Local Functionality
    public void OnLoginButtonClicked()
    {
        string email = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Vui lòng điền đầy đủ Email và Mật khẩu!");
            return;
        }

        // Tải danh sách người dùng từ tệp
        UserDatabase database = LoadDatabase();

        // Tìm kiếm xem có tài khoản nào khớp với email vừa nhập không
        LocalUserData foundUser = database.users.Find(u => u.email == email);

        if (foundUser != null)
        {
            if (foundUser.password == password)
            {
                Debug.Log("<color=green>Đăng nhập thành công!</color> Chào mừng: " + email);

                // Lưu phiên đăng nhập hiện tại (có thể giữ PlayerPrefs cho việc này vì nó chỉ là trạng thái tạm thời)
                PlayerPrefs.SetString("CurrentUser", email);
                PlayerPrefs.Save();

                SceneManager.LoadScene(mainGameSceneIndex);
            }
            else
            {
                Debug.LogError("Sai mật khẩu!");
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy tài khoản nào với email này!");
        }
    }
    #endregion

    #region Register Local Functionality
    public void OnRegisterButtonClicked()
    {
        string username = registerUsernameInput.text;
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            Debug.LogWarning("Vui lòng điền đầy đủ thông tin đăng ký!");
            return;
        }

        if (password != confirmPassword)
        {
            Debug.LogError("Mật khẩu xác nhận không khớp!");
            return;
        }

        // Tải danh sách người dùng từ tệp
        UserDatabase database = LoadDatabase();

        // Kiểm tra xem email đã tồn tại trong danh sách chưa
        if (database.users.Exists(u => u.email == email))
        {
            Debug.LogError("Email này đã được đăng ký!");
            return;
        }

        // Tạo tài khoản mới và thêm vào danh sách
        LocalUserData newUser = new LocalUserData
        {
            userName = username,
            email = email,
            password = password
        };
        database.users.Add(newUser);

        // Lưu danh sách mới xuống tệp
        SaveDatabase(database);

        Debug.Log("<color=green>Đăng ký thành công!</color> Chuyển hướng sang màn hình đăng nhập...");
        Invoke(nameof(OnClick_GoToLoginBtn), 0.5f);
    }
    #endregion
}

[System.Serializable]
public class LocalUserData
{
    public string userName;
    public string email;
    public string password;
}

// Class mới tạo để quản lý danh sách người dùng
[System.Serializable]
public class UserDatabase
{
    public List<LocalUserData> users = new List<LocalUserData>();
}