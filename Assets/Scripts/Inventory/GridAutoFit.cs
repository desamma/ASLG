using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GridAutoFit - T? ??ng fit kích th??c khe item theo container.
/// Attach vào object ch?a GridLayoutGroup.
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class GridAutoFit : MonoBehaviour
{
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 2;
    [SerializeField] private float spacing = 6f;

    private GridLayoutGroup grid;
    private RectTransform rect;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rect = GetComponent<RectTransform>();
    }

    private void OnRectTransformDimensionsChange() => Fit();

    private void Fit()
    {
        if (grid == null || rect == null) return;

        var pad = grid.padding;
        float usableW = rect.rect.width - pad.left - pad.right - spacing * (columns - 1);
        float usableH = rect.rect.height - pad.top - pad.bottom - spacing * (rows - 1);

        float w = usableW / columns;
        float h = usableH / rows;

        if (w > 0 && h > 0)
            grid.cellSize = new Vector2(w, h);
    }
}