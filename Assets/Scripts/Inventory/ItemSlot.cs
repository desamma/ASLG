using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// ItemSlot - KHÔNG cần sprite asset nào.
/// Dùng Image màu thuần + TextMeshPro emoji làm icon tạm.
///
/// Hierarchy của prefab:
///   ItemSlot  [Image + ItemSlot.cs]
///   ├── Background  [Image]
///   ├── RarityGlow  [Image]
///   ├── TXT_Icon    [TextMeshPro]  ← emoji tạm
///   └── TXT_Qty     [TextMeshPro]  ← góc phải dưới
/// </summary>
public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Kéo thả vào đây trong Inspector")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private TextMeshProUGUI txt_icon;
    [SerializeField] private TextMeshProUGUI txt_qty;

    private ItemData _item;
    private Action _onHovered;
    private bool _hasItem;

    static readonly Color32 BG_EMPTY = new Color32(20, 18, 14, 255);
    static readonly Color32 BG_HOVER = new Color32(40, 34, 24, 255);
    static readonly Color32 COL_EMPTY = new Color32(50, 45, 38, 100);
    static readonly Color32 COL_COMMON = new Color32(160, 160, 160, 200);
    static readonly Color32 COL_UNCOMMON = new Color32(80, 200, 80, 220);
    static readonly Color32 COL_RARE = new Color32(80, 130, 255, 220);
    static readonly Color32 COL_EPIC = new Color32(160, 80, 230, 220);
    static readonly Color32 COL_LEGEND = new Color32(255, 160, 30, 240);

    public void SetItem(ItemData item, Action onHoveredCallback)
    {
        _item = item; _onHovered = onHoveredCallback; _hasItem = true;
        if (slotBackground) slotBackground.color = BG_EMPTY;
        if (rarityBorder) rarityBorder.color = RarityColor(item.rarity);
        if (txt_icon) { txt_icon.text = item.emojiIcon; txt_icon.enabled = true; }
        bool showQty = item.isStackable && item.currentStack > 1;
        if (txt_qty) { txt_qty.text = showQty ? $"x{item.currentStack}" : ""; txt_qty.enabled = showQty; }
    }

    public void SetEmpty()
    {
        _item = null; _onHovered = null; _hasItem = false;
        if (slotBackground) slotBackground.color = BG_EMPTY;
        if (rarityBorder) rarityBorder.color = COL_EMPTY;
        if (txt_icon) { txt_icon.text = ""; txt_icon.enabled = false; }
        if (txt_qty) { txt_qty.text = ""; txt_qty.enabled = false; }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (!_hasItem) return;
        if (slotBackground) slotBackground.color = BG_HOVER;
        _onHovered?.Invoke();
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (slotBackground) slotBackground.color = BG_EMPTY;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!_hasItem) return;
        if (e.button == PointerEventData.InputButton.Right)
        {
            _onHovered?.Invoke();
            InventoryUI.instance?.DropSelected();
        }
    }

    static Color32 RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common => COL_COMMON,
        ItemRarity.Uncommon => COL_UNCOMMON,
        ItemRarity.Rare => COL_RARE,
        ItemRarity.Epic => COL_EPIC,
        ItemRarity.Legendary => COL_LEGEND,
        _ => COL_EMPTY
    };
}