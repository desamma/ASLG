using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CraftingManager – xử lý toàn bộ logic craft.
/// Attach vào một GameObject trong Crafting Scene.
/// Không phụ thuộc UI, dễ test độc lập.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [Header("Recipe Database")]
    [Tooltip("Kéo tất cả CraftingRecipe ScriptableObjects vào đây")]
    public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

    [Header("Unlock")]
    [Tooltip("Danh sách recipe đã được unlock (runtime)")]
    [SerializeField] private List<CraftingRecipe> _unlockedRecipes = new List<CraftingRecipe>();

    // Sự kiện broadcast khi craft thành công
    public event System.Action<CraftingRecipe, int> OnCraftSuccess;
    // Sự kiện broadcast khi craft thất bại (thiếu nguyên liệu / full inventory)
    public event System.Action<CraftingRecipe, string> OnCraftFailed;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về danh sách recipe hiển thị cho player (đã unlock + không yêu cầu unlock).
    /// </summary>
    public List<CraftingRecipe> GetAvailableRecipes(int playerLevel = 0)
    {
        return allRecipes.Where(r =>
        {
            if (r.resultItem == null) return false;
            if (r.requiredLevel > playerLevel) return false;
            if (r.requiresUnlock && !_unlockedRecipes.Contains(r)) return false;
            return true;
        }).ToList();
    }

    /// <summary>
    /// Thử craft recipe với số lần nhân (multiplier).
    /// Trả về true nếu craft thành công.
    /// Gọi OnCraftSuccess hoặc OnCraftFailed event.
    /// </summary>
    public bool TryCraft(CraftingRecipe recipe, PlayerInventory inventory, int multiplier = 1)
    {
        // ✓ Validation: Kiểm tra tham số đầu vào
        if (recipe == null)
        {
            OnCraftFailed?.Invoke(null, "Recipe is null.");
            return false;
        }

        if (inventory == null)
        {
            OnCraftFailed?.Invoke(recipe, "Inventory is null.");
            return false;
        }

        if (multiplier <= 0)
        {
            OnCraftFailed?.Invoke(recipe, "Multiplier must be greater than 0.");
            return false;
        }

        if (recipe.resultItem == null)
        {
            OnCraftFailed?.Invoke(recipe, "Recipe result item is null.");
            return false;
        }

        // ✓ Validation: Kiểm tra nguyên liệu đủ
        if (!recipe.CanCraft(inventory, multiplier))
        {
            OnCraftFailed?.Invoke(recipe, "Not enough materials.");
            return false;
        }

        // ✓ Validation: Kiểm tra khoảng trống inventory
        // Lưu ý: AddItem có thể fail nếu inventory full hoặc không stack được
        int totalResult = recipe.resultAmount * multiplier;
        if (!CanAddToInventory(inventory, recipe.resultItem, totalResult))
        {
            OnCraftFailed?.Invoke(recipe, "Result item won't fit in inventory.");
            return false;
        }

        // ✓ Step 1: Tiêu thụ nguyên liệu
        // Nếu CanCraft đã check, step này should always succeed
        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;

            if (!inventory.TryConsume(ing.item, ing.amount * multiplier))
            {
                // Không nên xảy ra nếu CanCraft đúng
                Debug.LogError($"[CraftingManager] Unexpected: TryConsume failed for {ing.item.itemName}");
                OnCraftFailed?.Invoke(recipe, $"Failed to consume {ing.item.itemName}.");
                return false;
            }
        }

        // ✓ Step 2: Thêm kết quả vào inventory
        bool added = inventory.AddItem(recipe.resultItem, totalResult);

        if (!added)
        {
            // ❌ Hoàn nguyên liệu nếu thêm result fail
            Debug.LogError($"[CraftingManager] AddItem failed. Rolling back ingredients.");
            RollbackIngredients(recipe, inventory, multiplier);

            OnCraftFailed?.Invoke(recipe, "Could not add result item to inventory.");
            return false;
        }

        // ✓ Craft thành công!
        OnCraftSuccess?.Invoke(recipe, multiplier);
        return true;
    }

    /// <summary>
    /// Kiểm tra xem có thể thêm item vào inventory không (dựa trên space available).
    /// </summary>
    private bool CanAddToInventory(PlayerInventory inventory, ItemData item, int amount)
    {
        if (inventory == null || item == null || amount <= 0)
            return false;

        // Nếu stackable: kiểm tra space hiện tại có đủ không
        if (item.isStackable)
        {
            int currentCount = inventory.GetItemCount(item);
            int canStack = (item.maxStack - currentCount) * 100; // Estimate: có thể cải thiện
            return currentCount + amount <= item.maxStack * 100; // Rough check
        }

        // Nếu non-stackable: cần số slot = amount
        return (inventory.Entries.Count + amount) <= inventory.maxSlots;
    }

    /// <summary>
    /// Hoàn nguyên liệu khi craft thất bại ở step AddItem.
    /// </summary>
    private void RollbackIngredients(CraftingRecipe recipe, PlayerInventory inventory, int multiplier)
    {
        if (recipe == null || inventory == null) return;

        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            inventory.AddItem(ing.item, ing.amount * multiplier);
        }
    }

    /// <summary>Unlock một recipe mới.</summary>
    public void UnlockRecipe(CraftingRecipe recipe)
    {
        if (recipe != null && !_unlockedRecipes.Contains(recipe))
            _unlockedRecipes.Add(recipe);
    }

    /// <summary>Kiểm tra recipe đã được unlock chưa.</summary>
    public bool IsUnlocked(CraftingRecipe recipe)
        => !recipe.requiresUnlock || _unlockedRecipes.Contains(recipe);
}
