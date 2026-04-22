using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingRaycastFixer : MonoBehaviour
{
    [ContextMenu("Fix Raycast Blocking")]
    public void FixRaycastBlocking()
    {
        var images = GetComponentsInChildren<Image>(true);
        int fixedCount = 0;

        foreach (var img in images)
        {
            if (img == null) continue;

            // Chỉ xử lý các nền dễ gây chặn click
            if (!IsBackgroundName(img.gameObject.name)) continue;

            // Nếu nằm trong RecipeListItem thì giữ lại để item vẫn click được
            if (img.GetComponentInParent<RecipeListItem>() != null) continue;

            // Nếu chính nó là control tương tác thì bỏ qua
            if (HasInteractiveComponent(img.gameObject)) continue;

            if (img.raycastTarget)
            {
                img.raycastTarget = false;
                fixedCount++;
                Debug.Log($"[CraftingRaycastFixer] Disabled raycast: {GetPath(img.transform)}", img.gameObject);
            }
        }

        Debug.Log($"[CraftingRaycastFixer] Done. Fixed {fixedCount} blocking background image(s).");
    }

    private static bool IsBackgroundName(string n)
    {
        if (string.IsNullOrEmpty(n)) return false;
        string lower = n.ToLowerInvariant();
        return lower == "background" || lower == "background" || lower.Contains("background") || lower.Contains("bg");
    }

    private static bool HasInteractiveComponent(GameObject go)
    {
        return go.GetComponent<Button>() != null ||
               go.GetComponent<Toggle>() != null ||
               go.GetComponent<Slider>() != null ||
               go.GetComponent<Scrollbar>() != null ||
               go.GetComponent<ScrollRect>() != null ||
               go.GetComponent<TMP_InputField>() != null ||
               go.GetComponent<Dropdown>() != null ||
               go.GetComponent<TMP_Dropdown>() != null;
    }

    private static string GetPath(Transform t)
    {
        if (t == null) return "<null>";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}