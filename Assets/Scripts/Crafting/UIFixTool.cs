using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIFixTool : MonoBehaviour
{
    // Tạo một nút bấm siêu to khổng lồ ngay trong menu chuột phải
    [ContextMenu("🔥 BẤM VÀO ĐÂY ĐỂ ĐỔI CHỮ & LAYOUT 🔥")]
    public void RunToolInEditMode()
    {
        FixLayouts();
        AutoFillTextEnglish();

#if UNITY_EDITOR
        // Bắt buộc Unity phải ghi nhận sự thay đổi để bạn có thể Save (Ctrl+S) Scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("<color=green>Đã cập nhật xong toàn bộ UI bằng Edit Mode!</color>");
    }

    private void AutoFillTextEnglish()
    {
        // 1. Top Bar
        ChangeTextHelper("Txt_Title", "CRAFTING");
        ChangeTextHelper("Btn_back", "Back");

        // 2. Tabs
        ChangeTextHelper("Tab_All", "All");
        ChangeTextHelper("Tab_Weapon", "Weapon");
        ChangeTextHelper("Tab_Armor", "Armor");
        ChangeTextHelper("Tab_Accessory", "Accessory"); // Phải có nút tên này nhé
        ChangeTextHelper("Tab_Consumable", "Consumable");
        ChangeTextHelper("Tab_Misc", "Misc");

        // 3. Mid Panel (Chú ý trong ảnh của bạn tên object là "Txt_MidTitle:")
        ChangeTextHelper("Txt_MidTitle:", "RECIPE");
        ChangeTextHelper("Btn_QtyMinus", "-");
        ChangeTextHelper("Txt_Qty", "1");
        ChangeTextHelper("Btn_QtyPlus", "+");
        ChangeTextHelper("Btn_Craft", "CRAFT");

        // 4. Right Panel
        ChangeTextHelper("Txt_ItemName", "Item Name");
        ChangeTextHelper("Txt_Rarity", "Rarity");
        ChangeTextHelper("Txt_Description", "Select a recipe to view details...");
        ChangeTextHelper("Txt_StatsHeader", "STATS");
        ChangeTextHelper("Txt_MatHeader", "MATERIALS");
    }

    // --- CÁC HÀM HỖ TRỢ BÊN DƯỚI ---

    private void ChangeTextHelper(string objectName, string newText)
    {
        Transform found = FindChildRecursive(transform, objectName);
        if (found != null)
        {
            TMP_Text tmp = found.GetComponentInChildren<TMP_Text>();
            if (tmp != null) { tmp.text = newText; return; }

            Text legacyText = found.GetComponentInChildren<Text>();
            if (legacyText != null) { legacyText.text = newText; }
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy object tên: " + objectName + " - Hãy kiểm tra lại tên trong Hierarchy!");
        }
    }

    private Transform FindChildRecursive(Transform parent, string exactName)
    {
        if (parent.name == exactName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, exactName);
            if (result != null) return result;
        }
        return null;
    }

    private void FixLayouts()
    {
        VerticalLayoutGroup rootVg = GetOrAdd<VerticalLayoutGroup>(gameObject);
        rootVg.childControlWidth = true; rootVg.childControlHeight = true;
        rootVg.childForceExpandWidth = true; rootVg.childForceExpandHeight = true;

        Transform mainContent = FindChildRecursive(transform, "MainContent");
        if (mainContent)
        {
            HorizontalLayoutGroup hg = GetOrAdd<HorizontalLayoutGroup>(mainContent.gameObject);
            hg.childControlWidth = true; hg.childControlHeight = true;
            hg.childForceExpandWidth = false; hg.childForceExpandHeight = true;
            hg.spacing = 15;

            SetupPanel(FindChildRecursive(mainContent, "Left Panel"), 1f);
            SetupPanel(FindChildRecursive(mainContent, "Mid Panel"), 1.2f);
            SetupPanel(FindChildRecursive(mainContent, "Right Panel"), 1.2f);

            Transform midPanel = FindChildRecursive(mainContent, "Mid Panel");
            if (midPanel)
            {
                Transform craftRow = FindChildRecursive(midPanel, "CraftRow");
                if (craftRow) SetupHorizontalRow(GetOrAdd<HorizontalLayoutGroup>(craftRow.gameObject));

                Transform qtyRow = FindChildRecursive(midPanel, "QtyRow");
                if (qtyRow) SetupHorizontalRow(GetOrAdd<HorizontalLayoutGroup>(qtyRow.gameObject));
            }
        }
    }

    private void SetupPanel(Transform panel, float flexWidth)
    {
        if (panel == null) return;
        VerticalLayoutGroup vg = GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
        vg.childControlWidth = true; vg.childControlHeight = false;
        vg.childForceExpandWidth = true; vg.childForceExpandHeight = false;
        vg.spacing = 15; vg.padding = new RectOffset(15, 15, 15, 15);
        LayoutElement le = GetOrAdd<LayoutElement>(panel.gameObject);
        le.flexibleWidth = flexWidth;
    }

    private void SetupHorizontalRow(HorizontalLayoutGroup hg)
    {
        hg.childControlWidth = false; hg.childControlHeight = false;
        hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
        hg.childAlignment = TextAnchor.MiddleCenter; hg.spacing = 20;
    }

    private T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }
}