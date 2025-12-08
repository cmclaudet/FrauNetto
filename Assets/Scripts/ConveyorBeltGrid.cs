using System.Collections.Generic;
using UnityEngine;

public class ConveyorBeltGrid : MonoBehaviour
{
    public Vector2Int gridSize;
    public float cellSize;
    public float moveSpeed;
    public float itemSpawnFrequency;
    public ItemDefinition[] itemDefinitions;

    private GridManager gridManager;
    private List<ConveyorItem> activeItems = new List<ConveyorItem>();
    private float spawnTimer;
    private float spawnItemCount;

    void Start()
    {
        gridManager = new GridManager(gridSize);
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= itemSpawnFrequency)
        {
            spawnTimer = 0f;
            TrySpawnItem();
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
        GameObject itemObj = Instantiate(itemDef.prefab, worldPos, Quaternion.identity, transform);
        itemObj.name = itemDef.name + "_" + spawnItemCount;
        Debug.Log($"Spawning item {itemObj.name} at gridX={gridX}, worldPos={worldPos}, prefab={itemDef.prefab}");

        ConveyorItem item = new ConveyorItem
        {
            gameObject = itemObj,
            itemDefinition = itemDef,
            currentCells = GetCellsForGridPosition(gridX, 0, itemDef.gridDefinition)
        };

        gridManager.OccupyCells(item.currentCells);
        activeItems.Add(item);
    }

    void MoveItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            ConveyorItem item = activeItems[i];

            // Move in negative x direction (upward)
            item.gameObject.transform.position += Vector3.left * moveSpeed * Time.deltaTime;

            // Update grid cells
            Vector2Int[] newCells = gridManager.GetCellsForPosition(
                item.gameObject.transform.position,
                item.itemDefinition.gridDefinition,
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
                gridManager.FreeCells(item.currentCells);
                Destroy(item.gameObject);
                activeItems.RemoveAt(i);
            }
            else if (!AreCellsSame(item.currentCells, newCells))
            {
                gridManager.FreeCells(item.currentCells);
                gridManager.OccupyCells(newCells);
                item.currentCells = newCells;
            }
        }
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

    private class ConveyorItem
    {
        public GameObject gameObject;
        public ItemDefinition itemDefinition;
        public Vector2Int[] currentCells;
    }
}
