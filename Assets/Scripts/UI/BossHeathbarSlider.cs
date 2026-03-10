using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BossHeathUI : MonoBehaviour
{
    [SerializeField] private Slider heathSlider;
    [SerializeField] private TMP_Text bossName;

    public void Initialize(string name, float maxHealth)
    {
        bossName.text = name;
        heathSlider.maxValue = maxHealth;
        heathSlider.value = maxHealth;
    }

    public void Initialize(string name, float maxHealth, Color textColor)
    {
        bossName.text = name;
        bossName.color = textColor;
        heathSlider.maxValue = maxHealth;
        heathSlider.value = maxHealth;
    }

    public void Initialize(string name, float maxHealth, string hexColor)
    {
        bossName.text = name;
        if (ColorUtility.TryParseHtmlString(hexColor, out Color textColor))
        {
            bossName.color = textColor;
        }
        heathSlider.maxValue = maxHealth;
        heathSlider.value = maxHealth;
    }

    public void UpdateHealth(float current)
    {
        heathSlider.value = current;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
