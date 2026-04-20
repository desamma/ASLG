using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    // ── Inspector refs ───────────────────────────────────────────────────────

    [Header("Managers")]
    public CraftingManager craftingManager;
    public PlayerInventory playerInventory;

    [Header("Prefabs")]
    [Tooltip("Prefab 1 dòng recipe trong danh sách")]
    public GameObject recipeItemPrefab;
    [Tooltip("Prefab 1 hàng stat trong panel bên phải")]
    public GameObject statRowPrefab;
    [Tooltip("Prefab 1 hàng material trong panel bên phải")]
    public GameObject matRowPrefab;

    [Header("Left Panel")]
    public TMP_InputField searchInput;
    public Transform recipeListContent;    // Content của ScrollRect

    [Tooltip("Thứ tự: All, Weapon, Armor, Accessory, Consumable, Misc")]
    public Button[] categoryTabButtons;
    public Color tabActiveColor = new Color(0.35f, 0.40f, 0.15f);
    public Color tabNormalColor = new Color(0.28f, 0.19f, 0.12f);

    // ĐÃ SỬA THÀNH 3 SLOT THEO GUIDE V2
    [Header("Mid Panel – Grid 3 Slot Ngang")]
    [Tooltip("Kéo 3 Image theo thứ tự: Slot_0 | Slot_1 | Slot_2")]
    public Image[] gridSlotImages = new Image[3];
    [Tooltip("Text số lượng tương ứng 3 slot")]
    public TextMeshProUGUI[] gridSlotTexts = new TextMeshProUGUI[3];

    [Header("Mid Panel – Result & Craft")]
    public Image resultIconImage;
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;
    public Button qtyMinusButton;
    public Button qtyPlusButton;
    public TextMeshProUGUI qtyText;

    [Header("Right Panel – Info")]
    public Image infoIconImage;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoRarityText;
    public TextMeshProUGUI infoDescText;
    public Transform statsContainer;
    public Transform materialsContainer;

    [Header("Rarity Colors")]
    public Color colorCommon = new Color(0.67f, 0.67f, 0.67f);
    public Color colorUncommon = new Color(0.20f, 0.80f, 0.20f);
    public Color colorRare = new Color(0.20f, 0.60f, 1.00f);
    public Color colorEpic = new Color(0.80f, 0.20f, 0.95f);
    public Color colorLegendary = new Color(1.00f, 0.70f, 0.00f);

    [Header("Feedback")]
    [Tooltip("Duration hiệu ứng flash khi craft thành công (giây)")]
    public float craftFlashDuration = 0.4f;
    public Color craftSuccessColor = new Color(0.5f, 1f, 0.3f, 0.6f);

    // ── Runtime ──────────────────────────────────────────────────────────────

    private List<CraftingRecipe> _filteredRecipes = new List<CraftingRecipe>();
    private CraftingRecipe _selectedRecipe;
    private int _craftQty = 1;
    private string _currentCategory = "All";
    private string _searchQuery = "";
    private int _playerLevel = 1;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Bind events
        craftButton.onClick.AddListener(OnCraftClicked);
        qtyMinusButton.onClick.AddListener(() => ChangeCraftQty(-1));
        qtyPlusButton.onClick.AddListener(() => ChangeCraftQty(1));
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        // ĐÃ SỬA: Cập nhật 6 tab (Thêm Accessory vào index 3)
        for (int i = 0; i < categoryTabButtons.Length; i++)
        {
            string cat = i switch
            {
                1 => "Weapon",
                2 => "Armor",
                3 => "Accessory",
                4 => "Consumable",
                5 => "Misc",
                _ => "All"
            };
            int idx = i;
            categoryTabButtons[idx].onClick.AddListener(() => OnCategorySelected(cat, idx));
        }

        if (playerInventory != null)
            playerInventory.OnInventoryChanged += RefreshCurrentSelection;

        if (craftingManager != null)
        {
            craftingManager.OnCraftSuccess += HandleCraftSuccess;
            craftingManager.OnCraftFailed += HandleCraftFailed;
        }

        RefreshRecipeList();
        SetCraftButtonState(false);
        ClearInfo();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= RefreshCurrentSelection;
        if (craftingManager != null)
        {
            craftingManager.OnCraftSuccess -= HandleCraftSuccess;
            craftingManager.OnCraftFailed -= HandleCraftFailed;
        }
    }

    // ── Recipe list ──────────────────────────────────────────────────────────

    private void RefreshRecipeList()
    {
        foreach (Transform child in recipeListContent)
            Destroy(child.gameObject);

        var allAvailable = craftingManager != null
            ? craftingManager.GetAvailableRecipes(_playerLevel)
            : new List<CraftingRecipe>();

        _filteredRecipes.Clear();
        foreach (var r in allAvailable)
        {
            if (r.resultItem == null) continue;

            bool matchCat = _currentCategory == "All" ||
                            r.resultItem.itemType.ToString() == _currentCategory;
            bool matchSearch = string.IsNullOrEmpty(_searchQuery) ||
                               r.DisplayName.ToLower().Contains(_searchQuery.ToLower());
            if (!matchCat || !matchSearch) continue;

            _filteredRecipes.Add(r);
        }

        foreach (var recipe in _filteredRecipes)
        {
            var go = Instantiate(recipeItemPrefab, recipeListContent);
            var row = go.GetComponent<RecipeListItem>();
            if (row != null)
            {
                bool canCraft = recipe.CanCraft(playerInventory);
                bool isSelected = _selectedRecipe == recipe;
                row.Setup(recipe, canCraft, isSelected, () => SelectRecipe(recipe));
            }
        }
    }

    private void OnSearchChanged(string value)
    {
        _searchQuery = value;
        RefreshRecipeList();
    }

    private void OnCategorySelected(string cat, int btnIndex)
    {
        _currentCategory = cat;
        for (int i = 0; i < categoryTabButtons.Length; i++)
        {
            var img = categoryTabButtons[i].GetComponent<Image>();
            if (img) img.color = (i == btnIndex) ? tabActiveColor : tabNormalColor;
        }
        RefreshRecipeList();
    }

    // ── Recipe selection ─────────────────────────────────────────────────────

    private void SelectRecipe(CraftingRecipe recipe)
    {
        _selectedRecipe = recipe;
        _craftQty = 1;
        UpdateQtyDisplay();
        UpdateGrid(recipe);
        UpdateInfoPanel(recipe);
        RefreshCraftButton();
        RefreshRecipeList();
    }

    // ĐÃ SỬA: Cập nhật lưới hiển thị 3 slot ngang theo v2
    private void UpdateGrid(CraftingRecipe recipe)
    {
        int slotCount = gridSlotImages != null ? gridSlotImages.Length : 3;
        var ingredientMap = new (Sprite icon, int qty)[slotCount];

        int slot = 0;
        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null || slot >= slotCount) continue;
            ingredientMap[slot] = (ing.item.icon, ing.amount);
            slot++;
        }

        for (int i = 0; i < slotCount; i++)
        {
            var img = (gridSlotImages != null && i < gridSlotImages.Length) ? gridSlotImages[i] : null;
            var txt = (gridSlotTexts != null && i < gridSlotTexts.Length) ? gridSlotTexts[i] : null;

            bool hasItem = ingredientMap[i].icon != null || ingredientMap[i].qty > 0;
            if (img)
            {
                img.sprite = ingredientMap[i].icon;
                img.enabled = hasItem;
            }
            if (txt)
            {
                txt.text = (hasItem && ingredientMap[i].qty > 1) ? $"x{ingredientMap[i].qty}" : "";
                txt.enabled = hasItem;
            }
        }

        if (resultIconImage && recipe.resultItem != null)
            resultIconImage.sprite = recipe.resultItem.icon;
    }

    private void UpdateInfoPanel(CraftingRecipe recipe)
    {
        if (recipe.resultItem == null) { ClearInfo(); return; }
        var item = recipe.resultItem;

        if (infoIconImage) infoIconImage.sprite = item.icon;
        if (infoNameText) infoNameText.text = item.itemName;
        if (infoRarityText)
        {
            infoRarityText.text = item.rarity.ToString();
            infoRarityText.color = GetRarityColor(item.rarity);
        }
        if (infoDescText) infoDescText.text = item.description;

        if (statsContainer)
        {
            foreach (Transform child in statsContainer) Destroy(child.gameObject);
            foreach (var bonus in item.statBonuses)
            {
                var row = Instantiate(statRowPrefab, statsContainer);
                var sr = row.GetComponent<StatRowUI>();
                if (sr) sr.Setup(bonus.statName, bonus.value);
            }
        }

        if (materialsContainer)
        {
            foreach (Transform child in materialsContainer) Destroy(child.gameObject);
            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null) continue;
                var row = Instantiate(matRowPrefab, materialsContainer);
                var mr = row.GetComponent<MaterialRowUI>();
                int have = playerInventory != null ? playerInventory.GetItemCount(ing.item) : 0;
                if (mr) mr.Setup(ing.item, ing.amount * _craftQty, have);
            }
        }
    }

    private void ClearInfo()
    {
        if (infoIconImage) infoIconImage.sprite = null;
        if (infoNameText) infoNameText.text = "Select a recipe";
        if (infoRarityText) infoRarityText.text = "—";

        // Đã đổi dòng này sang tiếng Anh
        if (infoDescText) infoDescText.text = "Select a recipe to view its details.";

        if (statsContainer) foreach (Transform c in statsContainer) Destroy(c.gameObject);
        if (materialsContainer) foreach (Transform c in materialsContainer) Destroy(c.gameObject);
        if (resultIconImage) resultIconImage.sprite = null;
    }

    // ── Crafting ─────────────────────────────────────────────────────────────

    private void OnCraftClicked()
    {
        if (_selectedRecipe == null || craftingManager == null || playerInventory == null) return;
        craftingManager.TryCraft(_selectedRecipe, playerInventory, _craftQty);
    }

    private void HandleCraftSuccess(CraftingRecipe recipe, int qty)
    {
        Debug.Log($"[Crafting] Crafted {qty}x {recipe.DisplayName}");
        StartCoroutine(FlashCraftSuccess());
        RefreshCurrentSelection();
        RefreshRecipeList();
    }

    private void HandleCraftFailed(CraftingRecipe recipe, string reason)
    {
        Debug.LogWarning($"[Crafting] Failed: {reason}");
    }

    private IEnumerator FlashCraftSuccess()
    {
        if (resultIconImage == null) yield break;
        var originalColor = resultIconImage.color;
        resultIconImage.color = craftSuccessColor;
        yield return new WaitForSeconds(craftFlashDuration);
        resultIconImage.color = originalColor;
    }

    // ── Qty controls ─────────────────────────────────────────────────────────

    private void ChangeCraftQty(int delta)
    {
        _craftQty = Mathf.Clamp(_craftQty + delta, 1, 99);
        UpdateQtyDisplay();
        if (_selectedRecipe != null) UpdateInfoPanel(_selectedRecipe);
        RefreshCraftButton();
    }

    private void UpdateQtyDisplay()
    {
        if (qtyText) qtyText.text = _craftQty.ToString();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void RefreshCurrentSelection()
    {
        if (_selectedRecipe == null) return;
        UpdateInfoPanel(_selectedRecipe);
        RefreshCraftButton();
    }

    private void RefreshCraftButton()
    {
        bool canCraft = _selectedRecipe != null &&
                        craftingManager != null &&
                        playerInventory != null &&
                        _selectedRecipe.CanCraft(playerInventory, _craftQty);
        SetCraftButtonState(canCraft);
    }

    private void SetCraftButtonState(bool interactable)
    {
        if (craftButton) craftButton.interactable = interactable;
        if (craftButtonText) craftButtonText.alpha = interactable ? 1f : 0.5f;
    }

    private Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => colorCommon,
        ItemRarity.Uncommon => colorUncommon,
        ItemRarity.Rare => colorRare,
        ItemRarity.Epic => colorEpic,
        ItemRarity.Legendary => colorLegendary,
        _ => Color.white
    };
}