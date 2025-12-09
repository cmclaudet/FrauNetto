using UnityEngine;

public class Bag : MonoBehaviour
{
    public Vector3Int gridSize;

    private GridManager3D gridManager;

    void Start()
    {
        gridManager = new GridManager3D(gridSize);
    }

    void Update()
    {
        
    }

    /// <summary>
    /// Adds an item to the bag at the specified XZ grid position.
    /// The item will be placed at the lowest available Y position where it doesn't collide.
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <param name="gridX">X position on the grid</param>
    /// <param name="gridZ">Z position on the grid</param>
    /// <returns>True if the item was successfully added, false otherwise</returns>
    public bool TryAddItem(Item item, int gridX, int gridZ)
    {
        Vector3Int[] itemShape = item.gridDefinition;
        
        // Find the lowest available Y position
        int gridY = gridManager.FindLowestAvailableY(gridX, gridZ, itemShape);
        
        if (gridY < 0)
        {
            Debug.Log($"Cannot add item {item.name} at grid position ({gridX}, {gridZ}) - no space available");
            return false;
        }

        // Calculate the cells this item will occupy
        Vector3Int[] cells = GetCellsForGridPosition(gridX, gridY, gridZ, itemShape);
        
        // Occupy the cells
        gridManager.OccupyCells(cells, item);
        
        // Update item position
        Vector3 worldPos = GetWorldPositionFromGrid(gridX, gridY, gridZ);
        item.transform.position = worldPos;
        item.transform.SetParent(transform, true);
        
        Debug.Log($"Added item {item.name} at grid position ({gridX}, {gridY}, {gridZ}), worldPos={worldPos}");
        
        return true;
    }

    /// <summary>
    /// Removes an item from the bag at the specified XZ grid position.
    /// Finds the topmost item at this position and removes it if no items are above it.
    /// </summary>
    /// <param name="gridX">X position on the grid</param>
    /// <param name="gridZ">Z position on the grid</param>
    /// <returns>The removed item if successful, null otherwise</returns>
    public Item TryRemoveItem(int gridX, int gridZ)
    {
        // Find the topmost item at this XZ position
        Item item = gridManager.FindTopMostItemAtXZ(gridX, gridZ);
        
        if (item == null)
        {
            Debug.Log($"No item found at grid position ({gridX}, {gridZ})");
            return null;
        }

        // Get all cells occupied by this item (stored in Item.CurrentCells as Vector2Int, need to reconstruct as Vector3Int)
        // For now, we need to calculate the cells from the item's position
        Vector3 localPos = transform.InverseTransformPoint(item.transform.position);
        int itemGridX = Mathf.RoundToInt(localPos.x / Constants.CellSize + gridSize.x * 0.5f);
        int itemGridY = Mathf.RoundToInt(localPos.y / Constants.CellSize);
        int itemGridZ = Mathf.RoundToInt(localPos.z / Constants.CellSize);
        
        Vector3Int[] itemCells = GetCellsForGridPosition(itemGridX, itemGridY, itemGridZ, item.gridDefinition);
        
        // Check if the item can be removed (no items above it)
        if (!gridManager.CanRemoveItem(item, itemCells))
        {
            Debug.Log($"Cannot remove item {item.name} at grid position ({gridX}, {gridZ}) - items are stacked above it");
            return null;
        }

        // Free the cells
        gridManager.FreeCells(itemCells);
        
        Debug.Log($"Removed item {item.name} from grid position ({gridX}, {gridZ})");
        
        return item;
    }

    /// <summary>
    /// Calculates which grid cells an item with the given shape would occupy at the specified grid position.
    /// </summary>
    private Vector3Int[] GetCellsForGridPosition(int gridX, int gridY, int gridZ, Vector3Int[] itemShape)
    {
        Vector3Int[] cells = new Vector3Int[itemShape.Length];
        for (int i = 0; i < itemShape.Length; i++)
        {
            cells[i] = new Vector3Int(
                gridX + itemShape[i].x,
                gridY + itemShape[i].y,
                gridZ + itemShape[i].z
            );
        }
        return cells;
    }

    /// <summary>
    /// Converts a grid position to world space.
    /// The object's transform position defines the center of the grid on the XZ plane,
    /// and the Y position defines the very bottom of the grid.
    /// </summary>
    private Vector3 GetWorldPositionFromGrid(int gridX, int gridY, int gridZ)
    {
        // gridX maps to x-axis, gridY maps to y-axis, gridZ maps to z-axis
        // Transform center is at grid center on XZ plane, bottom on Y axis
        float worldX = (gridX - gridSize.x * 0.5f) * Constants.CellSize;
        float worldY = gridY * Constants.CellSize;
        float worldZ = (gridZ - gridSize.z * 0.5f) * Constants.CellSize;
        
        return transform.TransformPoint(new Vector3(worldX, worldY, worldZ));
    }
}
