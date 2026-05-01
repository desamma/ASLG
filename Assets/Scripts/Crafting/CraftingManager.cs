using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CraftingManager – xử lý logic craft, dùng RecipeDatabase (JSON).
/// Không còn phụ thuộc CraftingRecipe SO hay PlayerInventory.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [Header("Unlock (runtime)")]
    [Tooltip("itemKey của các recipe đã được unlock")]
    [SerializeField] private List<string> _unlockedRecipeKeys = new List<string>();

    public event System.Action<RecipeDefinition, int> OnCraftSuccess;
    public event System.Action<RecipeDefinition, string> OnCraftFailed;

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────────────

    public List<RecipeDefinition> GetAvailableRecipes(int playerLevel = 0)
        => RecipeDatabase.GetAvailable(playerLevel, _unlockedRecipeKeys);

    /// <summary>Thử craft. Trả về true nếu thành công.</summary>
    public bool TryCraft(RecipeDefinition recipe, int multiplier = 1)
    {
        if (recipe == null)
        {
            OnCraftFailed?.Invoke(null, "Recipe is null.");
            return false;
        }

        if (InventoryManager.instance == null)
        {
            OnCraftFailed?.Invoke(recipe, "InventoryManager chưa sẵn sàng.");
            return false;
        }

        if (multiplier <= 0)
        {
            OnCraftFailed?.Invoke(recipe, "Multiplier phải lớn hơn 0.");
            return false;
        }

        if (recipe.GetResultDefinition() == null)
        {
            OnCraftFailed?.Invoke(recipe, $"Không tìm thấy item '{recipe.resultItemKey}' trong ItemDatabase.");
            return false;
        }

        if (!recipe.CanCraft(multiplier))
        {
            OnCraftFailed?.Invoke(recipe, "Không đủ nguyên liệu.");
            return false;
        }

        // Tiêu thụ nguyên liệu
        foreach (var ing in recipe.ingredients)
        {
            if (string.IsNullOrEmpty(ing.itemKey)) continue;
            if (!InventoryManager.instance.TryConsume(ing.itemKey, ing.amount * multiplier))
            {
                Debug.LogError($"[CraftingManager] TryConsume thất bại cho '{ing.itemKey}' – rollback.");
                RollbackIngredients(recipe, multiplier);
                OnCraftFailed?.Invoke(recipe, $"Không thể tiêu thụ {ing.itemKey}.");
                return false;
            }
        }

        // Thêm kết quả
        InventoryManager.instance.AddItem(recipe.resultItemKey, recipe.resultAmount * multiplier);

        OnCraftSuccess?.Invoke(recipe, multiplier);
        return true;
    }

    public void UnlockRecipe(string resultItemKey)
    {
        if (!string.IsNullOrEmpty(resultItemKey) && !_unlockedRecipeKeys.Contains(resultItemKey))
            _unlockedRecipeKeys.Add(resultItemKey);
    }

    public bool IsUnlocked(RecipeDefinition recipe)
        => !recipe.requiresUnlock || _unlockedRecipeKeys.Contains(recipe.resultItemKey);

    // ── Private ──────────────────────────────────────────────────────────

    private void RollbackIngredients(RecipeDefinition recipe, int multiplier)
    {
        foreach (var ing in recipe.ingredients)
        {
            if (string.IsNullOrEmpty(ing.itemKey)) continue;
            InventoryManager.instance.AddItem(ing.itemKey, ing.amount * multiplier);
        }
    }
}
