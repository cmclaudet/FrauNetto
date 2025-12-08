using System.Collections.Generic;
using UnityEngine;

public class ConveyorBeltGrid : MonoBehaviour
{
    public Vector2Int gridSize;
    public float cellSize;
    public float moveSpeed;
    public float itemSpawnFrequency;
    public ItemDefinition[] itemDefinitions;
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
        if (itemDefinitions == null || itemDefinitions.Length == 0)
            return;

        ItemDefinition itemDef = itemDefinitions[Random.Range(0, itemDefinitions.Length)];
        if (itemDef.gridDefinition == null || itemDef.gridDefinition.Length == 0)
            return;

        // Find spawn position at bottom row
        List<int> validSpawnX = new List<int>();

        for (int x = 0; x < gridSize.x; x++)
        {
            Vector2Int[] cells = GetCellsForGridPosition(x, 0, itemDef.gridDefinition);
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

    void SpawnItem(ItemDefinition itemDef, int gridX)
    {
        Vector3 worldPos = GetWorldPositionFromGrid(gridX, 0);
        spawnItemCount++;
        Item itemObj = Instantiate(itemDef.prefab, worldPos, Quaternion.identity, transform);
        itemObj.name = itemDef.name + "_" + spawnItemCount;
        Debug.Log($"Spawning item {itemObj.name} at gridX={gridX}, worldPos={worldPos}, prefab={itemDef.prefab}");
        itemObj.Init(itemDef.gridDefinition, GetCellsForGridPosition(gridX, 0, itemDef.gridDefinition));
        
        gridManager.OccupyCells(itemObj.CurrentCells);
        activeItems.Add(itemObj);
    }

    void MoveItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            Item item = activeItems[i];

            // Move in local negative x direction relative to this grid's transform
            item.transform.position += transform.TransformDirection(Vector3.left) * moveSpeed * Time.deltaTime;

            // Update grid cells
            Vector2Int[] newCells = gridManager.GetCellsForPosition(
                item.transform.position,
                item.GridDefinition,
                transform,
                cellSize
            );

            // Check if item moved off grid
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
                gridManager.FreeCells(item.CurrentCells);

                // Try to transfer to next grid
                if (nextGrid != null && nextGrid.TryAddItem(item))
                {
                    activeItems.RemoveAt(i);
                }
                else
                {
                    Destroy(item.gameObject);
                    activeItems.RemoveAt(i);
                }
            }
            else if (!AreCellsSame(item.CurrentCells, newCells))
            {
                gridManager.FreeCells(item.CurrentCells);
                gridManager.OccupyCells(newCells);
                item.UpdateCells(newCells);
            }
        }
    }

    public bool TryAddItem(Item item)
    {
        // Get cells for this item in the next grid's coordinate system
        Vector2Int[] newCells = gridManager.GetCellsForPosition(
            item.transform.position,
            item.GridDefinition,
            transform,
            cellSize
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

        // Add to this grid
        gridManager.OccupyCells(newCells);
        item.UpdateCells(newCells);
        activeItems.Add(item);

        return true;
    }

    Vector2Int[] GetCellsForGridPosition(int gridX, int gridY, Vector3Int[] itemShape)
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
        // gridX maps to z-axis (right), gridY maps to negative x-axis (up)
        // Transform is at bottom center, so offset by half grid width
        float worldX = -gridY * cellSize;
        float worldZ = (gridX - gridSize.x * 0.5f) * cellSize;
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
}
