using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private CanvasGroup statCanvas;

    [Header("Stat Slots")]
    [SerializeField] private TextMeshProUGUI maxHealthText;
    [SerializeField] private TextMeshProUGUI maxManaText;
    [SerializeField] private TextMeshProUGUI maxStaminaText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI defenceText;
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI upgradePointText;

    private bool statOpen = false;

    private void Start()
    {
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnStatsChangedEvent += RefreshUI;
        }

        SetCanvasVisible(false);
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnStatsChangedEvent -= RefreshUI;
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("ToggleStats"))
        {
            statOpen = !statOpen;
            if (statOpen) RefreshUI();
            SetCanvasVisible(statOpen);
            Time.timeScale = statOpen ? 0f : 1f;
        }
    }

    private void SetCanvasVisible(bool visible)
    {
        statCanvas.alpha = visible ? 1f : 0f;
        statCanvas.blocksRaycasts = visible;
        statCanvas.interactable = visible;
    }

    public void RefreshUI()
    {
        var s = StatsManager.instance;
        if (s == null) return;

        SetText(maxHealthText, $"Max Health: {s.maxHealth:0}");
        SetText(maxManaText, $"Max Mana: {s.maxMana:0}");
        SetText(maxStaminaText, $"Max Stamina: {s.maxStamina:0}");
        SetText(damageText, $"Damage: {s.damage}");
        SetText(defenceText, $"Defence: {s.defence:0}");
        SetText(moveSpeedText, $"Move Speed: {s.moveSpeed:0.###}");
        SetText(upgradePointText, $"Upgrade Point: {s.upgradePoints:0.#}");
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }
}
