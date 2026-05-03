using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterCreation : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button startButton;

    [Header("Class Buttons")]
    [SerializeField] private Button knightButton;
    [SerializeField] private Button archerButton;
    [SerializeField] private Button rogueButton;
    [SerializeField] private Button summonerButton;

    [Header("Selected Highlight")]
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.7f, 1f);
    [SerializeField] private Color deselectedColor = Color.white;

    [Header("Class Preview")]
    [SerializeField] private TMP_Text classDescriptionText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text movespeedText;
    [SerializeField] private TMP_Text attackCooldownText;
    [SerializeField] private TMP_Text attatckTypeText;

    //[Header("Scene")]
    //[SerializeField] private string gameSceneName = "StarterVillage

    public event System.Action OnStartButtonClickEvent;

    private PlayerClass selectedClass = PlayerClass.Knight;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        nameInput.text = null;
        knightButton.onClick.AddListener(() => SelectClass(PlayerClass.Knight));
        archerButton.onClick.AddListener(() => SelectClass(PlayerClass.Archer));
        rogueButton.onClick.AddListener(() => SelectClass(PlayerClass.Rogue));
        summonerButton.onClick.AddListener(() => SelectClass(PlayerClass.Summoner));

        nameInput.onValueChanged.AddListener(_ => RefreshStartButton());
        startButton.onClick.AddListener(OnStartClicked);

        SelectClass(selectedClass);
        RefreshStartButton();
    }

    private void OnDestroy()
    {
        knightButton.onClick.RemoveAllListeners();
        archerButton.onClick.RemoveAllListeners();
        rogueButton.onClick.RemoveAllListeners();
        summonerButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();
    }

    // ── Class selection ───────────────────────────────────────────────────────

    private void SelectClass(PlayerClass playerClass)
    {
        selectedClass = playerClass;
        ClassManager.Instance.SelectClass(selectedClass);
        RefreshButtonHighlights();
        RefreshPreview();
    }

    private void RefreshButtonHighlights()
    {
        SetButtonHighlight(knightButton, selectedClass == PlayerClass.Knight);
        SetButtonHighlight(archerButton, selectedClass == PlayerClass.Archer);
        SetButtonHighlight(rogueButton, selectedClass == PlayerClass.Rogue);
        SetButtonHighlight(summonerButton, selectedClass == PlayerClass.Summoner);
    }

    private void SetButtonHighlight(Button button, bool isSelected)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = isSelected ? selectedColor : deselectedColor;
    }

    // ── Preview panel ─────────────────────────────────────────────────────────

    private void RefreshPreview()
    {
        var data = ClassManager.Instance.CurrentClassData;
        if (data == null) return;

        classDescriptionText.text = data.description;

        healthText.text = "Health: " + data.maxHealth.ToString();
        defenseText.text = "Defense: " + data.defence.ToString();
        staminaText.text = "Stamina: " + data.maxStamina.ToString();
        damageText.text = "Damage: " + data.damage.ToString();
        movespeedText.text = "Movespeed: " + data.moveSpeed.ToString();
        attackCooldownText.text = "Attack Cooldown: " + data.attackCooldown.ToString();
        attatckTypeText.text = "Attack Type: " + data.attackType.ToString();
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
        //SceneManager.LoadScene("StarterVillage");
        OnStartButtonClickEvent?.Invoke();
    }
}