using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Một ingredient slot trong recipe: item + số lượng cần.
/// </summary>
[System.Serializable]
public class CraftingIngredient
{
    [Tooltip("ItemData cần dùng để craft")]
    public ItemData item;

    [Min(1), Tooltip("Số lượng cần")]
    public int amount = 1;
}

/// <summary>
/// CraftingRecipe – ScriptableObject mô tả 1 công thức craft.
/// Tạo recipe: Assets > Create > Crafting > Recipe
/// </summary>
[CreateAssetMenu(menuName = "Crafting/Recipe", fileName = "NewRecipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Result")]
    [Tooltip("Item tạo ra sau khi craft")]
    public ItemData resultItem;

    [Min(1), Tooltip("Số lượng item tạo ra mỗi lần craft")]
    public int resultAmount = 1;

    [Header("Ingredients")]
    [Tooltip("Danh sách nguyên liệu cần (tối đa 9 slot cho grid 3x3)")]
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

    [Header("Unlock")]
    [Tooltip("Cần unlock trước khi hiển thị? Nếu false = luôn thấy")]
    public bool requiresUnlock = false;

    [Tooltip("Level tối thiểu để có thể craft. 0 = không yêu cầu")]
    [Min(0)] public int requiredLevel = 0;

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Kiểm tra xem inventory có đủ nguyên liệu để craft (multiplier lần) không.
    /// </summary>
    public bool CanCraft(PlayerInventory inventory, int multiplier = 1)
    {
        if (inventory == null) return false;
        foreach (var ing in ingredients)
        {
            if (ing.item == null) continue;
            int needed = ing.amount * multiplier;
            if (inventory.GetItemCount(ing.item) < needed)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Tên hiển thị fallback nếu resultItem null.
    /// </summary>
    public string DisplayName => resultItem != null ? resultItem.itemName : name;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ingredients != null && ingredients.Count > 3)
        {
            UnityEngine.Debug.LogWarning(
                $"[{name}] Có {ingredients.Count} ingredients > 3 slot! Chỉ 3 đầu hiển thị.", this);
        }
    }
#endif
}
