using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn script này vào root GameObject của prefab RecipeListItem.
/// Nó sẽ tự động set Anchors + RectTransform cho tất cả các child
/// để layout khớp với thiết kế (xem ảnh mẫu).
///
/// Layout mục tiêu (mỗi item cao ROW_HEIGHT px, full width):
/// ┌──────────────────────────────────────────────┐
/// │ [Icon 40x40]  ItemName            [dot 10x10]│
/// │               Type · Rarity                  │
/// └──────────────────────────────────────────────┘
/// </summary>
[ExecuteAlways]  // chạy cả trong Edit Mode để preview ngay
public class RecipeListItemLayout : MonoBehaviour
{
    // ── Kích thước tổng thể ───────────────────────────────────────────
    [Header("Row")]
    public float rowHeight = 52f;           // chiều cao mỗi hàng

    // ── Icon ──────────────────────────────────────────────────────────
    [Header("Icon")]
    public float iconSize   = 36f;          // width & height icon
    public float iconLeft   = 8f;           // khoảng cách từ mép trái
    // icon được căn giữa dọc theo rowHeight

    // ── Text block ────────────────────────────────────────────────────
    [Header("Text")]
    public float textLeft   = 54f;          // X bắt đầu cột text
    public float textRight  = 28f;          // khoảng cách từ mép phải (chừa chỗ dot)
    public float nameHeight = 22f;          // chiều cao dòng tên
    public float subHeight  = 18f;          // chiều cao dòng type·rarity

    // ── Status dot ────────────────────────────────────────────────────
    [Header("Status Dot")]
    public float dotSize    = 10f;
    public float dotRight   = 10f;          // khoảng cách từ mép phải

    // ── References (kéo thả trong Inspector hoặc để tự tìm) ──────────
    [Header("References (tuỳ chọn – tự tìm nếu để trống)")]
    public RectTransform iconRect;
    public RectTransform nameRect;
    public RectTransform typeRarityRect;
    public RectTransform dotRect;
    public RectTransform backgroundRect;
    public RectTransform highlightRect;

    // ─────────────────────────────────────────────────────────────────
    void Awake()  => Apply();
    void OnValidate() => Apply();   // cập nhật ngay khi đổi giá trị trong Inspector

    [ContextMenu("Apply Layout Now")]
    public void Apply()
    {
        // Nếu chưa assign thì tự tìm qua RecipeListItem
        var item = GetComponent<RecipeListItem>();
        if (item != null) AutoResolveRefs(item);

        var selfRect = GetComponent<RectTransform>();
        if (selfRect == null) return;

        // ── 1. Root: anchor stretch full width, height cố định ────────
        SetStretchH(selfRect, 0, 0);
        selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);

        // ── 2. Background: full fill ──────────────────────────────────
        if (backgroundRect)
            SetFill(backgroundRect);

        // ── 3. Selection Highlight: full fill ────────────────────────
        if (highlightRect)
            SetFill(highlightRect);

        // ── 4. Icon: left-middle ──────────────────────────────────────
        if (iconRect)
        {
            // anchor: left-center
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot     = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(iconLeft, 0f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        // ── 5. Name text: upper part of text block ───────────────────
        float textBlockH = nameHeight + subHeight;
        float textBlockY = textBlockH * 0.5f;   // offset từ center để cả block căn giữa

        if (nameRect)
        {
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 0.5f);
            nameRect.pivot     = new Vector2(0f, 0f);
            nameRect.offsetMin = new Vector2(textLeft,  textBlockY - textBlockH);
            nameRect.offsetMax = new Vector2(-textRight, textBlockY - subHeight);
        }

        // ── 6. Type·Rarity text: lower part ──────────────────────────
        if (typeRarityRect)
        {
            typeRarityRect.anchorMin = new Vector2(0f, 0.5f);
            typeRarityRect.anchorMax = new Vector2(1f, 0.5f);
            typeRarityRect.pivot     = new Vector2(0f, 0f);
            typeRarityRect.offsetMin = new Vector2(textLeft,  textBlockY - textBlockH);
            typeRarityRect.offsetMax = new Vector2(-textRight, textBlockY - nameHeight);
        }

        // ── 7. Status dot: right-center ───────────────────────────────
        if (dotRect)
        {
            dotRect.anchorMin = new Vector2(1f, 0.5f);
            dotRect.anchorMax = new Vector2(1f, 0.5f);
            dotRect.pivot     = new Vector2(1f, 0.5f);
            dotRect.anchoredPosition = new Vector2(-dotRight, 0f);
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// Stretch full width (horizontal), giữ nguyên Y
    private static void SetStretchH(RectTransform rt, float left, float right)
    {
        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
        rt.offsetMin = new Vector2(left,  rt.offsetMin.y);
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    /// Fill toàn bộ parent
    private static void SetFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// Tự tìm references qua RecipeListItem component
    private void AutoResolveRefs(RecipeListItem item)
    {
        if (!iconRect        && item.iconImage)         iconRect        = item.iconImage.rectTransform;
        if (!nameRect        && item.nameText)          nameRect        = item.nameText.rectTransform;
        if (!typeRarityRect  && item.typeRarityText)    typeRarityRect  = item.typeRarityText.rectTransform;
        if (!dotRect         && item.statusDot)         dotRect         = item.statusDot.rectTransform;
        if (!backgroundRect  && item.background)        backgroundRect  = item.background.rectTransform;
        if (!highlightRect   && item.selectionHighlight) highlightRect  = item.selectionHighlight.rectTransform;
    }
}
