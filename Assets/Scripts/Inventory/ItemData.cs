using UnityEngine;
using System.Collections.Generic;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

/// <summary>
/// Loại item – dùng để kiểm soát logic equip / use.
/// </summary>
public enum ItemType
{
    Weapon,       // Tối đa 1
    Armor,        // Dựa vào ArmorSlot (Head/Body/Leg/Shoes), mỗi slot tối đa 1
    Accessory,    // Tối đa 2 (nhẫn, dây chuyền …)
    Consumable,   // Dùng 1 lần: trừ stack / xóa item, áp dụng effect ngay
    Misc          // Đồ linh tinh, không equip được
}

/// <summary>
/// Vị trí giáp – chỉ dùng khi ItemType == Armor.
/// </summary>
public enum ArmorSlot { Head, Body, Leg, Shoes }

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

    [Header("Item Type")]
    public ItemType itemType = ItemType.Misc;

    [Tooltip("Chỉ dùng khi itemType = Armor")]
    public ArmorSlot armorSlot = ArmorSlot.Body;

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
    [Tooltip("Equipment: cộng vào stats khi equip.\nConsumable: áp dụng 1 lần khi USE (hồi HP, Mana…)")]
    public List<StatBonus> statBonuses = new List<StatBonus>();

    [Header("World Drop")]
    [Tooltip("Prefab spawn khi drop ra đất")]
    public GameObject worldPrefab;

    // ── Helpers ────────────────────────────────────────────────────────────
    public bool IsEquippable =>
        itemType == ItemType.Weapon ||
        itemType == ItemType.Armor ||
        itemType == ItemType.Accessory;

    public bool IsConsumable => itemType == ItemType.Consumable;

    /// <summary>Convert List‹StatBonus› → Dictionary (backward compat).</summary>
    public Dictionary<string, float> GetStatBonusesDict()
    {
        var dict = new Dictionary<string, float>();
        foreach (var bonus in statBonuses)
            if (!string.IsNullOrEmpty(bonus.statName))
                dict[bonus.statName] = bonus.value;
        return dict;
    }
}

/// <summary>
/// StatBonus - Cặp Key-Value cho stat bonuses / consumable effects.
/// </summary>
[System.Serializable]
public class StatBonus
{
    [Tooltip("Tên stat: damage, maxHealth, currentHealth, currentMana, moveSpeed, etc.")]
    public string statName = "damage";

    [Tooltip("Giá trị cộng thêm (hoặc hồi phục với Consumable)")]
    public float value = 0f;
}