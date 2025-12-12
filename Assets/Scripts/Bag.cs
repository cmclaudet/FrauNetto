using UnityEngine;

public class Bag : MonoBehaviour
{
    public Vector3Int gridSize;
    public Color[] baseRowColors;

    private GridManager3D gridManager;
    private Color[] rowColors;

    void Start()
    {
        gridManager = new GridManager3D(gridSize);
        rowColors = GenerateColorGradient(gridSize.y);
        CreateCrateVisual();
    }

    private void CreateCrateVisual()
    {
        GameObject crateParent = new GameObject("CrateVisual");
        crateParent.transform.SetParent(transform);
        crateParent.transform.localPosition = Vector3.zero;

        // Create quads for each exterior face
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    // Skip top faces (y == gridSize.y - 1)

                    // Bottom faces (y == 0)
                    if (y == 0)
                    {
                        CreateQuad(crateParent.transform, x, y, z, Vector3.down, rowColors[0]);
                    }

                    // Front faces (z == 0)
                    if (z == 0)
                    {
                        CreateQuad(crateParent.transform, x, y, z, Vector3.back, rowColors[y]);
                    }

                    // Back faces (z == gridSize.z - 1)
                    if (z == gridSize.z - 1)
                    {
                        CreateQuad(crateParent.transform, x, y, z, Vector3.forward, rowColors[y]);
                    }

                    // Left faces (x == 0)
                    if (x == 0)
                    {
                        CreateQuad(crateParent.transform, x, y, z, Vector3.left, rowColors[y]);
                    }

                    // Right faces (x == gridSize.x - 1)
                    if (x == gridSize.x - 1)
                    {
                        CreateQuad(crateParent.transform, x, y, z, Vector3.right, rowColors[y]);
                    }
                }
            }
        }
    }

    private Color[] GenerateColorGradient(int rowCount)
    {
        Color[] colors = new Color[rowCount];
        Color[] baseColors = baseRowColors;

        if (rowCount <= 4)
        {
            // Use base colors directly
            for (int i = 0; i < rowCount; i++)
            {
                colors[i] = baseColors[i];
            }
        }
        else
        {
            // Interpolate between base colors
            for (int i = 0; i < rowCount; i++)
            {
                float t = i / (float)(rowCount - 1);
                float scaledT = t * (baseColors.Length - 1);
                int lowerIndex = Mathf.FloorToInt(scaledT);
                int upperIndex = Mathf.Min(lowerIndex + 1, baseColors.Length - 1);
                float localT = scaledT - lowerIndex;
                colors[i] = Color.Lerp(baseColors[lowerIndex], baseColors[upperIndex], localT);
            }
        }

        return colors;
    }

    private void CreateQuad(Transform parent, int gridX, int gridY, int gridZ, Vector3 normal, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = $"Quad_{gridX}_{gridY}_{gridZ}_{normal}";
        quad.transform.SetParent(parent);

        // Calculate world position for this grid cell
        float worldX = (gridX - gridSize.x * 0.5f + 0.5f) * Constants.CellSize;
        float worldY = (gridY + 0.5f) * Constants.CellSize;
        float worldZ = (gridZ - gridSize.z * 0.5f + 0.5f) * Constants.CellSize;

        Vector3 localPos = new Vector3(worldX, worldY, worldZ);

        // Offset the quad to the face of the cell
        localPos += normal * Constants.CellSize * 0.5f;

        quad.transform.localPosition = localPos;

        // Rotate quad to face outward
        if (normal == Vector3.up)
            quad.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        else if (normal == Vector3.down)
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
        else if (normal == Vector3.forward)
            quad.transform.localRotation = Quaternion.Euler(0, 0, 0);
        else if (normal == Vector3.back)
            quad.transform.localRotation = Quaternion.Euler(0, 180, 0);
        else if (normal == Vector3.left)
            quad.transform.localRotation = Quaternion.Euler(0, -90, 0);
        else if (normal == Vector3.right)
            quad.transform.localRotation = Quaternion.Euler(0, 90, 0);

        // Scale quad to match cell size
        quad.transform.localScale = new Vector3(Constants.CellSize, Constants.CellSize, 1);

        // Apply color
        Renderer renderer = quad.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;

        // Remove collider if present
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    public int GetOccupiedCellCount()
    {
        return gridManager.GetOccupiedCellCount();
    }

    public int GetTotalCellCount()
    {
        return gridManager.GetTotalCellCount();
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

        // Apply tint based on the lowest Y position
        int lowestY = GetLowestYFromCells(cells);
        item.SetTint(GetColorForRow(lowestY));

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

        // Reset tint when removed from bag
        item.ResetTint();

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
            item.ResetTint();
            return false;
        }

        // Calculate the cells this item would occupy
        Vector3Int[] cells = GetCellsForGridPosition(gridX, gridY, gridZ, itemShape);

        // Double-check all cells are free (should be guaranteed by FindLowestAvailableY, but be safe)
        if (!gridManager.AreAllCellsFree(cells))
        {
            item.ResetTint();
            return false;
        }

        // Calculate world position
        previewPosition = GetWorldPositionFromGrid(gridX, gridY, gridZ);

        // Apply preview tint based on the lowest Y position
        int lowestY = GetLowestYFromCells(cells);
        item.SetTint(GetColorForRow(lowestY));

        return true;
    }

    private int GetLowestYFromCells(Vector3Int[] cells)
    {
        int lowestY = int.MaxValue;
        foreach (var cell in cells)
        {
            if (cell.y < lowestY)
                lowestY = cell.y;
        }
        return lowestY;
    }

    public Color GetColorForRow(int y)
    {
        if (rowColors == null || rowColors.Length == 0)
            return Color.white;

        if (y < 0)
            y = 0;
        if (y >= rowColors.Length)
            y = rowColors.Length - 1;

        return rowColors[y];
    }
}
