using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    [Header("Managers")]
    public CraftingManager craftingManager;
    public PlayerInventory playerInventory;

    [Header("Prefabs")]
    public GameObject recipeItemPrefab;
    public GameObject statRowPrefab;
    public GameObject matRowPrefab;

    [Header("Left Panel")]
    public TMP_InputField searchInput;
    public Transform recipeListContent;

    [Tooltip("Thứ tự: All, Weapon, Armor, Accessory, Consumable, Misc")]
    public Button[] categoryTabButtons;
    public Color tabActiveColor = new Color(0.16f, 0.16f, 0.16f, 1f); // đen nhẹ khi chọn
    public Color tabNormalColor = new Color(0.28f, 0.19f, 0.12f, 1f);

    [Header("Recipe List Layout Fix")]
    [SerializeField] private float recipeRowHeight = 34f;
    [SerializeField] private float recipeRowSpacing = 2f;

    [Header("Mid Panel – Grid 3 Slot Ngang")]
    public Image[] gridSlotImages = new Image[3];
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
    public float craftFlashDuration = 0.4f;
    public Color craftSuccessColor = new Color(0.5f, 1f, 0.3f, 0.6f);

    private readonly List<CraftingRecipe> _filteredRecipes = new List<CraftingRecipe>();
    private CraftingRecipe _selectedRecipe;
    private int _craftQty = 1;
    private string _currentCategory = "All";
    private string _searchQuery = "";
    private int _playerLevel = 1;

    private void Start()
    {
        if (EventSystem.current == null)
            Debug.LogError("[CraftingUI] Missing EventSystem in scene. UI click will not work.");

        EnsureRecipeListLayout();

        craftButton.onClick.AddListener(OnCraftClicked);
        qtyMinusButton.onClick.AddListener(() => ChangeCraftQty(-1));
        qtyPlusButton.onClick.AddListener(() => ChangeCraftQty(1));
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        foreach (var img in gridSlotImages)
            if (img) img.enabled = false;
        if (resultIconImage) resultIconImage.enabled = false;
        if (infoIconImage) infoIconImage.enabled = false;

        for (int i = 0; i < categoryTabButtons.Length; i++)
        {
            
            string cat = i switch
            {
                1 => "Accessory",
                2 => "Weapon",
                3 => "Armor",
                4 => "Consumable",
                5 => "Misc",
                _ => "All"
            };

            int idx = i;
            var btn = categoryTabButtons[idx];
            if (btn == null) continue;

            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnCategorySelected(cat, idx));
        }

        // Set trạng thái tab mặc định ngay khi mở UI
        OnCategorySelected("All", 0);

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

    private void EnsureRecipeListLayout()
    {
        if (recipeListContent == null) return;

        var contentRt = recipeListContent as RectTransform;
        if (contentRt == null) return;

        // 1) Normalize Content rect – top-anchored so VLG stacks downward
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.localScale = Vector3.one;

        // 2) Remove conflicting layout components on Content
        //    Use DestroyImmediate so they're gone before RefreshRecipeList() runs
        var grid = recipeListContent.GetComponent<GridLayoutGroup>();
        if (grid != null) DestroyImmediate(grid);

        var hlg = recipeListContent.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) DestroyImmediate(hlg);

        // 3) Ensure VerticalLayoutGroup
        var vlg = recipeListContent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = recipeListContent.gameObject.AddComponent<VerticalLayoutGroup>();

        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;   // ← FALSE: row height is driven by LayoutElement, not VLG
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = recipeRowSpacing;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        // 4) ContentSizeFitter stretches Content height to fit all rows
        var fitter = recipeListContent.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = recipeListContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 5) Safety: Viewport must NOT have its own layout group / fitter
        var viewport = contentRt.parent;
        if (viewport != null)
        {
            var viewportVlg = viewport.GetComponent<VerticalLayoutGroup>();
            if (viewportVlg != null) DestroyImmediate(viewportVlg);

            var viewportHlg = viewport.GetComponent<HorizontalLayoutGroup>();
            if (viewportHlg != null) DestroyImmediate(viewportHlg);

            var viewportGrid = viewport.GetComponent<GridLayoutGroup>();
            if (viewportGrid != null) DestroyImmediate(viewportGrid);

            var viewportFitter = viewport.GetComponent<ContentSizeFitter>();
            if (viewportFitter != null) DestroyImmediate(viewportFitter);
        }
    }

    private void RefreshRecipeList()
    {
        // Dùng DestroyImmediate để children biến mất ngay, tránh VLG
        // tính toán layout với children cũ trong cùng frame.
        var toDestroy = new List<GameObject>();
        foreach (Transform child in recipeListContent)
            toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy)
            DestroyImmediate(go);

        var allAvailable = craftingManager != null
            ? craftingManager.GetAvailableRecipes(_playerLevel)
            : new List<CraftingRecipe>();

        _filteredRecipes.Clear();
        foreach (var r in allAvailable)
        {
            if (r.resultItem == null) continue;

            bool matchCat = _currentCategory == "All" || r.resultItem.itemType.ToString() == _currentCategory;
            bool matchSearch = string.IsNullOrEmpty(_searchQuery) || r.DisplayName.ToLower().Contains(_searchQuery.ToLower());
            if (!matchCat || !matchSearch) continue;

            _filteredRecipes.Add(r);
        }

        foreach (var recipe in _filteredRecipes)
        {
            var go = Instantiate(recipeItemPrefab, recipeListContent);

            // VLG owns positioning – chỉ reset scale
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.localScale = Vector3.one;

            // Kill row-level fitters/layouts that fight VLG
            var rowFitter = go.GetComponent<ContentSizeFitter>();
            if (rowFitter != null) DestroyImmediate(rowFitter);

            var rowVlg = go.GetComponent<VerticalLayoutGroup>();
            if (rowVlg != null) DestroyImmediate(rowVlg);

            var rowHlg = go.GetComponent<HorizontalLayoutGroup>();
            if (rowHlg != null) DestroyImmediate(rowHlg);

            var rowGrid = go.GetComponent<GridLayoutGroup>();
            if (rowGrid != null) DestroyImmediate(rowGrid);

            // LayoutElement: báo VLG chiều cao mỗi row
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = recipeRowHeight;
            le.preferredHeight = recipeRowHeight;
            le.flexibleHeight = 0f;
            le.minWidth = -1f;

            var row = go.GetComponent<RecipeListItem>();
            if (row == null)
                row = go.GetComponentInChildren<RecipeListItem>(true);

            if (row == null)
            {
                Debug.LogError("[CraftingUI] recipeItemPrefab is missing RecipeListItem component.");
                continue;
            }

            bool canCraft = recipe.CanCraft(playerInventory);
            bool isSelected = _selectedRecipe == recipe;
            row.Setup(recipe, canCraft, isSelected, () => SelectRecipe(recipe));
        }

        // Double-rebuild: lần 1 tính preferred size, lần 2 áp dụng
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(recipeListContent as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(recipeListContent as RectTransform);

        // Reset scroll về đầu list sau mỗi lần refresh category/search
        var scrollRect = recipeListContent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
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
            var btn = categoryTabButtons[i];
            if (btn == null) continue;

            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = (i == btnIndex) ? tabActiveColor : tabNormalColor;
        }

        RefreshRecipeList();
    }

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

        if (resultIconImage)
        {
            resultIconImage.sprite = recipe.resultItem != null ? recipe.resultItem.icon : null;
            resultIconImage.enabled = recipe.resultItem != null && recipe.resultItem.icon != null;
        }
    }

    private void UpdateInfoPanel(CraftingRecipe recipe)
    {
        if (recipe.resultItem == null) { ClearInfo(); return; }
        var item = recipe.resultItem;

        if (infoIconImage)
        {
            infoIconImage.sprite = item.icon;
            infoIconImage.enabled = item.icon != null;
        }
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
        if (infoIconImage)
        {
            infoIconImage.sprite = null;
            infoIconImage.enabled = false;
        }
        if (infoNameText) infoNameText.text = "Select a recipe";
        if (infoRarityText) infoRarityText.text = "—";
        if (infoDescText) infoDescText.text = "Select a recipe to view its details.";

        if (statsContainer) foreach (Transform c in statsContainer) Destroy(c.gameObject);
        if (materialsContainer) foreach (Transform c in materialsContainer) Destroy(c.gameObject);
        if (resultIconImage)
        {
            resultIconImage.sprite = null;
            resultIconImage.enabled = false;
        }
    }

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