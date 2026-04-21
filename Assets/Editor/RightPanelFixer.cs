// ===============================================================
// RightPanelFixer.cs
// Bỏ vào: Assets/Editor/RightPanelFixer.cs
// Chạy: Menu Unity → Tools → Fix Right Panel Layout
// ===============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class RightPanelFixer : Editor
{
    [MenuItem("Tools/Fix Right Panel Layout")]
    public static void FixRightPanel()
    {
        GameObject craftingRoot = GameObject.Find("CraftingRoot");
        if (craftingRoot == null) { Debug.LogError("[RightFixer] Không tìm thấy CraftingRoot!"); return; }

        Transform right = FindDeep(craftingRoot.transform, "Right Panel");
        if (right == null) { Debug.LogError("[RightFixer] Không tìm thấy Right Panel!"); return; }

        // ── Right Panel VLG ─────────────────────────────────────
        // Tắt Control Child Size Width và Force Expand Width
        // để các container không bị stretch sai
        VerticalLayoutGroup vlg = right.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.childControlWidth  = false; // ← tắt, để child tự quyết width
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 6;
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.childAlignment = TextAnchor.UpperLeft;
            Debug.Log("[RightFixer] ✅ Fix Right Panel VLG xong");
        }

        // ── Img_InfoIcon ─────────────────────────────────────────
        Transform icon = FindDeep(right, "Img_InfoIcon");
        if (icon != null)
        {
            RectTransform rt = icon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 64);
            LayoutElement le = GetOrAdd<LayoutElement>(icon.gameObject);
            le.minWidth = 64; le.preferredWidth = 64;
            le.minHeight = 64; le.preferredHeight = 64;
        }

        // ── Các text — set width bằng right panel content width ──
        // Right Panel width ~357, padding 12*2 = 24 → content = 333
        float contentW = 333f;

        SetTextSize(FindDeep(right, "Txt_ItemName"),    contentW, 36f);
        SetTextSize(FindDeep(right, "Txt_Rarity"),      contentW, 28f);
        SetTextSize(FindDeep(right, "Txt_Description"), contentW, 48f);
        SetTextSize(FindDeep(right, "Txt_StatsHeader"), contentW, 28f);
        SetTextSize(FindDeep(right, "Txt_MatHeader"),   contentW, 28f);

        // ── StatsContainer ───────────────────────────────────────
        Transform statsContainer = FindDeep(right, "StatsContainer");
        if (statsContainer != null)
        {
            RectTransform rt = statsContainer.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(contentW, rt.sizeDelta.y);

            // Đảm bảo VLG đúng
            VerticalLayoutGroup svlg = GetOrAdd<VerticalLayoutGroup>(statsContainer.gameObject);
            svlg.spacing = 3;
            svlg.padding = new RectOffset(0, 0, 4, 4);
            svlg.childControlWidth  = false;
            svlg.childControlHeight = false;
            svlg.childForceExpandWidth  = false;
            svlg.childForceExpandHeight = false;
            svlg.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter csf = GetOrAdd<ContentSizeFitter>(statsContainer.gameObject);
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            LayoutElement le = GetOrAdd<LayoutElement>(statsContainer.gameObject);
            le.minWidth = contentW;
            le.preferredWidth = contentW;

            // Fix từng StatRow bên trong
            foreach (Transform child in statsContainer)
                FixStatRowSize(child.gameObject, contentW);

            Debug.Log("[RightFixer] ✅ Fix StatsContainer xong");
        }

        // ── MaterialsContainer ───────────────────────────────────
        Transform matContainer = FindDeep(right, "MaterialsContainer");
        if (matContainer != null)
        {
            RectTransform rt = matContainer.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(contentW, rt.sizeDelta.y);

            VerticalLayoutGroup mvlg = GetOrAdd<VerticalLayoutGroup>(matContainer.gameObject);
            mvlg.spacing = 4;
            mvlg.padding = new RectOffset(0, 0, 4, 4);
            mvlg.childControlWidth  = false;
            mvlg.childControlHeight = false;
            mvlg.childForceExpandWidth  = false;
            mvlg.childForceExpandHeight = false;
            mvlg.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter csf = GetOrAdd<ContentSizeFitter>(matContainer.gameObject);
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            LayoutElement le = GetOrAdd<LayoutElement>(matContainer.gameObject);
            le.minWidth = contentW;
            le.preferredWidth = contentW;

            // Fix từng MaterialRow bên trong
            foreach (Transform child in matContainer)
                FixMaterialRowSize(child.gameObject, contentW);

            Debug.Log("[RightFixer] ✅ Fix MaterialsContainer xong");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RightFixer] ✅ Hoàn tất!");
    }

    static void FixStatRowSize(GameObject row, float rowWidth)
    {
        RectTransform rt = row.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(rowWidth, 52);

        LayoutElement le = GetOrAdd<LayoutElement>(row);
        le.minWidth = rowWidth;
        le.preferredWidth = rowWidth;
        le.minHeight = 52;
        le.preferredHeight = 52;
    }

    static void FixMaterialRowSize(GameObject row, float rowWidth)
    {
        RectTransform rt = row.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(rowWidth, 52);

        LayoutElement le = GetOrAdd<LayoutElement>(row);
        le.minWidth = rowWidth;
        le.preferredWidth = rowWidth;
        le.minHeight = 52;
        le.preferredHeight = 52;
    }

    static void SetTextSize(Transform t, float width, float height)
    {
        if (t == null) return;
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(width, height);
        LayoutElement le = GetOrAdd<LayoutElement>(t.gameObject);
        le.minWidth = width;
        le.preferredWidth = width;
        le.preferredHeight = height;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
