using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private TextMeshProUGUI goldText;

    private void Start()
    {
        UpdateSliderUI();

        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnStatsChangedEvent += UpdateSliderUI;
        }
    }

    private void OnDestroy()
    {
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnStatsChangedEvent -= UpdateSliderUI;
        }
    }

    private void UpdateSliderUI()
    {
        if (StatsManager.instance == null) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = StatsManager.instance.maxHealth;
            healthSlider.value = StatsManager.instance.currentHealth;
        }

        if (manaSlider != null)
        {
            manaSlider.maxValue = StatsManager.instance.maxMana;
            manaSlider.value = StatsManager.instance.currentMana;
        }

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = StatsManager.instance.maxStamina;
            staminaSlider.value = StatsManager.instance.currentStamina;
        }

        if (goldText != null)
        {
            goldText.text = StatsManager.instance.gold.ToString();
        }
    }
}