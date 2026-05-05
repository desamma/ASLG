using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatRowUI : MonoBehaviour
{
    public TextMeshProUGUI statNameText;
    public TextMeshProUGUI statValueText;

    public Color positiveColor = new Color(0.50f, 0.75f, 0.25f);
    public Color negativeColor = new Color(0.87f, 0.38f, 0.38f);
    public Color neutralColor  = new Color(0.75f, 0.63f, 0.44f);

    public void Setup(string statName, float value)
    {
        if (string.IsNullOrWhiteSpace(statName))
        {
            Debug.LogWarning("[StatRowUI] statName is null or empty!");
            statName = "UNKNOWN";
        }

        if (statNameText)
            statNameText.text = statName.ToUpper();
        else
            Debug.LogWarning("[StatRowUI] statNameText is not assigned!");

        if (statValueText)
        {
            string sign = value > 0 ? "+" : "";
            statValueText.text = $"{sign}{value:F1}";

            if (value > 0)
                statValueText.color = positiveColor;
            else if (value < 0)
                statValueText.color = negativeColor;
            else
                statValueText.color = neutralColor;
        }
        else
        {
            Debug.LogWarning("[StatRowUI] statValueText is not assigned!");
        }
    }
}
