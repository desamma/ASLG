using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// InventoryUI - Bấm E để mở/đóng.
/// KHÔNG cần sprite/asset nào - chạy được ngay với UI thuần màu + emoji.
/// Khi có asset thật chỉ cần gán Sprite vào ItemData.icon là xong.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject inventoryPanel;

    // ── STATS PANEL ──────────────────────────────────────────────────────
    [Header("Stats Panel")]
    [SerializeField] private TextMeshProUGUI txt_level;
    [SerializeField] private TextMeshProUGUI txt_expBar;       // "34 / 100 EXP"
    [SerializeField] private Slider slider_exp;
    [SerializeField] private TextMeshProUGUI txt_hp;
    [SerializeField] private Slider slider_hp;
    [SerializeField] private TextMeshProUGUI txt_mana;
    [SerializeField] private Slider slider_mana;
    [SerializeField] private TextMeshProUGUI txt_stamina;
    [SerializeField] private Slider slider_stamina;
    [SerializeField] private TextMeshProUGUI txt_damage;
    [SerializeField] private TextMeshProUGUI txt_moveSpeed;
    [SerializeField] private TextMeshProUGUI txt_range;
    [SerializeField] private TextMeshProUGUI txt_cooldown;
    [SerializeField] private TextMeshProUGUI txt_knockback;
    [SerializeField] private TextMeshProUGUI txt_upgradePoints;

    // ── INVENTORY PANEL ──────────────────────────────────────────────────
    [Header("Inventory Panel")]
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private Transform slotGrid;
    [SerializeField] private Button btn_nextPage;
    [SerializeField] private Button btn_prevPage;
    [SerializeField] private TextMeshProUGUI txt_pageIndicator;
    [SerializeField] private int slotsPerPage = 10;

    // ── PREVIEW PANEL ────────────────────────────────────────────────────
    [Header("Preview Panel")]
    [SerializeField] private GameObject emptyHint;        // "Hover an item..."
    [SerializeField] private GameObject previewContent;   // group các field bên dưới
    [SerializeField] private TextMeshProUGUI txt_previewIcon;  // emoji lớn
    [SerializeField] private Image img_previewIcon;  // sprite thật (nếu có)
    [SerializeField] private TextMeshProUGUI txt_previewName;
    [SerializeField] private TextMeshProUGUI txt_previewRarity;
    [SerializeField] private TextMeshProUGUI txt_previewDesc;
    [SerializeField] private TextMeshProUGUI txt_previewStats;
    [SerializeField] private Button btn_drop;

    // ── Runtime ──────────────────────────────────────────────────────────
    private bool isOpen;
    private int currentPage;
    private ItemData selectedItem;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    public List<ItemData> playerItems = new List<ItemData>();

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        inventoryPanel.SetActive(false);
        btn_nextPage?.onClick.AddListener(NextPage);
        btn_prevPage?.onClick.AddListener(PrevPage);
        btn_drop?.onClick.AddListener(DropSelected);

        if (StatsManager.instance != null)
            StatsManager.instance.OnStatsChangedEvent += RefreshStats;

        SpawnSlots();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) Toggle();
    }

    // ── Mở / Đóng ────────────────────────────────────────────────────────
    public void Toggle()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        if (isOpen) { currentPage = 0; RefreshAll(); }
    }

    // ── Refresh ───────────────────────────────────────────────────────────
    private void RefreshAll() { RefreshStats(); RenderPage(); ShowEmptyPreview(); }

    private void RefreshStats()
    {
        var s = StatsManager.instance;
        if (s == null) return;

        txt_level?.SetText($"LV {s.level}");
        txt_expBar?.SetText($"{s.currentExp} / {s.expToNextLevel} EXP");
        if (slider_exp) slider_exp.value = (float)s.currentExp / s.expToNextLevel;

        txt_hp?.SetText($"{s.currentHealth:0} / {s.maxHealth:0}");
        if (slider_hp) slider_hp.value = s.currentHealth / s.maxHealth;

        txt_mana?.SetText($"{s.currentMana:0} / {s.maxMana:0}");
        if (slider_mana) slider_mana.value = s.currentMana / s.maxMana;

        txt_stamina?.SetText($"{s.currentStamina:0} / {s.maxStamina:0}");
        if (slider_stamina) slider_stamina.value = s.currentStamina / s.maxStamina;

        txt_damage?.SetText($"{s.damage}");
        txt_moveSpeed?.SetText($"{s.moveSpeed:F1}");
        txt_range?.SetText($"{s.weaponRange:F1}");
        txt_cooldown?.SetText($"{s.cooldown:F2}s");
        txt_knockback?.SetText($"{s.knockbackForce:F1}");
        txt_upgradePoints?.SetText($"{s.upgradePoints} pts");
    }

    // ── Grid / Paging ─────────────────────────────────────────────────────
    private void SpawnSlots()
    {
        foreach (var s in spawnedSlots) Destroy(s);
        spawnedSlots.Clear();
        for (int i = 0; i < slotsPerPage; i++)
            spawnedSlots.Add(Instantiate(itemSlotPrefab, slotGrid));
    }

    private void RenderPage()
    {
        int total = Mathf.Max(1, Mathf.CeilToInt((float)playerItems.Count / slotsPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, total - 1);
        txt_pageIndicator?.SetText($"{currentPage + 1} / {total}");
        btn_prevPage?.gameObject.SetActive(currentPage > 0);
        btn_nextPage?.gameObject.SetActive(currentPage < total - 1);

        int start = currentPage * slotsPerPage;
        for (int i = 0; i < slotsPerPage; i++)
        {
            if (i >= spawnedSlots.Count) break;
            var slot = spawnedSlots[i].GetComponent<ItemSlot>();
            int idx = start + i;
            if (idx < playerItems.Count)
                slot?.SetItem(playerItems[idx], () => ShowPreview(playerItems[idx]));
            else
                slot?.SetEmpty();
        }
    }

    private void NextPage() { currentPage++; RenderPage(); ShowEmptyPreview(); }
    private void PrevPage() { currentPage--; RenderPage(); ShowEmptyPreview(); }

    // ── Preview ───────────────────────────────────────────────────────────
    private void ShowPreview(ItemData item)
    {
        selectedItem = item;
        emptyHint?.SetActive(false);
        previewContent?.SetActive(true);

        // Icon: ưu tiên Sprite thật, fallback emoji
        bool hasSprite = item.icon != null;
        if (img_previewIcon) { img_previewIcon.sprite = item.icon; img_previewIcon.enabled = hasSprite; }
        if (txt_previewIcon) { txt_previewIcon.text = hasSprite ? "" : item.emojiIcon; txt_previewIcon.enabled = !hasSprite; }

        txt_previewName?.SetText(item.itemName);

        // Rarity + màu
        if (txt_previewRarity)
        {
            txt_previewRarity.SetText(item.rarity.ToString().ToUpper());
            txt_previewRarity.color = item.rarity switch
            {
                ItemRarity.Common => new Color(0.65f, 0.65f, 0.65f),
                ItemRarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
                ItemRarity.Rare => new Color(0.30f, 0.50f, 1.00f),
                ItemRarity.Epic => new Color(0.65f, 0.30f, 0.90f),
                ItemRarity.Legendary => new Color(1.00f, 0.60f, 0.10f),
                _ => Color.white
            };
        }

        txt_previewDesc?.SetText(item.description);

        // Stat bonuses
        var sb = new System.Text.StringBuilder();
        foreach (var kv in item.statBonuses)
            sb.AppendLine($"<color=#6a5a42>{kv.Key}</color>   <color=#90c060>+{kv.Value}</color>");
        txt_previewStats?.SetText(sb.ToString());

        btn_drop?.gameObject.SetActive(true);
    }

    private void ShowEmptyPreview()
    {
        selectedItem = null;
        emptyHint?.SetActive(true);
        previewContent?.SetActive(false);
        btn_drop?.gameObject.SetActive(false);
    }

    // ── Drop (public - gọi được từ ItemSlot chuột phải) ───────────────────
    public void DropSelected()
    {
        if (selectedItem == null) return;
        Debug.Log($"[Inventory] Dropped: {selectedItem.itemName}");
        playerItems.Remove(selectedItem);
        ShowEmptyPreview();
        RenderPage();
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void AddItem(ItemData item)
    {
        // Stack nếu được
        if (item.isStackable)
        {
            var existing = playerItems.Find(i => i.itemName == item.itemName && i.currentStack < i.maxStack);
            if (existing != null) { existing.currentStack++; if (isOpen) RenderPage(); return; }
        }
        playerItems.Add(item);
        if (isOpen) RenderPage();
    }

    private void OnDestroy()
    {
        if (StatsManager.instance != null)
            StatsManager.instance.OnStatsChangedEvent -= RefreshStats;
    }
}