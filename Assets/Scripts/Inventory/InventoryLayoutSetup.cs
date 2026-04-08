using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này vào InventoryPanel.
/// Bấm chuột phải vào component này trong Inspector → "Setup Layout"
/// Script tự động set đúng kích thước và vị trí cho tất cả panel con.
/// </summary>
public class InventoryLayoutSetup : MonoBehaviour
{
    [Header("Tự động tìm theo tên — không cần kéo thả")]
    [SerializeField] private float totalWidth  = 900f;
    [SerializeField] private float totalHeight = 500f;
    [SerializeField] private float statsWidth  = 220f;
    [SerializeField] private float previewWidth= 220f;

    [ContextMenu("Setup Layout")]
    public void SetupLayout()
    {
        // ── 1. Setup bản thân InventoryPanel ──────────────────────────────
        var rt = GetComponent<RectTransform>();
        // Anchor giữa màn hình
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(totalWidth, totalHeight);

        // ── 2. Tìm 3 panel con theo tên ───────────────────────────────────
        var statsPanel   = transform.Find("StatsPanel")?.GetComponent<RectTransform>();
        var bagPanel     = transform.Find("BagPanel")?.GetComponent<RectTransform>();
        var previewPanel = transform.Find("PreviewPanel")?.GetComponent<RectTransform>();

        if (!statsPanel || !bagPanel || !previewPanel)
        {
            Debug.LogError("Không tìm thấy StatsPanel / BagPanel / PreviewPanel. Kiểm tra tên trong Hierarchy.");
            return;
        }

        float bagWidth = totalWidth - statsWidth - previewWidth;

        // Helper: set anchor top-left, pos tính từ góc trái trên của parent
        void SetPanel(RectTransform panel, float x, float w)
        {
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);  // stretch theo chiều dọc
            panel.pivot     = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(x, 0f);
            panel.sizeDelta = new Vector2(w, 0f);   // height = 0 vì đang stretch
        }

        SetPanel(statsPanel,   0f,                          statsWidth);
        SetPanel(bagPanel,     statsWidth,                  bagWidth);
        SetPanel(previewPanel, statsWidth + bagWidth,       previewWidth);

        Debug.Log($"[InventoryLayoutSetup] Done! Stats={statsWidth} Bag={bagWidth} Preview={previewWidth}");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }
}
