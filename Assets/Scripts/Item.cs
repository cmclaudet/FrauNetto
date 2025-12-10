using UnityEngine;

public class Item : MonoBehaviour
{
    // assume each vector is relative to the bottom left corner in grid units
    public Vector3Int[] gridDefinition;
    public Renderer itemRenderer;
    public GameObject rendererContainer;
    [SerializeField]
    private Vector2Int[] currentCells;
    [SerializeField]
    private Vector3Int[] currentCells3D;
    private bool isStatic = false;
    private Color originalColor;

    public Vector2Int[] CurrentCells => currentCells;
    public Vector3Int[] CurrentCells3D => currentCells3D;
    public bool IsStatic => isStatic;

    public void Init(Vector2Int[] currentCells)
    {
        this.currentCells = currentCells;
        originalColor = itemRenderer.material.color;
    }

    public void Init3D(Vector3Int[] currentCells3D)
    {
        this.currentCells3D = currentCells3D;
        originalColor = itemRenderer.material.color;
    }

    public void UpdateCells(Vector2Int[] newCells)
    {
        currentCells = newCells;
    }

    public void UpdateCells3D(Vector3Int[] newCells)
    {
        currentCells3D = newCells;
    }

    public void MakeStatic()
    {
        isStatic = true;
    }

    public void MakeNonStatic()
    {
        isStatic = false;
    }

    public void SetHover()
    {
        itemRenderer.material.color = Color.yellow;
    }
    
    public void ResetHover()
    {
        itemRenderer.material.color = originalColor;
    }

    public Vector2Int[] GetFlatGridDefinition()
    {
        // transform gridDefinition into same coordinate system as ConveyorBeltGrid: positive x axis should be positive
        // x, and positive z axis should be positive y
        Vector2Int[] transformed = new Vector2Int[gridDefinition.Length];
        for (int i = 0; i < gridDefinition.Length; i++)
        {
            // Map: Item's X -> Grid's X, Item's Z -> Grid's Y
            transformed[i] = new Vector2Int(gridDefinition[i].x, gridDefinition[i].z);
        }
        return transformed;
    }

    public void RotateClockwise90()
    {
        // rotate object clockwise 90 degree around Y axis, using the renderer container as the pivot point so object rotates around its center
        rendererContainer.transform.Rotate(Vector3.up, 90f);
        
        // Calculate the center of the item in grid units on the XZ plane
        Vector2 center = CalculateGridCenter();
        Vector2 transformedCenter = new Vector2(center.y, center.x);

        // Rotate each point in gridDefinition 90 degrees clockwise around the center
        Vector3Int[] newGridDefinition = new Vector3Int[gridDefinition.Length];
        for (int i = 0; i < gridDefinition.Length; i++)
        {
            Vector3Int original = gridDefinition[i];

            // Translate to center
            float relX = original.x + 0.5f - center.x;
            float relZ = original.z + 0.5f - center.y;

            // Rotate 90 degrees clockwise around Y axis: (x, z) -> (z, -x)
            float newRelX = relZ;
            float newRelZ = -relX;

            // Translate back and round to nearest integer
            newGridDefinition[i] = new Vector3Int(
                Mathf.RoundToInt(newRelX - 0.5f + transformedCenter.x),
                original.y,
                Mathf.RoundToInt(newRelZ - 0.5f + transformedCenter.y)
            );
        }

        gridDefinition = newGridDefinition;

        var gridSize = GetGridSize();
        rendererContainer.transform.localPosition = new Vector3(gridSize.x, 0, gridSize.y) * 0.5f * Constants.CellSize;
    }

    private Vector2 CalculateGridCenter()
    {
        // Find the bounds of the grid definition in the XZ plane
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var cell in gridDefinition)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.z < minZ) minZ = cell.z;
            if (cell.z > maxZ) maxZ = cell.z;
        }

        // Calculate center point
        float centerX = (1 + minX + maxX) / 2f;
        float centerZ = (1 + minZ + maxZ) / 2f;

        return new Vector2(centerX, centerZ);
    }

    private Vector2 GetGridSize()
    {
        // Find the bounds of the grid definition in the XZ plane
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var cell in gridDefinition)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.z < minZ) minZ = cell.z;
            if (cell.z > maxZ) maxZ = cell.z;
        }
        
        return new Vector2(1 + maxX - minX, 1 + maxZ - minZ);
    }
}