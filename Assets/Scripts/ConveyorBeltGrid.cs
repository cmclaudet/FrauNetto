using System.Collections.Generic;
using UnityEngine;

public class ConveyorBeltGrid : MonoBehaviour
{
    public Vector2Int gridSize;
    public float moveSpeed;
    public float itemSpawnFrequency;
    public Item[] itemPrefabs;
    public ConveyorBeltGrid nextGrid;
    public bool enableSpawning = true;

    private GridManager gridManager;
    private List<Item> activeItems = new List<Item>();
    private float spawnTimer;
    private float spawnItemCount;

    void Start()
    {
        gridManager = new GridManager(gridSize);
    }

    void Update()
    {
        if (enableSpawning)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= itemSpawnFrequency)
            {
                spawnTimer = 0f;
                TrySpawnItem();
            }
        }

        MoveItems();
    }

    void TrySpawnItem()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
            return;

        Item itemDef = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        if (itemDef.gridDefinition.Length == 0)
            return;

        // Find spawn position at bottom row
        List<int> validSpawnX = new List<int>();

        for (int x = 0; x < gridSize.x; x++)
        {
            Vector2Int[] cells = GetCellsForGridPosition(x, 0, itemDef.GetFlatGridDefinition());
            if (gridManager.AreAllCellsFree(cells))
            {
                validSpawnX.Add(x);
            }
        }

        if (validSpawnX.Count > 0)
        {
            int spawnX = validSpawnX[Random.Range(0, validSpawnX.Count)];
            SpawnItem(itemDef, spawnX);
        }
    }

    void SpawnItem(Item itemDef, int gridX)
    {
        Vector3 worldPos = GetWorldPositionFromGrid(gridX, 0);
        spawnItemCount++;
        Item itemObj = Instantiate(itemDef, worldPos, Quaternion.identity, transform);
        itemObj.name = itemDef.name + "_" + spawnItemCount;
        Debug.Log($"Spawning item {itemObj.name} at gridX={gridX}, worldPos={worldPos}, prefab={itemDef}");
        itemObj.Init(GetCellsForGridPosition(gridX, 0, itemObj.GetFlatGridDefinition()));
        
        gridManager.OccupyCells(itemObj.CurrentCells);
        activeItems.Add(itemObj);
    }

    void MoveItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            Item item = activeItems[i];

            // Skip static items
            if (item.IsStatic)
                continue;

            // Calculate new position after movement
            Vector3 newPosition = item.transform.position + transform.TransformDirection(Vector3.forward) * moveSpeed * Time.deltaTime;

            // Get cells at new position
            Vector2Int[] newCells = gridManager.GetCellsForPosition(
                newPosition,
                item.GetFlatGridDefinition(),
                transform,
                Constants.CellSize
            );

            // Check if cells have changed (crossed midpoint)
            if (!AreCellsSame(item.CurrentCells, newCells))
            {
                // Check if new cells would be off grid
                bool offGrid = true;
                foreach (var cell in newCells)
                {
                    if (gridManager.IsCellInBounds(cell))
                    {
                        offGrid = false;
                        break;
                    }
                }

                if (offGrid)
                {
                    // Try to transfer to next grid
                    if (nextGrid != null)
                    {
                        var oldCells = item.CurrentCells;
                        if (nextGrid.TryAddItem(item, newPosition))
                        {
                            Debug.Log($"Moved item {item.name} to next grid");
                            gridManager.FreeCells(oldCells);
                            activeItems.RemoveAt(i);
                        }
                        else
                        {
                            // Haven't reached next grid yet, apply movement
                            item.transform.position = newPosition;
                        }
                    }
                    else
                    {
                        // No next grid, snap to current cell position and make static
                        Debug.Log($"Item {item.name} is off grid, making static");
                        item.transform.position = GetWorldPositionFromCells(item.CurrentCells);
                        item.MakeStatic();
                    }
                }
                else
                {
                    // Check if new cells would collide with any occupied cells
                    // (excluding cells currently occupied by this item)
                    bool wouldCollide = false;
                    foreach (var cell in newCells)
                    {
                        if (gridManager.IsCellOccupied(cell) && !IsCellInArray(cell, item.CurrentCells))
                        {
                            wouldCollide = true;
                            break;
                        }
                    }

                    if (wouldCollide)
                    {
                        // Snap to current cell position and make static
                        Debug.Log($"Item {item.name} would collide with occupied cell, making static");
                        item.transform.position = GetWorldPositionFromCells(item.CurrentCells);
                        item.MakeStatic();
                    }
                    else
                    {
                        // Move to new cells
                        item.transform.position = newPosition;
                        gridManager.FreeCells(item.CurrentCells);
                        gridManager.OccupyCells(newCells);
                        item.UpdateCells(newCells);
                    }
                }
            }
            else
            {
                // Cells haven't changed (haven't crossed midpoint), apply movement
                item.transform.position = newPosition;
            }
        }
    }

    Vector3 GetWorldPositionFromCells(Vector2Int[] cells)
    {
        // Calculate the center position of the occupied cells
        // Find the base cell (minimum x and y)
        int minX = cells[0].x;
        int minY = cells[0].y;
        foreach (var cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }

        // The base cell corresponds to grid position (0,0) in gridDefinition
        return GetWorldPositionFromGrid(minX, minY);
    }

    public bool TryAddItem(Item item, Vector3 newPosition)
    {
        // Get cells for this item in the next grid's coordinate system at the new position
        Vector2Int[] newCells = gridManager.GetCellsForPosition(
            newPosition,
            item.GetFlatGridDefinition(),
            transform,
            Constants.CellSize
        );

        // Check if any cells are in bounds for this grid
        bool anyInBounds = false;
        foreach (var cell in newCells)
        {
            if (gridManager.IsCellInBounds(cell))
            {
                anyInBounds = true;
                break;
            }
        }

        if (!anyInBounds)
            return false;

        // Check if cells are free
        if (!gridManager.AreAllCellsFree(newCells))
            return false;

        // Re-parent item to this grid
        item.transform.SetParent(transform, true);
        item.transform.localRotation = Quaternion.identity;

        // Update item position to the new position
        item.transform.position = newPosition;

        // Add to this grid
        gridManager.OccupyCells(newCells);
        item.UpdateCells(newCells);
        activeItems.Add(item);

        return true;
    }

    Vector2Int[] GetCellsForGridPosition(int gridX, int gridY, Vector2Int[] itemShape)
    {
        Vector2Int[] cells = new Vector2Int[itemShape.Length];
        for (int i = 0; i < itemShape.Length; i++)
        {
            cells[i] = new Vector2Int(gridX + itemShape[i].x, gridY + itemShape[i].y);
        }
        return cells;
    }

    Vector3 GetWorldPositionFromGrid(int gridX, int gridY)
    {
        // gridX maps to x-axis (right), gridY maps to z-axis (up)
        // Transform is at bottom center, so offset by half grid width
        float worldX = (gridX - gridSize.x * 0.5f) * Constants.CellSize;
        float worldZ = gridY * Constants.CellSize;
        return transform.TransformPoint(new Vector3(worldX, 0, worldZ));
    }

    bool AreCellsSame(Vector2Int[] cells1, Vector2Int[] cells2)
    {
        if (cells1.Length != cells2.Length)
            return false;

        for (int i = 0; i < cells1.Length; i++)
        {
            if (cells1[i] != cells2[i])
                return false;
        }
        return true;
    }

    bool IsCellInArray(Vector2Int cell, Vector2Int[] cells)
    {
        foreach (var c in cells)
        {
            if (c == cell)
                return true;
        }
        return false;
    }
}
