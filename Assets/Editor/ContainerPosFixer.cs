// ===============================================================
// ContainerPosFixer.cs
// Bỏ vào: Assets/Editor/ContainerPosFixer.cs
// Chạy: Menu Unity → Tools → Fix Container Positions
// ===============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class ContainerPosFixer : Editor
{
    [MenuItem("Tools/Fix Container Positions")]
    public static void FixPositions()
    {
        GameObject craftingRoot = GameObject.Find("CraftingRoot");
        if (craftingRoot == null) { Debug.LogError("Không tìm thấy CraftingRoot!"); return; }

        // Reset StatsContainer
        Transform stats = FindDeep(craftingRoot.transform, "StatsContainer");
        if (stats != null)
        {
            RectTransform rt = stats.GetComponent<RectTransform>();
            // Anchor top-left, reset về 0 để VLG tự đặt
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(333, rt.sizeDelta.y);

            // Xóa LayoutElement min/preferred width để VLG tự control
            LayoutElement le = stats.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minWidth = -1;       // -1 = disabled
                le.preferredWidth = -1;
            }
            Debug.Log("[ContainerFixer] ✅ Reset StatsContainer position");
        }

        // Reset MaterialsContainer
        Transform mats = FindDeep(craftingRoot.transform, "MaterialsContainer");
        if (mats != null)
        {
            RectTransform rt = mats.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(333, rt.sizeDelta.y);

            LayoutElement le = mats.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minWidth = -1;
                le.preferredWidth = -1;
            }
            Debug.Log("[ContainerFixer] ✅ Reset MaterialsContainer position");
        }

        // Bật lại Control Child Size Width trên Right Panel VLG
        // để VLG tự stretch containers theo panel width
        Transform right = FindDeep(craftingRoot.transform, "Right Panel");
        if (right != null)
        {
            VerticalLayoutGroup vlg = right.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
                Debug.Log("[ContainerFixer] ✅ Bật Control Child Size Width trên Right Panel VLG");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[ContainerFixer] ✅ Hoàn tất! Kiểm tra Game view.");
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
