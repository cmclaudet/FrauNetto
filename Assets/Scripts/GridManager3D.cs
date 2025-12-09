using System.Collections.Generic;
using UnityEngine;

public class GridManager3D
{
    private Vector3Int gridSize;
    private Item[,,] occupiedCells;

    public GridManager3D(Vector3Int gridSize)
    {
        this.gridSize = gridSize;
        occupiedCells = new Item[gridSize.x, gridSize.y, gridSize.z];
    }

    public bool AreAllCellsFree(Vector3Int[] cells)
    {
        foreach (var cell in cells)
        {
            if (!IsCellInBounds(cell) || IsCellOccupied(cell))
                return false;
        }
        return true;
    }

    public void OccupyCells(Vector3Int[] cells, Item item)
    {
        foreach (var cell in cells)
        {
            if (IsCellInBounds(cell))
                occupiedCells[cell.x, cell.y, cell.z] = item;
        }
    }

    public void FreeCells(Vector3Int[] cells)
    {
        foreach (var cell in cells)
        {
            if (IsCellInBounds(cell))
                occupiedCells[cell.x, cell.y, cell.z] = null;
        }
    }

    public bool IsCellOccupied(Vector3Int cell)
    {
        if (!IsCellInBounds(cell))
            return false;
        return occupiedCells[cell.x, cell.y, cell.z] != null;
    }

    public bool IsCellInBounds(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < gridSize.x && 
               cell.y >= 0 && cell.y < gridSize.y && 
               cell.z >= 0 && cell.z < gridSize.z;
    }

    public Item GetItemAtCell(Vector3Int cell)
    {
        if (!IsCellInBounds(cell))
            return null;
        return occupiedCells[cell.x, cell.y, cell.z];
    }

    public int FindLowestAvailableY(int gridX, int gridZ, Vector3Int[] itemShape)
    {
        // Get the XZ footprint of the item
        HashSet<Vector2Int> xzFootprint = new HashSet<Vector2Int>();
        foreach (var cell in itemShape)
        {
            xzFootprint.Add(new Vector2Int(gridX + cell.x, gridZ + cell.z));
        }

        // Find the lowest Y where all cells of the item fit
        for (int y = 0; y < gridSize.y; y++)
        {
            if (CanPlaceItemAtY(gridX, y, gridZ, itemShape, xzFootprint))
                return y;
        }

        return -1; // No valid Y position found
    }

    private bool CanPlaceItemAtY(int gridX, int gridY, int gridZ, Vector3Int[] itemShape, HashSet<Vector2Int> xzFootprint)
    {
        // First check if all cells of the item at this Y are free
        foreach (var cell in itemShape)
        {
            Vector3Int checkCell = new Vector3Int(gridX + cell.x, gridY + cell.y, gridZ + cell.z);
            if (!IsCellInBounds(checkCell) || IsCellOccupied(checkCell))
                return false;
        }

        // Find the maximum Y occupied by this item
        int maxItemY = gridY;
        foreach (var cell in itemShape)
        {
            int cellY = gridY + cell.y;
            if (cellY > maxItemY)
                maxItemY = cellY;
        }

        // Check that all cells above the item's XZ footprint are free up to maxItemY
        foreach (var xz in xzFootprint)
        {
            for (int y = 0; y < gridY; y++)
            {
                Vector3Int checkCell = new Vector3Int(xz.x, y, xz.y);
                if (IsCellOccupied(checkCell))
                    return false;
            }
        }

        return true;
    }

    public Item FindTopMostItemAtXZ(int gridX, int gridZ)
    {
        Item topMostItem = null;
        int highestY = -1;

        // Search from top to bottom to find the highest occupied cell
        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            Vector3Int cell = new Vector3Int(gridX, y, gridZ);
            if (IsCellInBounds(cell) && IsCellOccupied(cell))
            {
                topMostItem = occupiedCells[cell.x, cell.y, cell.z];
                highestY = y;
                break;
            }
        }

        return topMostItem;
    }

    public bool CanRemoveItem(Item item, Vector3Int[] itemCells)
    {
        // Find all XZ positions occupied by this item
        HashSet<Vector2Int> itemXZPositions = new HashSet<Vector2Int>();
        int maxItemY = -1;

        foreach (var cell in itemCells)
        {
            itemXZPositions.Add(new Vector2Int(cell.x, cell.z));
            if (cell.y > maxItemY)
                maxItemY = cell.y;
        }

        // Check if there are any occupied cells above the item's maximum Y
        foreach (var xz in itemXZPositions)
        {
            for (int y = maxItemY + 1; y < gridSize.y; y++)
            {
                Vector3Int checkCell = new Vector3Int(xz.x, y, xz.y);
                if (IsCellOccupied(checkCell))
                    return false;
            }
        }

        return true;
    }
}
