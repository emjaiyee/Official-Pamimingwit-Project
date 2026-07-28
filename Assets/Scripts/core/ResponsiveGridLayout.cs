using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridLayout : MonoBehaviour
{
    public float widthToHeightRatio = 2f; // e.g., 2:1 ratio for a wide rectangle
    public int columns = 2;
    
    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        float parentWidth = rectTransform.rect.width;
        float padding = grid.padding.left + grid.padding.right;
        float spacing = grid.spacing.x * (columns - 1);
        
        float cellWidth = (parentWidth - padding - spacing) / columns;
        grid.cellSize = new Vector2(cellWidth, cellWidth / widthToHeightRatio);
    }
}
