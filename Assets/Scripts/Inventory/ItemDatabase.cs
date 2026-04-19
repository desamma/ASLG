using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class ItemDefinition
{
    public string name;
    public string rarity;
    public string itemType;
    public string armorSlot;
    public string description;
    public bool isStackable;
    public int maxStack;
    public string iconPath;
    public Dictionary<string, float> statBonuses = new Dictionary<string, float>();

    // Hàm load ảnh an toàn
    public Sprite GetIcon()
    {
        Sprite s = null;
        
        // 1. Thử load ảnh thật
        if (!string.IsNullOrEmpty(iconPath)) s = Resources.Load<Sprite>(iconPath);
        
        // 2. Nếu lỗi, load ảnh dấu "?"
        if (s == null) 
        {
            s = Resources.Load<Sprite>("Icons/missing_icon");
            if (s == null) Debug.LogWarning($"[ItemDatabase] Lỗi: Không tìm thấy ảnh '{iconPath}' và không có sẵn ảnh thay thế 'missing_icon'.");
        }
        
        return s;
    }
}

public static class ItemDatabase
{
    public static Dictionary<string, ItemDefinition> Items { get; private set; }

    public static void Initialize()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/items");
        if (jsonFile != null)
        {
            Items = JsonConvert.DeserializeObject<Dictionary<string, ItemDefinition>>(jsonFile.text);
            Debug.Log($"[ItemDatabase] 🚀 Đã nạp thành công {Items.Count} vật phẩm từ JSON.");
        }
        else
        {
            Debug.LogError("[ItemDatabase] ❌ KHÔNG THỂ NẠP DATA! Hãy đảm bảo file nằm ở: Assets/Resources/Data/items.json");
        }
    }

    public static ItemDefinition GetItem(string itemID)
    {
        if (Items != null && Items.TryGetValue(itemID, out ItemDefinition def)) return def;
        return null;
    }
}