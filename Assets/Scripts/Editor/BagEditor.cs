using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Bag))]
public class BagEditor : Editor
{
    void OnSceneGUI()
    {
        Bag bag = (Bag)target;

        if (bag.gridSize.x <= 0 || bag.gridSize.y <= 0 || bag.gridSize.z <= 0 || Constants.CellSize <= 0)
            return;

        Transform transform = bag.transform;

        Handles.color = Color.cyan;

        // Draw grid lines for each Y level
        for (int y = 0; y <= bag.gridSize.y; y++)
        {
            float worldY = y * Constants.CellSize;

            // Horizontal lines (along X axis)
            for (int z = 0; z <= bag.gridSize.z; z++)
            {
                float worldZ = (z - bag.gridSize.z * 0.5f) * Constants.CellSize;
                float startX = -bag.gridSize.x * 0.5f * Constants.CellSize;
                float endX = bag.gridSize.x * 0.5f * Constants.CellSize;

                Vector3 start = transform.TransformPoint(new Vector3(startX, worldY, worldZ));
                Vector3 end = transform.TransformPoint(new Vector3(endX, worldY, worldZ));

                Handles.DrawLine(start, end);
            }

            // Vertical lines (along Z axis)
            for (int x = 0; x <= bag.gridSize.x; x++)
            {
                float worldX = (x - bag.gridSize.x * 0.5f) * Constants.CellSize;
                float startZ = -bag.gridSize.z * 0.5f * Constants.CellSize;
                float endZ = bag.gridSize.z * 0.5f * Constants.CellSize;

                Vector3 start = transform.TransformPoint(new Vector3(worldX, worldY, startZ));
                Vector3 end = transform.TransformPoint(new Vector3(worldX, worldY, endZ));

                Handles.DrawLine(start, end);
            }
        }

        // Draw vertical edges connecting the Y levels
        for (int x = 0; x <= bag.gridSize.x; x++)
        {
            for (int z = 0; z <= bag.gridSize.z; z++)
            {
                float worldX = (x - bag.gridSize.x * 0.5f) * Constants.CellSize;
                float worldZ = (z - bag.gridSize.z * 0.5f) * Constants.CellSize;
                float startY = 0;
                float endY = bag.gridSize.y * Constants.CellSize;

                Vector3 start = transform.TransformPoint(new Vector3(worldX, startY, worldZ));
                Vector3 end = transform.TransformPoint(new Vector3(worldX, endY, worldZ));

                Handles.DrawLine(start, end);
            }
        }

        // Draw labels at bottom corners
        Handles.color = Color.white;

        // Bottom front left (0, 0, 0)
        Vector3 bottomFrontLeft = transform.TransformPoint(new Vector3(-bag.gridSize.x * 0.5f * Constants.CellSize, 0, -bag.gridSize.z * 0.5f * Constants.CellSize));
        Handles.Label(bottomFrontLeft, "(0, 0, 0)");

        // Bottom front right (gridSize.x-1, 0, 0)
        Vector3 bottomFrontRight = transform.TransformPoint(new Vector3(bag.gridSize.x * 0.5f * Constants.CellSize, 0, -bag.gridSize.z * 0.5f * Constants.CellSize));
        Handles.Label(bottomFrontRight, $"({bag.gridSize.x-1}, 0, 0)");

        // Bottom back left (0, 0, gridSize.z-1)
        Vector3 bottomBackLeft = transform.TransformPoint(new Vector3(-bag.gridSize.x * 0.5f * Constants.CellSize, 0, bag.gridSize.z * 0.5f * Constants.CellSize));
        Handles.Label(bottomBackLeft, $"(0, 0, {bag.gridSize.z-1})");

        // Bottom back right (gridSize.x-1, 0, gridSize.z-1)
        Vector3 bottomBackRight = transform.TransformPoint(new Vector3(bag.gridSize.x * 0.5f * Constants.CellSize, 0, bag.gridSize.z * 0.5f * Constants.CellSize));
        Handles.Label(bottomBackRight, $"({bag.gridSize.x-1}, 0, {bag.gridSize.z-1})");

        // Top front left (0, gridSize.y-1, 0)
        Vector3 topFrontLeft = transform.TransformPoint(new Vector3(-bag.gridSize.x * 0.5f * Constants.CellSize, bag.gridSize.y * Constants.CellSize, -bag.gridSize.z * 0.5f * Constants.CellSize));
        Handles.Label(topFrontLeft, $"(0, {bag.gridSize.y-1}, 0)");

        // Top back right (gridSize.x-1, gridSize.y-1, gridSize.z-1)
        Vector3 topBackRight = transform.TransformPoint(new Vector3(bag.gridSize.x * 0.5f * Constants.CellSize, bag.gridSize.y * Constants.CellSize, bag.gridSize.z * 0.5f * Constants.CellSize));
        Handles.Label(topBackRight, $"({bag.gridSize.x-1}, {bag.gridSize.y-1}, {bag.gridSize.z-1})");
    }
}
