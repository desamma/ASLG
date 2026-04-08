using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// ItemSlot - Khe chứa item trong grid inventory.
/// Tint color theo rarity + animation hover smooth.
/// </summary>
public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI txt_qty;

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationSpeed = 10f;

    private ItemData _item;
    private Action _onHovered;
    private bool _hasItem;
    private Vector3 _targetScale = Vector3.one;

    // Màu tint rarity (đậm để thấy rõ)
    static readonly Color TINT_EMPTY = new Color(1.00f, 1.00f, 1.00f, 1f);
    static readonly Color TINT_HOVER = new Color(1f, 0.85f, 0.5f, 1f);
    static readonly Color COL_COMMON = new Color(0.85f, 0.85f, 0.85f, 1f);
    static readonly Color COL_UNCOMMON = new Color(0.20f, 0.80f, 0.20f, 1f);
    static readonly Color COL_RARE = new Color(0.20f, 0.60f, 1.00f, 1f);
    static readonly Color COL_EPIC = new Color(0.80f, 0.20f, 0.95f, 1f);
    static readonly Color COL_LEGEND = new Color(1.00f, 0.70f, 0.00f, 1f);

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale, _targetScale, Time.deltaTime * animationSpeed);
    }

    public void SetItem(ItemData item, Action onHoveredCallback)
    {
        if (item == null) { SetEmpty(); return; }

        _item = item;
        _onHovered = onHoveredCallback;
        _hasItem = true;
        _targetScale = Vector3.one;

        if (slotBackground)
            slotBackground.color = GetRarityTint(item.rarity);

        if (icon)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
            icon.preserveAspect = true;
        }

        bool showQty = item.isStackable && item.currentStack > 1;
        if (txt_qty)
        {
            txt_qty.text = showQty ? item.currentStack.ToString() : "";
            txt_qty.enabled = showQty;
        }
    }

    public void SetEmpty()
    {
        _item = null;
        _onHovered = null;
        _hasItem = false;
        _targetScale = Vector3.one;

        if (slotBackground) slotBackground.color = TINT_EMPTY;
        if (icon) { icon.sprite = null; icon.enabled = false; }
        if (txt_qty) { txt_qty.text = ""; txt_qty.enabled = false; }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hasItem) return;

        if (slotBackground) slotBackground.color = TINT_HOVER;
        _targetScale = Vector3.one * hoverScale;
        _onHovered?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotBackground)
            slotBackground.color = _hasItem ? GetRarityTint(_item.rarity) : TINT_EMPTY;
        _targetScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_hasItem) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _onHovered?.Invoke();
            InventoryUI.instance?.DropSelected();
        }
    }

    private Color GetRarityTint(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => COL_COMMON,
        ItemRarity.Uncommon => COL_UNCOMMON,
        ItemRarity.Rare => COL_RARE,
        ItemRarity.Epic => COL_EPIC,
        ItemRarity.Legendary => COL_LEGEND,
        _ => TINT_EMPTY
    };
}