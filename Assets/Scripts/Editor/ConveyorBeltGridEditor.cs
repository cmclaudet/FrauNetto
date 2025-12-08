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
        // Horizontal lines (along Z axis / width)
        for (int y = 0; y <= grid.gridSize.y; y++)
        {
            float worldX = -y * Constants.CellSize;
            float startZ = -grid.gridSize.x * 0.5f * Constants.CellSize;
            float endZ = grid.gridSize.x * 0.5f * Constants.CellSize;

            Vector3 start = transform.TransformPoint(new Vector3(worldX, 0, startZ));
            Vector3 end = transform.TransformPoint(new Vector3(worldX, 0, endZ));

            Handles.DrawLine(start, end);
        }

        // Vertical lines (along X axis / height)
        for (int x = 0; x <= grid.gridSize.x; x++)
        {
            float worldZ = (x - grid.gridSize.x * 0.5f) * Constants.CellSize;
            float startX = 0;
            float endX = -grid.gridSize.y * Constants.CellSize;

            Vector3 start = transform.TransformPoint(new Vector3(startX, 0, worldZ));
            Vector3 end = transform.TransformPoint(new Vector3(endX, 0, worldZ));

            Handles.DrawLine(start, end);
        }

        // Draw labels at corners
        Handles.color = Color.white;

        // Bottom left (0, 0)
        Vector3 bottomLeft = transform.TransformPoint(new Vector3(0, 0, -grid.gridSize.x * 0.5f * Constants.CellSize));
        Handles.Label(bottomLeft, "(0, 0)");

        // Bottom right (gridSize.x-1, 0)
        Vector3 bottomRight = transform.TransformPoint(new Vector3(0, 0, grid.gridSize.x * 0.5f * Constants.CellSize));
        Handles.Label(bottomRight, $"({grid.gridSize.x-1}, 0)");

        // Top left (0, gridSize.y-1)
        Vector3 topLeft = transform.TransformPoint(new Vector3(-grid.gridSize.y * Constants.CellSize, 0, -grid.gridSize.x * 0.5f * Constants.CellSize));
        Handles.Label(topLeft, $"(0, {grid.gridSize.y-1})");

        // Top right (gridSize.x-1, gridSize.y-1)
        Vector3 topRight = transform.TransformPoint(new Vector3(-grid.gridSize.y * Constants.CellSize, 0, grid.gridSize.x * 0.5f * Constants.CellSize));
        Handles.Label(topRight, $"({grid.gridSize.x-1}, {grid.gridSize.y-1})");
    }
}
