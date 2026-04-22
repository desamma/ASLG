using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ═══════════════════════════════════════════════════════════════════════════
// MaterialRowUI.cs
// Một hàng nguyên liệu trong info panel (ví dụ: Iron Bar   5/3 ✓)
// ═══════════════════════════════════════════════════════════════════════════
public class MaterialRowUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI quantityText;   // "5/3" hoặc "0/3"
    public Image checkIcon;                // icon tích xanh nếu đủ

    public Color haveEnoughColor  = new Color(0.50f, 0.75f, 0.25f);
    public Color notEnoughColor   = new Color(0.87f, 0.38f, 0.38f);

    /// <summary>
    /// Thiết lập hiển thị nguyên liệu (icon + tên + số lượng + check mark).
    /// </summary>
    public void Setup(ItemData item, int required, int currentHave)
    {
        // ✓ Validation: Kiểm tra item đầu vào
        if (item == null)
        {
            Debug.LogWarning("[MaterialRowUI] Item is null!");
            return;
        }

        // ✓ Validation: Kiểm tra required và currentHave hợp lệ
        if (required <= 0)
        {
            Debug.LogWarning($"[MaterialRowUI] Invalid required amount: {required} for {item.itemName}");
            required = 1;
        }

        if (currentHave < 0)
        {
            Debug.LogWarning($"[MaterialRowUI] Invalid currentHave amount: {currentHave} for {item.itemName}");
            currentHave = 0;
        }

        // ✓ Kiểm tra xem có đủ nguyên liệu không
        bool enough = currentHave >= required;

        // ✓ Hiển thị icon (với null check)
        if (itemIcon)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = (item.icon != null);
        }
        else
        {
            Debug.LogWarning("[MaterialRowUI] itemIcon is not assigned!");
        }

        // ✓ Hiển thị tên item
        if (itemNameText)
            itemNameText.text = item.itemName;
        else
            Debug.LogWarning("[MaterialRowUI] itemNameText is not assigned!");

        // ✓ Hiển thị số lượng với color feedback
        if (quantityText)
        {
            quantityText.text = $"{currentHave}/{required}";
            quantityText.color = enough ? haveEnoughColor : notEnoughColor;
        }
        else
        {
            Debug.LogWarning("[MaterialRowUI] quantityText is not assigned!");
        }

        // ✓ Hiển thị check mark nếu đủ
        if (checkIcon)
            checkIcon.enabled = enough;
        else
            Debug.LogWarning("[MaterialRowUI] checkIcon is not assigned!");
    }
}
