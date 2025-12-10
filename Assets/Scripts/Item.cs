using UnityEngine;

public class Item : MonoBehaviour
{
    // assume each vector is relative to the bottom left corner in grid units
    public Vector3Int[] gridDefinition;
    public Renderer itemRenderer;
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
}