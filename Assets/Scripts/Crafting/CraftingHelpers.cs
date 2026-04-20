// ═══════════════════════════════════════════════════════════════════════════
// RecipeListItem.cs
// Component gắn vào prefab mỗi dòng recipe trong danh sách bên trái.
// ═══════════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RecipeListItem : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI subText;        // "3x Iron Bar, 1x Wood"
    public Image background;
    public Image selectionHighlight;       // có thể là outline hoặc bg overlay

    [Header("Colors")]
    public Color bgNormal    = new Color(0.35f, 0.25f, 0.18f);
    public Color bgSelected  = new Color(0.29f, 0.34f, 0.12f);
    public Color bgCantCraft = new Color(0.25f, 0.18f, 0.13f);

    private Action _onClick;
    private bool _canCraft;

    public void Setup(CraftingRecipe recipe, bool canCraft, bool isSelected, Action onClick)
    {
        _canCraft = canCraft;
        _onClick  = onClick;

        if (recipe.resultItem == null) return;
        var item = recipe.resultItem;

        if (iconImage) { iconImage.sprite = item.icon; iconImage.enabled = item.icon != null; }
        if (nameText)  nameText.text = item.itemName;

        if (rarityText)
        {
            rarityText.text = item.rarity.ToString();
            rarityText.color = GetRarityColor(item.rarity);
        }

        if (subText)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var ing in recipe.ingredients)
                if (ing.item != null) parts.Add($"{ing.amount}x {ing.item.itemName}");
            subText.text = string.Join(", ", parts);
        }

        if (background)
            background.color = isSelected ? bgSelected : (canCraft ? bgNormal : bgCantCraft);

        if (selectionHighlight)
            selectionHighlight.enabled = isSelected;

        // Dim nếu không đủ nguyên liệu
        GetComponent<CanvasGroup>()?.SetAlpha(canCraft ? 1f : 0.6f);
    }

    public void OnClicked()
    {
        _onClick?.Invoke();
    }

    private Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common    => new Color(0.67f, 0.67f, 0.67f),
        ItemRarity.Uncommon  => new Color(0.20f, 0.80f, 0.20f),
        ItemRarity.Rare      => new Color(0.20f, 0.60f, 1.00f),
        ItemRarity.Epic      => new Color(0.80f, 0.20f, 0.95f),
        ItemRarity.Legendary => new Color(1.00f, 0.70f, 0.00f),
        _ => Color.white
    };
}

// ───────────────────────────────────────────────────────────────────────────
// Tiny extension để tránh null check mỗi lần dùng CanvasGroup
// ───────────────────────────────────────────────────────────────────────────
public static class CanvasGroupExtensions
{
    public static void SetAlpha(this CanvasGroup cg, float alpha)
    {
        if (cg == null) return;
        cg.alpha = alpha;
    }
}



