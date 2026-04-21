using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public static float mouseSensitivity = 1f;

    [Header("UI References")]
    public GameObject pauseMenuUI;
    public GameObject settingMenuUI;

    [Header("Settings Tabs (Panels)")]
    public GameObject soundPanel;
    public GameObject mousePanel;
    public GameObject keybindPanel;

    [Header("Audio Settings")]
    public AudioMixer mainMixer;
    public Slider volumeSlider;

    [Header("Mouse Settings")]
    public Slider mouseSlider;

    [Header("Sound Effects")]
    public AudioClip buttonClickSound;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        if (settingMenuUI != null) settingMenuUI.SetActive(false);

        // Load cài đặt âm thanh
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolumePref", 1f);
            volumeSlider.value = savedVolume;
            SetMasterVolume(savedVolume);
        }

        // Load cài đặt tốc độ chuột
        if (mouseSlider != null)
        {
            float savedMouseSense = PlayerPrefs.GetFloat("MouseSensePref", 1f);
            mouseSlider.value = savedMouseSense;
            SetMouseSensitivity(savedMouseSense);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingMenuUI != null && settingMenuUI.activeSelf) CloseSettings();
            else if (GameIsPaused) Resume();
            else Pause();
        }
    }

    public void Resume() { PlayClickSound(); pauseMenuUI.SetActive(false); Time.timeScale = 1f; GameIsPaused = false; }
    void Pause() { pauseMenuUI.SetActive(true); Time.timeScale = 0f; GameIsPaused = true; }

    public void OpenSettings()
    {
        PlayClickSound();
        pauseMenuUI.SetActive(false);
        settingMenuUI.SetActive(true);

        // Mặc định mở Tab Âm thanh khi vừa vào Setting
        OpenSoundTab();
    }

    public void CloseSettings()
    {
        PlayClickSound();
        settingMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    // ================== HỆ THỐNG CHUYỂN TAB ==================
    public void OpenSoundTab()
    {
        PlayClickSound();
        soundPanel.SetActive(true);
        mousePanel.SetActive(false);
        keybindPanel.SetActive(false);
    }

    public void OpenMouseTab()
    {
        PlayClickSound();
        soundPanel.SetActive(false);
        mousePanel.SetActive(true);
        keybindPanel.SetActive(false);
    }

    public void OpenKeybindTab()
    {
        PlayClickSound();
        soundPanel.SetActive(false);
        mousePanel.SetActive(false);
        keybindPanel.SetActive(true);
    }

    // ================== HÀM CHỈNH ÂM LƯỢNG ==================
    public void SetMasterVolume(float sliderValue)
    {
        float dbValue = Mathf.Log10(sliderValue) * 20f;
        if (sliderValue <= 0.0001f) dbValue = -80f;
        mainMixer.SetFloat("MasterVolume", dbValue);
        PlayerPrefs.SetFloat("MasterVolumePref", sliderValue);
    }

    // ================== HÀM CHỈNH TỐC ĐỘ CHUỘT ==================
    public void SetMouseSensitivity(float sliderValue)
    {
        mouseSensitivity = sliderValue; // Cập nhật biến toàn cục
        PlayerPrefs.SetFloat("MouseSensePref", sliderValue); // Lưu vào máy
    }

    private void PlayClickSound()
    {
        if (buttonClickSound != null && SoundFXManager.Instance != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(buttonClickSound, transform, 1f);
        }
    }
}