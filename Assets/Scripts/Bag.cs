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

        // Store the cells in the item
        item.Init3D(cells);

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

        // Get the cells that were stored when the item was placed
        Vector3Int[] itemCells = item.CurrentCells3D;

        if (itemCells == null || itemCells.Length == 0)
        {
            Debug.LogError($"Item {item.name} has no stored cells! Cannot remove properly.");
            return null;
        }

        // Check if the item can be removed (no items above it)
        if (!gridManager.CanRemoveItem(item, itemCells))
        {
            Debug.Log($"Cannot remove item {item.name} at grid position ({gridX}, {gridZ}) - items are stacked above it");
            return null;
        }

        // Free the cells
        gridManager.FreeCells(itemCells);

        Debug.Log($"Removed item {item.name} from grid position ({gridX}, {gridZ}), freed {itemCells.Length} cells");

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

    /// <summary>
    /// Validates if an item can be placed at the given XZ grid position and returns the preview position.
    /// Returns true and sets previewPosition if valid, false otherwise.
    /// </summary>
    /// <param name="item">The item to validate placement for</param>
    /// <param name="gridX">X position on the grid</param>
    /// <param name="gridZ">Z position on the grid</param>
    /// <param name="previewPosition">The world position where the item would be placed</param>
    /// <returns>True if placement is valid, false otherwise</returns>
    public bool TryGetPreviewPosition(Item item, int gridX, int gridZ, out Vector3 previewPosition)
    {
        previewPosition = Vector3.zero;
        Vector3Int[] itemShape = item.gridDefinition;
        
        // Check bounds: get min/max X and Z from item's grid definition
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        
        foreach (var cell in itemShape)
        {
            int cellX = gridX + cell.x;
            int cellZ = gridZ + cell.z;
            
            if (cellX < minX) minX = cellX;
            if (cellX > maxX) maxX = cellX;
            if (cellZ < minZ) minZ = cellZ;
            if (cellZ > maxZ) maxZ = cellZ;
        }
        
        // Check if entire item is within bag grid bounds
        if (minX < 0 || maxX >= gridSize.x || minZ < 0 || maxZ >= gridSize.z)
        {
            return false;
        }
        
        // Find the lowest available Y position
        int gridY = gridManager.FindLowestAvailableY(gridX, gridZ, itemShape);
        
        if (gridY < 0)
        {
            return false;
        }
        
        // Calculate the cells this item would occupy
        Vector3Int[] cells = GetCellsForGridPosition(gridX, gridY, gridZ, itemShape);
        
        // Double-check all cells are free (should be guaranteed by FindLowestAvailableY, but be safe)
        if (!gridManager.AreAllCellsFree(cells))
        {
            return false;
        }
        
        // Calculate world position
        previewPosition = GetWorldPositionFromGrid(gridX, gridY, gridZ);
        return true;
    }
}
