using UnityEngine;
using System.Collections.Generic;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

/// <summary>
/// Tạo item: Assets > Create > Inventory > Item Data
/// Không cần Sprite - dùng emojiIcon làm placeholder.
/// Khi có asset thật thì gán Sprite vào trường icon và script sẽ ưu tiên dùng Sprite.
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
    [Tooltip("Emoji hiển thị tạm khi chưa có Sprite. Ví dụ: ⚔️ 🛡️ 🧪 🗡️ 💍 🪖")]
    public string emojiIcon = "📦";

    [Header("Stack")]
    public bool isStackable = false;
    [Min(1)] public int maxStack = 1;
    [HideInInspector] public int currentStack = 1;

    [Header("Stat Bonuses")]
    [Tooltip("Key = tên stat, Value = lượng cộng. Ví dụ: damage -> 10")]
    public SerializableDictionary<string, float> statBonuses = new SerializableDictionary<string, float>();

    [Header("World Drop")]
    [Tooltip("Prefab spawn khi drop ra đất - để trống nếu chưa có")]
    public GameObject worldPrefab;
}

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
    ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear(); values.Clear();
        foreach (var kv in this) { keys.Add(kv.Key); values.Add(kv.Value); }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
            this[keys[i]] = values[i];
    }
}