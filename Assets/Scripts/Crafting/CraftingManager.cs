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
    /// </summary>
    public bool TryCraft(CraftingRecipe recipe, PlayerInventory inventory, int multiplier = 1)
    {
        if (recipe == null || inventory == null)
        {
            OnCraftFailed?.Invoke(recipe, "Invalid recipe or inventory.");
            return false;
        }

        // Kiểm tra nguyên liệu
        if (!recipe.CanCraft(inventory, multiplier))
        {
            OnCraftFailed?.Invoke(recipe, "Not enough materials.");
            return false;
        }

        // Kiểm tra còn chỗ trong inventory
        if (!inventory.HasFreeSlot())
        {
            OnCraftFailed?.Invoke(recipe, "Inventory is full.");
            return false;
        }

        // Trừ nguyên liệu
        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            if (!inventory.TryConsume(ing.item, ing.amount * multiplier))
            {
                // Rollback không cần thiết vì CanCraft đã check trước
                OnCraftFailed?.Invoke(recipe, $"Failed to consume {ing.item.itemName}.");
                return false;
            }
        }

        // Thêm item vào inventory
        int totalResult = recipe.resultAmount * multiplier;
        bool added = inventory.AddItem(recipe.resultItem, totalResult);

        if (!added)
        {
            // Trả lại nguyên liệu nếu add thất bại
            foreach (var ing in recipe.ingredients)
                inventory.AddItem(ing.item, ing.amount * multiplier);

            OnCraftFailed?.Invoke(recipe, "Could not add result item to inventory.");
            return false;
        }

        OnCraftSuccess?.Invoke(recipe, multiplier);
        return true;
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
