using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GridAutoSizer : MonoBehaviour
{
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private RectTransform container;

    [Header("Layout")]
    public int columns = 3;
    public float spacingX = 24f;
    public float spacingY = 24f;
    public float padding = 16f;
    public float cellHeight = 320f;

    private void Reset()
    {
        grid = GetComponent<GridLayoutGroup>();
        container = GetComponent<RectTransform>();
    }

    private void OnEnable() => Apply();
    private void OnRectTransformDimensionsChange() => Apply();
    private void Update() => Apply();

    private void Apply()
    {
        if (!grid || !container || columns <= 0) return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = new Vector2(spacingX, spacingY);
        grid.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);

        float width = container.rect.width - grid.padding.left - grid.padding.right;
        float totalSpacing = spacingX * (columns - 1);
        float cellWidth = (width - totalSpacing) / columns;

        if (cellWidth < 10) return;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }
}
