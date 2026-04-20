using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime entry: item + số lượng hiện tại trong túi.
/// </summary>
[System.Serializable]
public class InventoryEntry
{
    public ItemData item;
    public int quantity;

    public InventoryEntry(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

/// <summary>
/// PlayerInventory – quản lý runtime inventory của player.
/// Attach vào GameObject Player.
/// Expose sự kiện OnInventoryChanged để các UI khác subscribe.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Capacity")]
    [Min(1)] public int maxSlots = 40;

    [Header("Runtime (read-only in inspector)")]
    [SerializeField] private List<InventoryEntry> _entries = new List<InventoryEntry>();

    public IReadOnlyList<InventoryEntry> Entries => _entries;

    // Gọi khi inventory thay đổi – CraftingUI và InventoryUI đều subscribe
    public event System.Action OnInventoryChanged;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Lấy tổng số lượng của một item trong inventory.</summary>
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;
        return _entries.Where(e => e.item == item).Sum(e => e.quantity);
    }

    /// <summary>Thêm item vào inventory. Trả về true nếu thành công.</summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (item.isStackable)
        {
            var existing = _entries.FirstOrDefault(e => e.item == item);
            if (existing != null)
            {
                int canAdd = item.maxStack - existing.quantity;
                existing.quantity += Mathf.Min(amount, canAdd);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        if (_entries.Count >= maxSlots) return false;

        _entries.Add(new InventoryEntry(item, Mathf.Min(amount, item.isStackable ? item.maxStack : 1)));
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Trừ đúng số lượng item. Trả về true nếu đủ và đã trừ thành công.</summary>
    public bool TryConsume(ItemData item, int amount = 1)
    {
        if (GetItemCount(item) < amount) return false;

        int remaining = amount;
        for (int i = _entries.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var e = _entries[i];
            if (e.item != item) continue;

            int take = Mathf.Min(e.quantity, remaining);
            e.quantity -= take;
            remaining -= take;

            if (e.quantity <= 0) _entries.RemoveAt(i);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Xóa toàn bộ item khỏi inventory.</summary>
    public void RemoveAllOf(ItemData item)
    {
        _entries.RemoveAll(e => e.item == item);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Inventory còn trống ít nhất 1 slot?</summary>
    public bool HasFreeSlot() => _entries.Count < maxSlots;
}
