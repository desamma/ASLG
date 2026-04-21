// ===============================================================
// RowWidthFixer.cs
// Bỏ vào: Assets/Editor/RowWidthFixer.cs
// Chạy: Menu Unity → Tools → Fix Row Widths
// ===============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class RowWidthFixer : Editor
{
    [MenuItem("Tools/Fix Row Widths")]
    public static void FixWidths()
    {
        GameObject craftingRoot = GameObject.Find("CraftingRoot");
        if (craftingRoot == null) { Debug.LogError("Không tìm thấy CraftingRoot!"); return; }

        int fixedCount = 0;

        // Fix tất cả StatRow và MaterialRow trong scene
        // Tìm trong StatsContainer
        Transform statsContainer = FindDeep(craftingRoot.transform, "StatsContainer");
        if (statsContainer != null)
        {
            foreach (Transform child in statsContainer)
            {
                ForceWidth(child.gameObject, 333f, 52f);
                fixedCount++;
            }
        }

        // Tìm trong MaterialsContainer  
        Transform matContainer = FindDeep(craftingRoot.transform, "MaterialsContainer");
        if (matContainer != null)
        {
            foreach (Transform child in matContainer)
            {
                ForceWidth(child.gameObject, 333f, 52f);
                fixedCount++;
            }
        }

        // Cũng fix prefab gốc nếu đang mở trong scene
        FixPrefabAsset("StatRow",    333f, 52f);
        FixPrefabAsset("MaterialRow", 333f, 52f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[RowWidthFixer] ✅ Đã fix {fixedCount} rows trong scene + 2 prefab assets!");
    }

    static void ForceWidth(GameObject go, float width, float height)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(width, height);

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minWidth       = width;
        le.preferredWidth  = width;
        le.minHeight      = height;
        le.preferredHeight = height;
    }

    static void FixPrefabAsset(string prefabName, float width, float height)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        if (guids.Length == 0) return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;
            RectTransform rt = root.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(width, height);

            LayoutElement le = root.GetComponent<LayoutElement>();
            if (le == null) le = root.AddComponent<LayoutElement>();
            le.minWidth       = width;
            le.preferredWidth  = width;
            le.minHeight      = height;
            le.preferredHeight = height;
        }
        Debug.Log($"[RowWidthFixer] ✅ Fixed prefab asset: {prefabName}");
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
