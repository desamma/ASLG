using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI txt_qty;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationSpeed = 10f;

    public ItemStack CurrentStack { get; private set; }
    private ItemDefinition _itemDef;
    private Action _onHovered;
    private bool _hasItem;
    private Vector3 _targetScale = Vector3.one;

    static readonly Color TINT_EMPTY = new Color(1.00f, 1.00f, 1.00f, 1f);
    static readonly Color TINT_HOVER = new Color(1f, 0.85f, 0.5f, 1f);

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * animationSpeed);
    }

    public void SetItem(ItemStack stack, Action onHoveredCallback)
    {
        if (stack == null || string.IsNullOrEmpty(stack.itemID)) { SetEmpty(); return; }

        _itemDef = ItemDatabase.GetItem(stack.itemID);
        if (_itemDef == null) { SetEmpty(); return; }

        CurrentStack = stack;
        _onHovered = onHoveredCallback;
        _hasItem = true;
        _targetScale = Vector3.one;

        if (slotBackground) slotBackground.color = GetRarityTint(_itemDef.rarity);
        
        if (icon)
        {
            Sprite s = _itemDef.GetIcon();
            icon.sprite = s;
            icon.enabled = (s != null);
            icon.preserveAspect = true;
        }

        bool showQty = _itemDef.isStackable && CurrentStack.amount > 1;
        if (txt_qty)
        {
            txt_qty.text = showQty ? CurrentStack.amount.ToString() : "";
            txt_qty.enabled = showQty;
        }
    }

    public void SetEmpty()
    {
        CurrentStack = null;
        _itemDef = null;
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
        if (slotBackground) slotBackground.color = _hasItem ? GetRarityTint(_itemDef.rarity) : TINT_EMPTY;
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

    private Color GetRarityTint(string rarityStr)
    {
        return rarityStr.ToLower() switch
        {
            "common" => new Color(0.85f, 0.85f, 0.85f, 1f),
            "uncommon" => new Color(0.20f, 0.80f, 0.20f, 1f),
            "rare" => new Color(0.20f, 0.60f, 1.00f, 1f),
            "epic" => new Color(0.80f, 0.20f, 0.95f, 1f),
            "legendary" => new Color(1.00f, 0.70f, 0.00f, 1f),
            _ => TINT_EMPTY
        };
    }
}