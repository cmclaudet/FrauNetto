using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(Item))]
public class ItemEditor : Editor
{
    void OnSceneGUI()
    {
        Item item = (Item)target;

        if (Constants.CellSize <= 0 || item.gridDefinition == null || item.gridDefinition.Length == 0)
            return;

        Transform transform = item.transform;

        // Calculate grid extents from gridDefinition
        int minX = item.gridDefinition.Min(v => v.x);
        int maxX = item.gridDefinition.Max(v => v.x);
        int minY = item.gridDefinition.Min(v => v.y);
        int maxY = item.gridDefinition.Max(v => v.y);
        int minZ = item.gridDefinition.Min(v => v.z);
        int maxZ = item.gridDefinition.Max(v => v.z);

        Vector3Int gridSize = new Vector3Int(maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1);
        Vector3Int gridMin = new Vector3Int(minX, minY, minZ);

        // Draw grid lines
        Handles.color = Color.cyan;

        // Draw lines along X axis (width)
        for (int y = 0; y <= gridSize.y; y++)
        {
            for (int z = 0; z <= gridSize.z; z++)
            {
                Vector3 start = GetLocalPosition(gridMin.x, gridMin.y + y, gridMin.z + z, Constants.CellSize);
                Vector3 end = GetLocalPosition(gridMin.x - gridSize.x, gridMin.y + y, gridMin.z + z, Constants.CellSize);
                Handles.DrawLine(transform.TransformPoint(start), transform.TransformPoint(end));
            }
        }

        // Draw lines along Y axis (height)
        for (int x = 0; x <= gridSize.x; x++)
        {
            for (int z = 0; z <= gridSize.z; z++)
            {
                Vector3 start = GetLocalPosition(gridMin.x - x, gridMin.y, gridMin.z + z, Constants.CellSize);
                Vector3 end = GetLocalPosition(gridMin.x - x, gridMin.y + gridSize.y, gridMin.z + z, Constants.CellSize);
                Handles.DrawLine(transform.TransformPoint(start), transform.TransformPoint(end));
            }
        }

        // Draw lines along Z axis (depth)
        for (int x = 0; x <= gridSize.x; x++)
        {
            for (int y = 0; y <= gridSize.y; y++)
            {
                Vector3 start = GetLocalPosition(gridMin.x - x, gridMin.y + y, gridMin.z, Constants.CellSize);
                Vector3 end = GetLocalPosition(gridMin.x - x, gridMin.y + y, gridMin.z + gridSize.z, Constants.CellSize);
                Handles.DrawLine(transform.TransformPoint(start), transform.TransformPoint(end));
            }
        }

        // Draw occupied cells
        Handles.color = new Color(1f, 0.5f, 0f, 0.3f); // Semi-transparent orange
        foreach (Vector3Int cell in item.gridDefinition)
        {
            Vector3 cellCenter = GetLocalPosition(-cell.x - 0.5f, cell.y + 0.5f, cell.z + 0.5f, Constants.CellSize);
            Vector3 worldCenter = transform.TransformPoint(cellCenter);

            // Draw a cube for each occupied cell
            Handles.CubeHandleCap(0, worldCenter, transform.rotation, Constants.CellSize * 0.9f, EventType.Repaint);
        }
    }

    Vector3 GetLocalPosition(float gridX, float gridY, float gridZ, float cellSize)
    {
        // Map grid coordinates to local space
        // Assuming standard Unity coordinate system
        return new Vector3(gridX * cellSize, gridY * cellSize, gridZ * cellSize);
    }
}
