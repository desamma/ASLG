using System.Collections;
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
            return;
        }

        if (PlayerPrefs.HasKey(email))
        {
            string jsonData = PlayerPrefs.GetString(email);
            LocalUserData savedData = JsonUtility.FromJson<LocalUserData>(jsonData);

            if (savedData.password == password)
            {
                PlayerPrefs.SetString("CurrentUser", email);
                PlayerPrefs.Save();

                SceneManager.LoadScene(mainGameSceneIndex);
            }
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
            return;
        }

        if (password != confirmPassword)
        {
            return;
        }

        if (PlayerPrefs.HasKey(email))
        {
            return;
        }

        LocalUserData newUser = new LocalUserData { 
            userName = username, 
            email = email, 
            password = password 
        };
        string jsonData = JsonUtility.ToJson(newUser);

        PlayerPrefs.SetString(email, jsonData);
        PlayerPrefs.Save();
        
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