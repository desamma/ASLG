using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class RecipeListItem : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI – Icon")]
    public Image iconImage;

    [Header("UI – Text")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeRarityText;

    [Header("UI – Status dot")]
    public Image statusDot;

    [Header("UI – Background & Selection")]
    public Image background;
    public Image selectionHighlight;

    [Header("Colors – Row background")]
    public Color bgNormal = new Color(0.70f, 0.59f, 0.42f, 1f);
    public Color bgSelected = new Color(0.55f, 0.39f, 0.22f, 1f);
    public Color bgHover = new Color(0.73f, 0.63f, 0.47f, 1f);

    [Header("Colors – Status dot")]
    public Color dotCanCraft = new Color(0.30f, 1.00f, 0.40f, 1f);
    public Color dotCantCraft = new Color(1.00f, 0.30f, 0.30f, 1f);

    [Header("Colors – Rarity")]
    public Color colorCommon = new Color(0.80f, 0.80f, 0.80f);
    public Color colorUncommon = new Color(0.20f, 0.80f, 0.20f);
    public Color colorRare = new Color(0.20f, 0.60f, 1.00f);
    public Color colorEpic = new Color(0.80f, 0.20f, 0.95f);
    public Color colorLegendary = new Color(1.00f, 0.70f, 0.00f);

    [Header("Alpha")]
    [Range(0.3f, 1f)]
    public float alphaCantCraft = 0.55f;

    [Header("Compact Layout (Runtime)")]
    [SerializeField] private float rowHeight = 36f;
    [SerializeField] private float iconSize = 24f;
    [SerializeField] private float leftPadding = 8f;
    [SerializeField] private float rightPadding = 10f;

    // ── Text sizing ───────────────────────────────────────────────────────
    [Header("Text Sizes")]
    [SerializeField] private float nameFontSize = 14f;
    [SerializeField] private float rarityFontSize = 11f;

    // ── Runtime ──────────────────────────────────────────────────────────
    private Action _onClick;
    private bool _isSelected;

    private void Awake()
    {
        EnsureRaycastSurface();
        ApplyCompactLayout();
    }

    private void EnsureRaycastSurface()
    {
        if (background == null)
        {
            var bgChild = transform.Find("Background");
            if (bgChild != null)
                background = bgChild.GetComponent<Image>();
        }
        if (background == null) background = GetComponent<Image>();
        if (background == null) background = gameObject.AddComponent<Image>();

        background.color = bgNormal;
        background.raycastTarget = true;

        if (selectionHighlight != null)
            selectionHighlight.enabled = false;

        var cg = GetComponent<CanvasGroup>();
        if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }
    }

    // ── Layout helpers ────────────────────────────────────────────────────
    private static void SetStretch(RectTransform rt, float l, float r, float t, float b)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
        rt.localScale = Vector3.one;
    }

    private static void SetAnchored(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 size, Vector2 pos)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.localScale = Vector3.one;
    }

    // ── Disable any LayoutGroup on a transform (prevents auto-resize) ─────
    private static void KillLayoutGroup(Transform t)
    {
        if (t == null) return;
        var hlg = t.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;
        var vlg = t.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;
    }

    private void ApplyCompactLayout()
    {
        // ── Row LayoutElement ─────────────────────────────────────────────
        var le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;
        le.flexibleHeight = 0f;

        // ── Background ────────────────────────────────────────────────────
        var bgT = transform.Find("Background");
        if (bgT == null && background != null && background.transform != transform)
            bgT = background.transform;
        if (bgT != null)
        {
            var rt = bgT.GetComponent<RectTransform>();
            if (rt != null) SetStretch(rt, 0, 0, 0, 0);
            bgT.SetAsFirstSibling();
        }

        var rootImg = GetComponent<Image>();
        if (rootImg != null && bgT != null && rootImg.transform != bgT)
        {
            rootImg.color = new Color(0, 0, 0, 0);
            rootImg.raycastTarget = true;
        }

        float eff = Mathf.Min(iconSize, rowHeight - 4f);
        float dotW = 20f;
        float dotH = 20f;
        float dotX = -(rightPadding + dotW * 0.5f + 2f);

        // ── IconImage ─────────────────────────────────────────────────────
        var iconT = transform.Find("IconImage");
        if (iconT != null)
        {
            var iconLE = iconT.GetComponent<LayoutElement>();
            if (iconLE != null) DestroyImmediate(iconLE);

            var rt = iconT.GetComponent<RectTransform>();
            if (rt != null)
                SetAnchored(rt,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(eff, eff),
                    new Vector2(leftPadding + eff * 0.5f, 0f));

            var img = iconT.GetComponent<Image>();
            if (img != null) img.preserveAspect = true;
        }

        // ── InfoGroup ─────────────────────────────────────────────────────
        // Tắt mọi LayoutGroup để các text con không bị auto-stack
        var infoT = transform.Find("InfoGroup");
        if (infoT != null)
        {
            KillLayoutGroup(infoT);

            // Loại bỏ LayoutElement trên InfoGroup (nếu có) để tránh xung đột
            var infoLE = infoT.GetComponent<LayoutElement>();
            if (infoLE != null) infoLE.ignoreLayout = true;

            float padL = leftPadding + eff + 6f;
            float padR = rightPadding + dotW + 8f;

            var rt = infoT.GetComponent<RectTransform>();
            if (rt != null) SetStretch(rt, padL, padR, 0f, 0f);

            // ── nameText: nửa trên ────────────────────────────────────────
            // Anchor: top-stretch, chiếm ~55% chiều cao
            var nameT = infoT.Find("NameText");
            // Fallback: tìm TMP đầu tiên nếu không có child tên "NameText"
            if (nameT == null && nameText != null)
                nameT = nameText.transform;

            if (nameT != null)
            {
                var nRT = nameT.GetComponent<RectTransform>();
                if (nRT != null)
                {
                    nRT.anchorMin = new Vector2(0f, 0.5f);
                    nRT.anchorMax = new Vector2(1f, 1f);
                    nRT.pivot = new Vector2(0f, 1f);
                    nRT.anchoredPosition = new Vector2(0f, -1f); // 1px từ top xuống
                    nRT.sizeDelta = new Vector2(0f, 0f);
                    nRT.offsetMin = new Vector2(0f, 0f);
                    nRT.offsetMax = new Vector2(0f, -1f);
                    nRT.localScale = Vector3.one;
                }
                var tmp = nameT.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = nameFontSize;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.BottomLeft;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                    tmp.enableWordWrapping = false;
                    tmp.raycastTarget = false;
                }
            }

            // ── typeRarityText: nửa dưới ─────────────────────────────────
            var rarityT = infoT.Find("TypeRarityText");
            if (rarityT == null && typeRarityText != null)
                rarityT = typeRarityText.transform;

            if (rarityT != null)
            {
                var rRT = rarityT.GetComponent<RectTransform>();
                if (rRT != null)
                {
                    rRT.anchorMin = new Vector2(0f, 0f);
                    rRT.anchorMax = new Vector2(1f, 0.5f);
                    rRT.pivot = new Vector2(0f, 0f);
                    rRT.anchoredPosition = new Vector2(0f, 1f);  // 1px từ bottom lên
                    rRT.sizeDelta = new Vector2(0f, 0f);
                    rRT.offsetMin = new Vector2(0f, 1f);
                    rRT.offsetMax = new Vector2(0f, 0f);
                    rRT.localScale = Vector3.one;
                }
                var tmp = rarityT.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = rarityFontSize;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                    tmp.enableWordWrapping = false;
                    tmp.raycastTarget = false;
                }
            }
        }

        // ── StatusDot ─────────────────────────────────────────────────────
        var dotT = transform.Find("StatusDot");
        if (dotT != null)
        {
            var rt = dotT.GetComponent<RectTransform>();
            if (rt != null)
                SetAnchored(rt,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(dotW, dotH),
                    new Vector2(dotX, 0f));
            var img = dotT.GetComponent<Image>();
            if (img != null) img.preserveAspect = false;
        }
    }

    // ── Setup ─────────────────────────────────────────────────────────────
    public void Setup(CraftingRecipe recipe, bool canCraft, bool isSelected, Action onClick)
    {
        EnsureRaycastSurface();
        ApplyCompactLayout();

        _onClick = onClick;
        _isSelected = isSelected;

        if (recipe?.resultItem == null) return;
        var item = recipe.resultItem;

        if (iconImage)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;
        }

        if (nameText)
            nameText.text = item.itemName;

        if (typeRarityText)
        {
            typeRarityText.text = $"{item.itemType} · {item.rarity}";
            typeRarityText.color = GetRarityColor(item.rarity);
        }

        if (statusDot)
            statusDot.color = canCraft ? dotCanCraft : dotCantCraft;

        if (background)
        {
            background.color = isSelected ? bgSelected : bgNormal;
            background.raycastTarget = true;
        }

        if (selectionHighlight)
            selectionHighlight.enabled = isSelected;

        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = canCraft ? 1f : alphaCantCraft;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    // ── Pointer events ────────────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[RecipeListItem] OnPointerClick: {nameText?.text}");
        _onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isSelected && background)
            background.color = bgHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelected && background)
            background.color = _isSelected ? bgSelected : bgNormal;
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

public static class CanvasGroupExtensions
{
    public static void SetAlpha(this CanvasGroup cg, float alpha)
    {
        if (cg == null) return;
        cg.alpha = alpha;
    }
}