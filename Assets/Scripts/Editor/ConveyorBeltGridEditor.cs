using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ConveyorBeltGrid))]
public class ConveyorBeltGridEditor : Editor
{
    void OnSceneGUI()
    {
        ConveyorBeltGrid grid = (ConveyorBeltGrid)target;

        if (grid.gridSize.x <= 0 || grid.gridSize.y <= 0 || Constants.CellSize <= 0)
            return;

        Transform transform = grid.transform;
        Vector3 gridOrigin = transform.position;

        Handles.color = Color.cyan;

        // Draw grid lines
        // Horizontal lines (along X axis / width)
        for (int y = 0; y <= grid.gridSize.y; y++)
        {
            float worldZ = y * Constants.CellSize;
            float startX = -grid.gridSize.x * 0.5f * Constants.CellSize;
            float endX = grid.gridSize.x * 0.5f * Constants.CellSize;

            Vector3 start = transform.TransformPoint(new Vector3(startX, 0, worldZ));
            Vector3 end = transform.TransformPoint(new Vector3(endX, 0, worldZ));

            Handles.DrawLine(start, end);
        }

        // Vertical lines (along Z axis / height)
        for (int x = 0; x <= grid.gridSize.x; x++)
        {
            float worldX = (x - grid.gridSize.x * 0.5f) * Constants.CellSize;
            float startZ = 0;
            float endZ = grid.gridSize.y * Constants.CellSize;

            Vector3 start = transform.TransformPoint(new Vector3(worldX, 0, startZ));
            Vector3 end = transform.TransformPoint(new Vector3(worldX, 0, endZ));

            Handles.DrawLine(start, end);
        }

        // Draw labels at corners
        Handles.color = Color.white;

        // Bottom left (0, 0)
        Vector3 bottomLeft = transform.TransformPoint(new Vector3(-grid.gridSize.x * 0.5f * Constants.CellSize, 0, 0));
        Handles.Label(bottomLeft, "(0, 0)");

        // Bottom right (gridSize.x-1, 0)
        Vector3 bottomRight = transform.TransformPoint(new Vector3(grid.gridSize.x * 0.5f * Constants.CellSize, 0, 0));
        Handles.Label(bottomRight, $"({grid.gridSize.x-1}, 0)");

        // Top left (0, gridSize.y-1)
        Vector3 topLeft = transform.TransformPoint(new Vector3(-grid.gridSize.x * 0.5f * Constants.CellSize, 0, grid.gridSize.y * Constants.CellSize));
        Handles.Label(topLeft, $"(0, {grid.gridSize.y-1})");

        // Top right (gridSize.x-1, gridSize.y-1)
        Vector3 topRight = transform.TransformPoint(new Vector3(grid.gridSize.x * 0.5f * Constants.CellSize, 0, grid.gridSize.y * Constants.CellSize));
        Handles.Label(topRight, $"({grid.gridSize.x-1}, {grid.gridSize.y-1})");

        // Draw occupied cells
        DrawOccupiedCells(grid, transform);
    }

    void DrawOccupiedCells(ConveyorBeltGrid grid, Transform transform)
    {
        // Access the grid manager through reflection to get occupied cells
        var gridManagerField = typeof(ConveyorBeltGrid).GetField("gridManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (gridManagerField == null)
            return;

        var gridManager = gridManagerField.GetValue(grid);
        if (gridManager == null)
            return;

        var gridManagerType = gridManager.GetType();
        var isCellOccupiedMethod = gridManagerType.GetMethod("IsCellOccupied");
        if (isCellOccupiedMethod == null)
            return;

        Handles.color = new Color(1f, 0.5f, 0f, 0.3f); // Semi-transparent orange

        // Iterate through all cells in the 2D grid
        for (int x = 0; x < grid.gridSize.x; x++)
        {
            for (int y = 0; y < grid.gridSize.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                bool isOccupied = (bool)isCellOccupiedMethod.Invoke(gridManager, new object[] { cell });

                if (isOccupied)
                {
                    // Calculate cell center in local space
                    // gridX maps to x-axis (right), gridY maps to z-axis (up)
                    float worldX = (x - grid.gridSize.x * 0.5f + 0.5f) * Constants.CellSize;
                    float worldZ = (y + 0.5f) * Constants.CellSize;

                    Vector3 cellCenter = transform.TransformPoint(new Vector3(worldX, 0, worldZ));

                    // Draw a cube for each occupied cell (flat on the XZ plane)
                    Handles.CubeHandleCap(0, cellCenter, transform.rotation, Constants.CellSize * 0.9f, EventType.Repaint);
                }
            }
        }
    }
}
