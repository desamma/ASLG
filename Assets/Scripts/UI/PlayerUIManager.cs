using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Slider staminaSlider;

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
        if (healthSlider != null && StatsManager.instance != null)
        {
            healthSlider.maxValue = StatsManager.instance.maxHealth;
            healthSlider.value = StatsManager.instance.currentHealth;

            manaSlider.maxValue = StatsManager.instance.maxMana;
            manaSlider.value = StatsManager.instance.currentMana;

            staminaSlider.maxValue = StatsManager.instance.maxStamina;
            staminaSlider.value = StatsManager.instance.currentStamina;
        }
    }
}