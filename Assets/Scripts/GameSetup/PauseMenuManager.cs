using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public static float mouseSensitivity = 1f;
    public static System.Action<float> OnMouseSensitivityChanged;
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
    public Slider volumeSlider;
    public Slider bgmSlider;
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

    // === CONSTANTS ===
    private const float BGM_MIN = 0.0001f;
    private const float BGM_MAX = 1f;
    private const float MOUSE_MIN = 0.1f;
    private const float MOUSE_MAX = 5f;
    [SerializeField] private bool isSettingAvailable;

    [SerializeField] private CanvasGroup settingCanvasGroup;
    [SerializeField] private CanvasGroup pauseSettingCanvasGroup;
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;
    [SerializeField] private CanvasGroup soundCanvasGroup;
    [SerializeField] private CanvasGroup mouseCanvasGroup;
    [SerializeField] private CanvasGroup keybindCanvasGroup;

    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseButton;
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        // Load Audio
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolumePref", 1f);
            volumeSlider.minValue = BGM_MIN;
            volumeSlider.maxValue = BGM_MAX;
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
            SetMasterVolume(savedVolume);
        }

        // Load Background Music
        if (bgmSlider != null)
        {
            float savedBGM = PlayerPrefs.GetFloat("BGMVolumePref", 1f);
            // Ép slider đúng range ngay từ đầu
            bgmSlider.minValue = BGM_MIN;
            bgmSlider.maxValue = BGM_MAX;
            bgmSlider.value = Mathf.Clamp(savedBGM, BGM_MIN, BGM_MAX);
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            SetBGMVolume(bgmSlider.value);
        }

        // Load Mouse
        if (mouseSlider != null)
        {
            float savedMouseSense = PlayerPrefs.GetFloat("MouseSensePref", 1f);
            // Ép slider đúng range ngay từ đầu
            mouseSlider.minValue = MOUSE_MIN;
            mouseSlider.maxValue = MOUSE_MAX;
            mouseSlider.value = Mathf.Clamp(savedMouseSense, MOUSE_MIN, MOUSE_MAX);
            mouseSlider.onValueChanged.RemoveAllListeners();
            mouseSlider.onValueChanged.AddListener(SetMouseSensitivity);
            SetMouseSensitivity(mouseSlider.value);
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

        // Auto-hook optional buttons
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
        if (!IsSettingsAvailable())
            return;

        if (Input.GetKeyDown(KeyCode.Escape) && actionToRebind == null)
            HandleEscape();
    }

    private bool IsSettingsAvailable()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Creation" || sceneName == "Start")
            return false;

        return true;
    }

    private void HandleEscape()
    {
        resumeButton.onClick.RemoveAllListeners();
        pauseButton.onClick.RemoveAllListeners();

        if (IsAnySubMenuOpen())
        {
            BackToOptionMenu();
        }
        else if (IsSettingMenuOpen())
        {
            CloseSettings();
        }
        else if (GameIsPaused)
        {
            Resume();
        }
        else
        {
            resumeButton.onClick.AddListener(Resume);
            pauseButton.onClick.AddListener(OpenSettings);
            Pause();
        }
    }

    private bool IsAnySubMenuOpen()
    {
        return IsVisible(soundCanvasGroup) ||
               IsVisible(mouseCanvasGroup) ||
               IsVisible(keybindCanvasGroup);
    }

    private bool IsSettingMenuOpen()
    {
        return IsVisible(pauseSettingCanvasGroup);
    }

    private bool IsVisible(CanvasGroup canvasGroup)
    {
        return canvasGroup != null && canvasGroup.alpha > 0f;
    }

    // ================== MENU CHÍNH ==================
    public void Resume()
    {
        PlayClickSound();
        SetCanvas(settingCanvasGroup, false);
        SetCanvas(pausePanelCanvasGroup, false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }
    void Pause()
    {
        SetCanvas(settingCanvasGroup, true);
        SetCanvas(pausePanelCanvasGroup, true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }
    public void OpenSettings()
    {
        PlayClickSound();
        SetCanvas(soundCanvasGroup, false);
        SetCanvas(keybindCanvasGroup, false);
        SetCanvas(mouseCanvasGroup, false);
        SetCanvas(pausePanelCanvasGroup, false);
        SetCanvas(pauseSettingCanvasGroup, true);
    }
    public void CloseSettings()
    {
        PlayClickSound();
        SetCanvas(pauseSettingCanvasGroup, false);
        SetCanvas(pausePanelCanvasGroup, true);
    }

    public void OpenSoundTab()
    {
        PlayClickSound();
        SetCanvas(pauseSettingCanvasGroup, false);
        SetCanvas(soundCanvasGroup, true);
    }

    public void OpenMouseTab()
    {
        PlayClickSound();
        SetCanvas(pauseSettingCanvasGroup, false);
        SetCanvas(mouseCanvasGroup, true);
    }
    public void OpenKeybindTab()
    {
        PlayClickSound();
        SetCanvas(pauseSettingCanvasGroup, false);
        SetCanvas(keybindCanvasGroup, true);
    }
    public void BackToOptionMenu()
    {
        PlayClickSound();
        SetCanvas(soundCanvasGroup, false);
        SetCanvas(mouseCanvasGroup, false);
        SetCanvas(keybindCanvasGroup, false);
        SetCanvas(pauseSettingCanvasGroup, true);
    }

    public void SetCanvas(CanvasGroup canvasGroup, bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    // ================== ÂM LƯỢNG TỔNG ==================
    public void SetMasterVolume(float sliderValue)
    {
        sliderValue = Mathf.Clamp(sliderValue, BGM_MIN, BGM_MAX);
        float dbValue = Mathf.Log10(sliderValue) * 20f;
        if (mainMixer != null && masterParamExposed)
            mainMixer.SetFloat(masterParamName, dbValue);
        PlayerPrefs.SetFloat("MasterVolumePref", sliderValue);
    }
    public void IncreaseVolume()
    {
        PlayClickSound();
        if (volumeSlider != null) volumeSlider.value = Mathf.Clamp(volumeSlider.value + 0.1f, BGM_MIN, BGM_MAX);
        // slider.onValueChanged sẽ tự gọi SetMasterVolume
    }
    public void DecreaseVolume()
    {
        PlayClickSound();
        if (volumeSlider != null) volumeSlider.value = Mathf.Clamp(volumeSlider.value - 0.1f, BGM_MIN, BGM_MAX);
    }

    // ================== ÂM LƯỢNG NHẠC NỀN ==================
    public void SetBGMVolume(float sliderValue)
    {
        // Clamp về đúng range, tránh log10(0)
        sliderValue = Mathf.Clamp(sliderValue, BGM_MIN, BGM_MAX);
        float dbValue = Mathf.Log10(sliderValue) * 20f;

        if (mainMixer != null)
        {
            if (bgmParamExposed)
                mainMixer.SetFloat(bgmParamName, dbValue);
            else
                Debug.LogWarning("[PauseMenu] BGM param chưa Expose. Vào AudioMixer → chuột phải vào tham số → 'Expose to script'.");
        }
        PlayerPrefs.SetFloat("BGMVolumePref", sliderValue);
    }

    public void IncreaseBGMVolume()
    {
        PlayClickSound();
        if (bgmSlider != null)
        {
            // Chỉ set slider, onValueChanged tự gọi SetBGMVolume — tránh gọi 2 lần
            bgmSlider.value = Mathf.Clamp(bgmSlider.value + 0.1f, BGM_MIN, BGM_MAX);
        }
        else
        {
            // Không có slider thì gọi thẳng
            float current = Mathf.Clamp(PlayerPrefs.GetFloat("BGMVolumePref", 1f) + 0.1f, BGM_MIN, BGM_MAX);
            SetBGMVolume(current);
        }
    }

    public void DecreaseBGMVolume()
    {
        PlayClickSound();
        if (bgmSlider != null)
        {
            bgmSlider.value = Mathf.Clamp(bgmSlider.value - 0.1f, BGM_MIN, BGM_MAX);
        }
        else
        {
            float current = Mathf.Clamp(PlayerPrefs.GetFloat("BGMVolumePref", 1f) - 0.1f, BGM_MIN, BGM_MAX);
            SetBGMVolume(current);
        }
    }

    // ================== ĐỘ NHẠY CHUỘT ==================
    public void SetMouseSensitivity(float sliderValue)
    {
        // Clamp về đúng range 0.1 - 5
        sliderValue = Mathf.Clamp(sliderValue, MOUSE_MIN, MOUSE_MAX);
        mouseSensitivity = sliderValue;
        PlayerPrefs.SetFloat("MouseSensePref", sliderValue);
        OnMouseSensitivityChanged?.Invoke(sliderValue);
        Debug.Log($"[PauseMenu] Mouse sensitivity: {sliderValue:F2}");
    }

    public void IncreaseMouseSense()
    {
        PlayClickSound();
        if (mouseSlider != null)
        {
            // Chỉ set slider, onValueChanged tự gọi SetMouseSensitivity
            mouseSlider.value = Mathf.Clamp(mouseSlider.value + 0.1f, MOUSE_MIN, MOUSE_MAX);
        }
        else
        {
            float current = Mathf.Clamp(PlayerPrefs.GetFloat("MouseSensePref", mouseSensitivity) + 0.1f, MOUSE_MIN, MOUSE_MAX);
            SetMouseSensitivity(current);
        }
    }

    public void DecreaseMouseSense()
    {
        PlayClickSound();
        if (mouseSlider != null)
        {
            mouseSlider.value = Mathf.Clamp(mouseSlider.value - 0.1f, MOUSE_MIN, MOUSE_MAX);
        }
        else
        {
            float current = Mathf.Clamp(PlayerPrefs.GetFloat("MouseSensePref", mouseSensitivity) - 0.1f, MOUSE_MIN, MOUSE_MAX);
            SetMouseSensitivity(current);
        }
    }

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