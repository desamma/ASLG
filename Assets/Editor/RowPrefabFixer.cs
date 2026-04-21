// ===============================================================
// RowPrefabFixer.cs
// Bỏ vào: Assets/Editor/RowPrefabFixer.cs
// Chạy: Menu Unity → Tools → Fix MaterialRow & StatRow Prefabs
// ===============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class RowPrefabFixer : Editor
{
    [MenuItem("Tools/Fix MaterialRow & StatRow Prefabs")]
    public static void FixRowPrefabs()
    {
        FixMaterialRow();
        FixStatRow();
    }

    // ════════════════════════════════════════════════════════════
    // MATERIAL ROW
    // Dựa theo thông số đo được khi Play:
    //   Root:         Anchor(0,1)-(0,1), W=362, H=92
    //   ItemIcon:     PosX=44, PosY=-41, W=89, H=82  → dùng tỉ lệ ~24% root
    //   ItemNameText: PosX=127, PosY=-47, W=88, H=41
    //   QuantityText: PosX=220, PosY=-50, W=46, H=39
    //   CheckIcon:    PosX=317, PosY=-54, W=52, H=55
    // Quy về prefab size chuẩn W=300, H=52
    // ════════════════════════════════════════════════════════════
    static void FixMaterialRow()
    {
        string[] guids = AssetDatabase.FindAssets("MaterialRow t:Prefab");
        if (guids.Length == 0) { Debug.LogWarning("[RowFixer] Không tìm thấy MaterialRow prefab!"); return; }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        Debug.Log($"[RowFixer] MaterialRow path: {path}");

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;

            // Root — xóa layout groups, set height
            DestroyIfExists<HorizontalLayoutGroup>(root);
            DestroyIfExists<VerticalLayoutGroup>(root);
            DestroyIfExists<ContentSizeFitter>(root);

            LayoutElement rootLE = GetOrAdd<LayoutElement>(root);
            rootLE.minHeight = 52;
            rootLE.preferredHeight = 52;

            // ── ItemIcon ────────────────────────────────────────
            // Anchor top-left, PosX=8, PosY=-10, W=32, H=32
            Transform icon = root.transform.Find("ItemIcon");
            if (icon != null)
            {
                RectTransform rt = icon.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(8, -10);
                rt.sizeDelta = new Vector2(32, 32);

                Image img = icon.GetComponent<Image>();
                if (img != null) img.preserveAspect = true;

                DestroyIfExists<LayoutElement>(icon.gameObject);
            }

            // ── ItemNameText ────────────────────────────────────
            // Anchor top-left, PosX=48, PosY=-14, W=140, H=28
            Transform nameT = root.transform.Find("ItemNameText");
            if (nameT != null)
            {
                RectTransform rt = nameT.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(48, -12);
                rt.sizeDelta = new Vector2(140, 28);

                TMP_Text tmp = nameT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 20;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.color = new Color(0.92f, 0.87f, 0.75f);
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                    tmp.enableWordWrapping = false;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                }

                DestroyIfExists<LayoutElement>(nameT.gameObject);
            }

            // ── QuantityText ────────────────────────────────────
            // Anchor top-left, PosX=200, PosY=-14, W=60, H=28
            Transform qtyT = root.transform.Find("QuantityText");
            if (qtyT != null)
            {
                RectTransform rt = qtyT.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(200, -12);
                rt.sizeDelta = new Vector2(60, 28);

                TMP_Text tmp = qtyT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 20;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Midline;
                    tmp.enableWordWrapping = false;
                }

                DestroyIfExists<LayoutElement>(qtyT.gameObject);
            }

            // ── CheckIcon ───────────────────────────────────────
            // Anchor top-left, PosX=268, PosY=-10, W=32, H=32
            Transform check = root.transform.Find("CheckIcon");
            if (check != null)
            {
                RectTransform rt = check.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(268, -10);
                rt.sizeDelta = new Vector2(32, 32);

                Image img = check.GetComponent<Image>();
                if (img != null) img.preserveAspect = true;

                DestroyIfExists<LayoutElement>(check.gameObject);
            }
        }

        Debug.Log("[RowFixer] ✅ Fix MaterialRow xong!");
    }

    // ════════════════════════════════════════════════════════════
    // STAT ROW
    // Dựa theo thông số đo được khi Play:
    //   Root:          W=333, H=93
    //   StatNameText:  PosX=132, PosY=-52, W=200, H=50  font=20
    //   StatValueText: (đối xứng bên phải)
    // ════════════════════════════════════════════════════════════
    static void FixStatRow()
    {
        string[] guids = AssetDatabase.FindAssets("StatRow t:Prefab");
        if (guids.Length == 0) { Debug.LogWarning("[RowFixer] Không tìm thấy StatRow prefab!"); return; }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        Debug.Log($"[RowFixer] StatRow path: {path}");

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;

            DestroyIfExists<HorizontalLayoutGroup>(root);
            DestroyIfExists<VerticalLayoutGroup>(root);
            DestroyIfExists<ContentSizeFitter>(root);

            LayoutElement rootLE = GetOrAdd<LayoutElement>(root);
            rootLE.minHeight = 52;
            rootLE.preferredHeight = 52;

            // Tìm text — ưu tiên tên cụ thể, fallback index
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>();
            Transform statNameT  = FindChild(root.transform, "StatNameText")
                                ?? (texts.Length > 0 ? texts[0].transform : null);
            Transform statValueT = FindChild(root.transform, "StatValueText")
                                ?? (texts.Length > 1 ? texts[1].transform : null);

            // ── StatNameText ────────────────────────────────────
            // Anchor top-left, PosX=8, PosY=-12, W=180, H=30
            if (statNameT != null)
            {
                RectTransform rt = statNameT.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(8, -12);
                rt.sizeDelta = new Vector2(180, 30);

                TMP_Text tmp = statNameT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 20;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.color = new Color(0.80f, 0.75f, 0.62f);
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                    tmp.enableWordWrapping = false;
                }

                DestroyIfExists<LayoutElement>(statNameT.gameObject);
            }

            // ── StatValueText ───────────────────────────────────
            // Anchor top-left, PosX=200, PosY=-12, W=100, H=30
            if (statValueT != null)
            {
                RectTransform rt = statValueT.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(200, -12);
                rt.sizeDelta = new Vector2(100, 30);

                TMP_Text tmp = statValueT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 20;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.MidlineRight;
                    tmp.enableWordWrapping = false;
                    // màu set bởi StatRowUI.Setup()
                }

                DestroyIfExists<LayoutElement>(statValueT.gameObject);
            }
        }

        Debug.Log("[RowFixer] ✅ Fix StatRow xong!");
    }

    // ── HELPERS ─────────────────────────────────────────────────
    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static void DestroyIfExists<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c != null) Object.DestroyImmediate(c);
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name) return child;
        return null;
    }
}
#endif
