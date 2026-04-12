using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterCreation : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Dropdown classDropdown;
    [SerializeField] private Button startButton;

    [Header("Class Preview")]
    [SerializeField] private TMP_Text classNameText;
    [SerializeField] private TMP_Text classDescriptionText;
    [SerializeField] private Image classIconImage;

    [Header("Scene")]
    [SerializeField] private int gameSceneIndex = 2;

    private PlayerClass selectedClass = PlayerClass.Knight;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildDropdown();
        RefreshPreview();

        classDropdown.onValueChanged.AddListener(OnClassDropdownChanged);
        nameInput.onValueChanged.AddListener(_ => RefreshStartButton());
        startButton.onClick.AddListener(OnStartClicked);

        RefreshStartButton();
    }

    private void OnDestroy()
    {
        classDropdown.onValueChanged.RemoveListener(OnClassDropdownChanged);
        startButton.onClick.RemoveListener(OnStartClicked);
    }

    // ── Dropdown ──────────────────────────────────────────────────────────────

    private void BuildDropdown()
    {
        classDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        foreach (PlayerClass pc in System.Enum.GetValues(typeof(PlayerClass)))
        {
            var data = ClassManager.Instance.GetDataFor(pc);
            string label = !string.IsNullOrEmpty(data?.displayName) ? data.displayName : pc.ToString();
            options.Add(label);
        }

        classDropdown.AddOptions(options);
        classDropdown.SetValueWithoutNotify((int)selectedClass);
        ClassManager.Instance.SelectClass(selectedClass);
        RefreshPreview();
    }

    private void OnClassDropdownChanged(int index)
    {
        selectedClass = (PlayerClass)index;
        ClassManager.Instance.SelectClass(selectedClass);
        RefreshPreview();
    }

    // ── Preview panel ─────────────────────────────────────────────────────────

    private void RefreshPreview()
    {
        var data = ClassManager.Instance.CurrentClassData;
        if (data == null) return;

        if (classNameText != null) classNameText.text = data.displayName;
        if (classDescriptionText != null) classDescriptionText.text = data.description;
        //if (classIconImage != null)
        //{
        //    // icon is optional — hide image if none assigned
        //    bool hasIcon = data.icon != null;
        //    classIconImage.gameObject.SetActive(hasIcon);
        //    if (hasIcon) classIconImage.sprite = data.icon;
        //}
    }

    // ── Start button ──────────────────────────────────────────────────────────

    private void RefreshStartButton()
    {
        if (startButton != null)
            startButton.interactable = !string.IsNullOrWhiteSpace(nameInput.text);
    }

    private void OnStartClicked()
    {
        string playerName = nameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(playerName)) return;

        StatsManager.instance.playerName = playerName;

        SceneManager.LoadScene(gameSceneIndex);
    }
}