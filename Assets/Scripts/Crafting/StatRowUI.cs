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

    /// <summary>
    /// Thiết lập hiển thị stat (tên + giá trị với color feedback).
    /// </summary>
    public void Setup(string statName, float value)
    {
        // ✓ Validation: Kiểm tra statName có hợp lệ
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
            // ✓ Format giá trị với dấu + cho số dương
            string sign = value > 0 ? "+" : "";
            statValueText.text = $"{sign}{value:F1}"; // F1 để format số thập phân

            // ✓ Color feedback: Dương = Xanh, Âm = Đỏ, Zero = Trung tính
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
