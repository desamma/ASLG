using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;


[System.Serializable]
public class RecipeIngredient
{
    public string itemKey;
    public int amount;

    public ItemDefinition GetDefinition() => ItemDatabase.GetItem(itemKey);
}


[System.Serializable]
public class RecipeDefinition
{
    public string resultItemKey;
    public int resultAmount = 1;
    public int requiredLevel = 0;
    public bool requiresUnlock = false;
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();


    public ItemDefinition GetResultDefinition() => ItemDatabase.GetItem(resultItemKey);

    public string DisplayName
    {
        get
        {
            var def = GetResultDefinition();
            return def != null ? def.name : resultItemKey;
        }
    }

    public bool CanCraft(int multiplier = 1)
    {
        if (InventoryManager.instance == null) return false;
        foreach (var ing in ingredients)
        {
            if (string.IsNullOrEmpty(ing.itemKey)) continue;
            if (InventoryManager.instance.GetItemCount(ing.itemKey) < ing.amount * multiplier)
                return false;
        }
        return true;
    }
}

/// <summary>
/// RecipeDatabase – load và tra cứu recipe từ Resources/Data/recipes.json.
/// Dùng pattern giống ItemDatabase.
/// Đặt file tại: Assets/Resources/Data/recipes.json
/// </summary>
public static class RecipeDatabase
{
    public static Dictionary<string, RecipeDefinition> Recipes { get; private set; }

    public static void Initialize()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/recipes");
        if (jsonFile != null)
        {
            Recipes = JsonConvert.DeserializeObject<Dictionary<string, RecipeDefinition>>(jsonFile.text);
            Debug.Log($"[RecipeDatabase] Đã nạp {Recipes.Count} recipes từ JSON.");
        }
        else
        {
            Debug.LogError("[RecipeDatabase] Không tìm thấy file! Đảm bảo file nằm ở: Assets/Resources/Data/recipes.json");
            Recipes = new Dictionary<string, RecipeDefinition>();
        }
    }

    public static RecipeDefinition GetRecipe(string resultItemKey)
    {
        if (Recipes != null && Recipes.TryGetValue(resultItemKey, out var recipe))
            return recipe;
        return null;
    }

    public static List<RecipeDefinition> GetAll()
    {
        if (Recipes == null) return new List<RecipeDefinition>();
        return new List<RecipeDefinition>(Recipes.Values);
    }

    public static List<RecipeDefinition> GetAvailable(int playerLevel = 0, List<string> unlockedKeys = null)
    {
        var result = new List<RecipeDefinition>();
        if (Recipes == null) return result;

        foreach (var recipe in Recipes.Values)
        {
            if (recipe.GetResultDefinition() == null) continue;
            if (recipe.requiredLevel > playerLevel) continue;
            if (recipe.requiresUnlock)
            {
                if (unlockedKeys == null || !unlockedKeys.Contains(recipe.resultItemKey)) continue;
            }
            result.Add(recipe);
        }
        return result;
    }
}
