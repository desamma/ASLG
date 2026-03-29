using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; 
using TMPro;

public class AuthManager : MonoBehaviour
{
    [Header("Scene Settings")]
    // Đã thay đổi từ biến chuỗi (string) sang biến số nguyên (int) và đặt mặc định là 0
    public int mainGameSceneIndex = 0; 

    [Header("Login UI (Assign only in Login Scene)")]
    public TMP_InputField loginUsernameInput; // Vẫn dùng ô này để nhập Email nhé
    public TMP_InputField loginPasswordInput;

    [Header("Register UI (Assign only in Register Scene)")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput; 

    // Thêm hàm Start này vào bên trong class AuthManager
    private void Start()
    {
        // Kiểm tra xem các ô nhập liệu đã được gán vào chưa để tránh lỗi null
        if (loginPasswordInput != null)
        {
            // Ép ô mật khẩu đăng nhập thành dạng Password (hiện dấu *)
            loginPasswordInput.contentType = TMP_InputField.ContentType.Password;
            // Cập nhật lại giao diện để áp dụng thay đổi
            loginPasswordInput.ForceLabelUpdate(); 
        }

        if (registerPasswordInput != null)
        {
            // Ép ô mật khẩu đăng ký thành dạng Password
            registerPasswordInput.contentType = TMP_InputField.ContentType.Password;
            registerPasswordInput.ForceLabelUpdate();
        }

        if (registerConfirmPasswordInput != null)
        {
            // Ép ô xác nhận mật khẩu thành dạng Password
            registerConfirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
            registerConfirmPasswordInput.ForceLabelUpdate();
        }
    }

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
        Debug.Log("Navigating to Forgot Password interface...");
    }
    #endregion

    #region Login Local Functionality
    public void OnLoginButtonClicked()
    {
        string email = loginUsernameInput.text; 
        string password = loginPasswordInput.text;

        // 1. Kiểm tra rỗng
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("Please enter Email and Password!");
            return;
        }

        Debug.Log("Checking local storage for account...");

        // 2. Kiểm tra xem Email có tồn tại trong bộ nhớ không
        if (PlayerPrefs.HasKey(email))
        {
            // 3. Lấy dữ liệu đã lưu và chuyển đổi từ JSON về Object
            string jsonData = PlayerPrefs.GetString(email);
            LocalUserData savedData = JsonUtility.FromJson<LocalUserData>(jsonData);

            // 4. Kiểm tra Mật khẩu
            if (savedData.password == password)
            {
                Debug.Log($"Login Success! Welcome back, {savedData.userName}!");
                
                // (Tùy chọn) Lưu lại email của người đang chơi hiện tại
                PlayerPrefs.SetString("CurrentUser", email);
                PlayerPrefs.Save();

                // 5. Chuyển sang Scene Game ở vị trí số 0 (Scene Play)
                Debug.Log($"Loading Main Game Scene at index: {mainGameSceneIndex}");
                SceneManager.LoadScene(mainGameSceneIndex);
            }
            else
            {
                Debug.LogError("Incorrect password!");
            }
        }
        else
        {
            Debug.LogError("Email not found! Please register a new account.");
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

        // 1. Kiểm tra rỗng
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            Debug.LogError("Please fill in all fields!");
            return;
        }

        // 2. Kiểm tra mật khẩu xác nhận
        if (password != confirmPassword)
        {
            Debug.LogError("Passwords do not match!");
            return;
        }

        // 3. Kiểm tra xem Email đã bị người khác đăng ký chưa
        if (PlayerPrefs.HasKey(email))
        {
            Debug.LogError("This email is already registered!");
            return;
        }

        Debug.Log("Creating new local account...");

        // 4. Tạo gói dữ liệu và chuyển thành JSON
        LocalUserData newUser = new LocalUserData { 
            userName = username, 
            email = email, 
            password = password 
        };
        string jsonData = JsonUtility.ToJson(newUser);

        // 5. Lưu vào PlayerPrefs (Dùng email làm chìa khóa)
        PlayerPrefs.SetString(email, jsonData);
        PlayerPrefs.Save();

        Debug.Log("Registration successful! Redirecting to login...");
        
        // Trì hoãn 0.5 giây rồi chuyển về màn hình đăng nhập
        Invoke(nameof(OnClick_GoToLoginBtn), 0.5f); 
    }
    #endregion
}

// Class chuyên dụng để chứa thông tin người chơi lưu trong máy
[System.Serializable] 
public class LocalUserData 
{ 
    public string userName; 
    public string email; 
    public string password; 
}