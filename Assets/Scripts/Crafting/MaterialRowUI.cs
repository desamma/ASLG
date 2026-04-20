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

    public void Setup(ItemData item, int required, int currentHave)
    {
        if (item == null) return;

        bool enough = currentHave >= required;

        if (itemIcon)    { itemIcon.sprite = item.icon; itemIcon.enabled = item.icon != null; }
        if (itemNameText) itemNameText.text = item.itemName;

        if (quantityText)
        {
            quantityText.text  = $"{currentHave}/{required}";
            quantityText.color = enough ? haveEnoughColor : notEnoughColor;
        }

        if (checkIcon) checkIcon.enabled = enough;
    }
}
