using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public static float mouseSensitivity = 1f;
    public static System.Action<float> OnMouseSensitivityChanged;
    // Optional auto-apply to example CameraController if present in scene
    private TMPro.Examples.CameraController exampleCameraController;
    public bool autoApplyToExampleCamera = true;

    public static Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();
    private string actionToRebind = null;

    [Header("UI References")]
    public GameObject pauseMenuUI;
    public GameObject settingMenuUI;

    [Header("Settings Tabs (Panels)")]
    public GameObject soundPanel;
    public GameObject mousePanel;
    public GameObject keybindPanel;

    [Header("Audio Settings")]
    public AudioMixer mainMixer;
    public Slider volumeSlider; // Âm lượng Tổng
    public Slider bgmSlider;    // ĐÃ THÊM: Âm lượng Nhạc Nền (Background)
    public string masterParamName = "MasterVolume";
    public string bgmParamName = "BGMVolume";
    [Header("Exposed Audio Parameters")]
    public bool masterParamExposed = false;
    public bool bgmParamExposed = false;

    [Header("Optional Buttons (auto-hook if assigned)")]
    public Button volumeIncreaseButton;
    public Button volumeDecreaseButton;
    public Button bgmIncreaseButton;
    public Button bgmDecreaseButton;
    public Button mouseIncreaseButton;
    public Button mouseDecreaseButton;

    [Header("Mouse Settings")]
    public Slider mouseSlider;

    [Header("Keybind UI Texts")]
    public TMP_Text forwardText;
    public TMP_Text backwardText;
    public TMP_Text leftText;
    public TMP_Text rightText;
    public TMP_Text inventoryText;
    public TMP_Text questsText;
    public TMP_Text skill1Text;
    public TMP_Text skill2Text;
    public TMP_Text skill3Text;

    [Header("Sound Effects")]
    public AudioClip buttonClickSound;

    void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingMenuUI != null) settingMenuUI.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (mousePanel != null) mousePanel.SetActive(false);
        if (keybindPanel != null) keybindPanel.SetActive(false);

        // Load Audio
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolumePref", 1f);
            volumeSlider.value = savedVolume;
            // Hook slider change to apply immediately
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
            SetMasterVolume(savedVolume);
        }

        // Load Background Music
        if (bgmSlider != null)
        {
            float savedBGM = PlayerPrefs.GetFloat("BGMVolumePref", 1f);
            bgmSlider.value = savedBGM;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            SetBGMVolume(savedBGM);
        }

        // Load Mouse
        if (mouseSlider != null)
        {
            float savedMouseSense = PlayerPrefs.GetFloat("MouseSensePref", 1f);
            mouseSlider.value = savedMouseSense;
            mouseSlider.onValueChanged.RemoveAllListeners();
            mouseSlider.onValueChanged.AddListener(SetMouseSensitivity);
            SetMouseSensitivity(savedMouseSense);
        }

        // Try to auto-find example camera controller and apply mouse sensitivity
        if (autoApplyToExampleCamera)
        {
            exampleCameraController = FindObjectOfType<TMPro.Examples.CameraController>();
            if (exampleCameraController != null)
            {
                exampleCameraController.MoveSensitivity = mouseSensitivity;
                OnMouseSensitivityChanged += v => exampleCameraController.MoveSensitivity = v;
            }
        }

        // Auto-hook optional buttons so Increase/Decrease work if buttons are assigned
        if (volumeIncreaseButton != null) volumeIncreaseButton.onClick.AddListener(() => IncreaseVolume());
        if (volumeDecreaseButton != null) volumeDecreaseButton.onClick.AddListener(() => DecreaseVolume());
        if (bgmIncreaseButton != null) bgmIncreaseButton.onClick.AddListener(() => IncreaseBGMVolume());
        if (bgmDecreaseButton != null) bgmDecreaseButton.onClick.AddListener(() => DecreaseBGMVolume());
        if (mouseIncreaseButton != null) mouseIncreaseButton.onClick.AddListener(() => IncreaseMouseSense());
        if (mouseDecreaseButton != null) mouseDecreaseButton.onClick.AddListener(() => DecreaseMouseSense());

        LoadKeybinds();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && actionToRebind == null)
        {
            if ((soundPanel != null && soundPanel.activeSelf) || (mousePanel != null && mousePanel.activeSelf) || (keybindPanel != null && keybindPanel.activeSelf))
                BackToOptionMenu();
            else if (settingMenuUI != null && settingMenuUI.activeSelf)
                CloseSettings();
            else if (GameIsPaused)
                Resume();
            else
                Pause();
        }
    }

    // ================== MENU CHÍNH ==================
    public void Resume() { PlayClickSound(); pauseMenuUI.SetActive(false); Time.timeScale = 1f; GameIsPaused = false; }
    void Pause() { pauseMenuUI.SetActive(true); Time.timeScale = 0f; GameIsPaused = true; }
    public void OpenSettings() { PlayClickSound(); pauseMenuUI.SetActive(false); soundPanel.SetActive(false); mousePanel.SetActive(false); keybindPanel.SetActive(false); settingMenuUI.SetActive(true); }
    public void CloseSettings() { PlayClickSound(); settingMenuUI.SetActive(false); pauseMenuUI.SetActive(true); }

    public void OpenSoundTab() { PlayClickSound(); settingMenuUI.SetActive(false); soundPanel.SetActive(true); }
    public void OpenMouseTab() { PlayClickSound(); settingMenuUI.SetActive(false); mousePanel.SetActive(true); }
    public void OpenKeybindTab() { PlayClickSound(); settingMenuUI.SetActive(false); keybindPanel.SetActive(true); }
    public void BackToOptionMenu() { PlayClickSound(); soundPanel.SetActive(false); mousePanel.SetActive(false); keybindPanel.SetActive(false); settingMenuUI.SetActive(true); }

    // ================== ÂM LƯỢNG TỔNG ==================
    public void SetMasterVolume(float sliderValue)
    {
        // Clamp slider to valid range
        sliderValue = Mathf.Clamp01(sliderValue);
        float dbValue = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        if (sliderValue <= 0.0001f) dbValue = -80f;
        if (mainMixer != null && masterParamExposed)
        {
            mainMixer.SetFloat(masterParamName, dbValue);
        }
        PlayerPrefs.SetFloat("MasterVolumePref", sliderValue);
    }
    public void IncreaseVolume() { PlayClickSound(); if (volumeSlider != null) { volumeSlider.value += 0.1f; SetMasterVolume(volumeSlider.value); } }
    public void DecreaseVolume() { PlayClickSound(); if (volumeSlider != null) { volumeSlider.value -= 0.1f; SetMasterVolume(volumeSlider.value); } }

    // ================== ÂM LƯỢNG NHẠC NỀN ==================
    public void SetBGMVolume(float sliderValue)
    {
        // Clamp slider to valid range
        sliderValue = Mathf.Clamp01(sliderValue);
        float dbValue = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        if (sliderValue <= 0.0001f) dbValue = -80f;
        // LƯU Ý: Bạn cần Expose tham số trong AudioMixer của Unity nhé!
        if (mainMixer != null && bgmParamExposed)
        {
            mainMixer.SetFloat(bgmParamName, dbValue);
        }
        PlayerPrefs.SetFloat("BGMVolumePref", sliderValue);
    }
    public void IncreaseBGMVolume() { PlayClickSound(); float current = (bgmSlider != null) ? bgmSlider.value : PlayerPrefs.GetFloat("BGMVolumePref", 1f); current = Mathf.Clamp01(current + 0.1f); if (bgmSlider != null) bgmSlider.value = current; SetBGMVolume(current); }
    public void DecreaseBGMVolume() { PlayClickSound(); float current = (bgmSlider != null) ? bgmSlider.value : PlayerPrefs.GetFloat("BGMVolumePref", 1f); current = Mathf.Clamp01(current - 0.1f); if (bgmSlider != null) bgmSlider.value = current; SetBGMVolume(current); }

    // ================== ĐỘ NHẠY CHUỘT ==================
    public void SetMouseSensitivity(float sliderValue)
    {
        // Clamp to a reasonable range to avoid zero/negative values
        sliderValue = Mathf.Clamp(sliderValue, 0f, 10f);
        mouseSensitivity = sliderValue;
        PlayerPrefs.SetFloat("MouseSensePref", sliderValue);
        OnMouseSensitivityChanged?.Invoke(sliderValue);
        Debug.Log($"Mouse sensitivity set to {sliderValue}");
    }
    public void IncreaseMouseSense() { PlayClickSound(); float current = (mouseSlider != null) ? mouseSlider.value : PlayerPrefs.GetFloat("MouseSensePref", mouseSensitivity); current = Mathf.Clamp(current + 0.1f, 0f, 10f); if (mouseSlider != null) mouseSlider.value = current; SetMouseSensitivity(current); }
    public void DecreaseMouseSense() { PlayClickSound(); float current = (mouseSlider != null) ? mouseSlider.value : PlayerPrefs.GetFloat("MouseSensePref", mouseSensitivity); current = Mathf.Clamp(current - 0.1f, 0f, 10f); if (mouseSlider != null) mouseSlider.value = current; SetMouseSensitivity(current); }

    // ================== ĐỔI PHÍM ==================
    private void LoadKeybinds()
    {
        keys["Forward"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Forward", "W"), KeyCode.W);
        keys["Backward"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Backward", "S"), KeyCode.S);
        keys["Left"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Left", "A"), KeyCode.A);
        keys["Right"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Right", "D"), KeyCode.D);
        keys["Inventory"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Inventory", "B"), KeyCode.B);
        keys["Quests"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Quests", "J"), KeyCode.J);
        keys["Skill1"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Skill1", "Alpha1"), KeyCode.Alpha1);
        keys["Skill2"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Skill2", "Alpha2"), KeyCode.Alpha2);
        keys["Skill3"] = ParseKeyOrDefault(PlayerPrefs.GetString("Key_Skill3", "Alpha3"), KeyCode.Alpha3);
        UpdateAllKeybindUI();
    }
    private KeyCode ParseKeyOrDefault(string keyString, KeyCode defaultKey)
    {
        if (string.IsNullOrEmpty(keyString)) return defaultKey;
        if (System.Enum.TryParse<KeyCode>(keyString, out var parsed)) return parsed;
        return defaultKey;
    }
    public void StartRebind(string actionName) { PlayClickSound(); actionToRebind = actionName; UpdateAllKeybindUI(); }
    void OnGUI()
    {
        if (actionToRebind != null)
        {
            Event e = Event.current;
            if (e.isKey && e.type == EventType.KeyDown)
            {
                if (e.keyCode != KeyCode.Escape) { keys[actionToRebind] = e.keyCode; PlayerPrefs.SetString("Key_" + actionToRebind, e.keyCode.ToString()); }
                actionToRebind = null; UpdateAllKeybindUI();
            }
        }
    }
    private void UpdateAllKeybindUI()
    {
        if (forwardText != null) forwardText.text = GetKeyDisplay("Forward");
        if (backwardText != null) backwardText.text = GetKeyDisplay("Backward");
        if (leftText != null) leftText.text = GetKeyDisplay("Left");
        if (rightText != null) rightText.text = GetKeyDisplay("Right");
        if (inventoryText != null) inventoryText.text = GetKeyDisplay("Inventory");
        if (questsText != null) questsText.text = GetKeyDisplay("Quests");
        if (skill1Text != null) skill1Text.text = GetKeyDisplay("Skill1");
        if (skill2Text != null) skill2Text.text = GetKeyDisplay("Skill2");
        if (skill3Text != null) skill3Text.text = GetKeyDisplay("Skill3");
    }
    private string GetKeyDisplay(string action)
    {
        if (actionToRebind == action) return "...";
        if (!keys.ContainsKey(action)) return "-";
        return FormatKeyName(keys[action]);
    }
    private string FormatKeyName(KeyCode kc)
    {
        var s = kc.ToString();
        if (s.StartsWith("Alpha")) return s.Replace("Alpha", "");
        if (s.StartsWith("Mouse")) { if (s == "Mouse0") return "LMB"; if (s == "Mouse1") return "RMB"; return s; }
        if (s.StartsWith("JoystickButton")) return "Joy" + s.Replace("JoystickButton", "");
        return s;
    }
    private void PlayClickSound() { if (buttonClickSound != null && SoundFXManager.Instance != null) SoundFXManager.Instance.PlaySoundFXClip(buttonClickSound, transform, 1f); }
}