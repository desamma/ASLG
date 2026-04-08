using UnityEngine;
using System.Collections.Generic;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

/// <summary>
/// ItemData - ScriptableObject cho item data.
/// Tạo item: Assets > Create > Inventory > Item Data
/// </summary>
[CreateAssetMenu(menuName = "Inventory/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "Item Name";
    public ItemRarity rarity = ItemRarity.Common;
    [TextArea(2, 4)]
    public string description = "Mô tả item ở đây.";

    [Header("Icon")]
    [Tooltip("Để trống nếu chưa có asset - dùng emojiIcon thay thế")]
    public Sprite icon;
    [Tooltip("Emoji hiển thị tạm khi chưa có Sprite")]
    public string emojiIcon = "📦";

    [Header("Stack")]
    public bool isStackable = false;
    [Min(1)] public int maxStack = 1;
    [HideInInspector] public int currentStack = 1;

    [Header("Stat Bonuses")]
    [Tooltip("Thêm stat bonus để cộng vào player")]
    public List<StatBonus> statBonuses = new List<StatBonus>();

    [Header("World Drop")]
    [Tooltip("Prefab spawn khi drop ra đất")]
    public GameObject worldPrefab;

    // ── Helper để tương thích với code cũ ──────────────────────────────
    /// <summary>
    /// Convert List<StatBonus> sang Dictionary cho compatibility.
    /// </summary>
    public Dictionary<string, float> GetStatBonusesDict()
    {
        var dict = new Dictionary<string, float>();
        foreach (var bonus in statBonuses)
        {
            if (!string.IsNullOrEmpty(bonus.statName))
                dict[bonus.statName] = bonus.value;
        }
        return dict;
    }
}

/// <summary>
/// StatBonus - Cặp Key-Value cho stat bonuses.
/// Dễ edit trong Inspector hơn Dictionary.
/// </summary>
[System.Serializable]
public class StatBonus
{
    [Tooltip("Tên stat: damage, maxHealth, moveSpeed, etc.")]
    public string statName = "damage";

    [Tooltip("Giá trị cộng thêm")]
    public float value = 0f;
}