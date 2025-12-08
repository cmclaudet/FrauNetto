using System.Collections.Generic;
using UnityEngine;

public class GridManager
{
    private Vector2Int gridSize;
    private bool[,] occupiedCells;

    public GridManager(Vector2Int gridSize)
    {
        this.gridSize = gridSize;
        occupiedCells = new bool[gridSize.x, gridSize.y];
    }

    public bool AreAllCellsFree(Vector2Int[] cells)
    {
        foreach (var cell in cells)
        {
            if (!IsCellInBounds(cell) || IsCellOccupied(cell))
                return false;
        }
        return true;
    }

    public void OccupyCells(Vector2Int[] cells)
    {
        foreach (var cell in cells)
        {
            if (IsCellInBounds(cell))
                occupiedCells[cell.x, cell.y] = true;
        }
    }

    public void FreeCells(Vector2Int[] cells)
    {
        foreach (var cell in cells)
        {
            if (IsCellInBounds(cell))
                occupiedCells[cell.x, cell.y] = false;
        }
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        if (!IsCellInBounds(cell))
            return false;
        return occupiedCells[cell.x, cell.y];
    }

    public bool IsCellInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y;
    }

    public Vector2Int[] GetCellsForPosition(Vector3 worldPosition, Vector3Int[] itemShape, Transform gridTransform, float cellSize)
    {
        Vector3 localPos = gridTransform.InverseTransformPoint(worldPosition);

        // Convert to grid coordinates
        // x-axis (negative = up) maps to y in grid, z-axis (positive = right) maps to x in grid
        // Account for center offset: reverse the (gridX - gridSize.x * 0.5f) offset applied in GetWorldPositionFromGrid
        int gridX = Mathf.RoundToInt(localPos.z / cellSize + gridSize.x * 0.5f);
        int gridY = Mathf.RoundToInt(-localPos.x / cellSize);

        Vector2Int[] cells = new Vector2Int[itemShape.Length];
        for (int i = 0; i < itemShape.Length; i++)
        {
            // itemShape is defined with x=right, y=up in grid space
            cells[i] = new Vector2Int(gridX + itemShape[i].x, gridY + itemShape[i].y);
        }
        return cells;
    }
}