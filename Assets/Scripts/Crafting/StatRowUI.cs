using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ═══════════════════════════════════════════════════════════════════════════
// StatRowUI.cs
// Một hàng stat trong info panel bên phải (ví dụ: "Damage  +15")
// ═══════════════════════════════════════════════════════════════════════════
public class StatRowUI : MonoBehaviour
{
    public TextMeshProUGUI statNameText;
    public TextMeshProUGUI statValueText;

    public Color positiveColor = new Color(0.50f, 0.75f, 0.25f);
    public Color negativeColor = new Color(0.87f, 0.38f, 0.38f);
    public Color neutralColor  = new Color(0.75f, 0.63f, 0.44f);

    public void Setup(string statName, float value)
    {
        if (statNameText) statNameText.text = statName.ToUpper();

        if (statValueText)
        {
            string sign = value >= 0 ? "+" : "";
            statValueText.text  = $"{sign}{value}";
            statValueText.color = value > 0 ? positiveColor : (value < 0 ? negativeColor : neutralColor);
        }
    }
}
