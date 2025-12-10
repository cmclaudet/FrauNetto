using UnityEngine;

public class Item : MonoBehaviour
{
    // assume each vector is relative to the bottom left corner in grid units
    public Vector3Int[] gridDefinition;
    public Material material;
    [SerializeField]
    private Vector2Int[] currentCells;
    private bool isStatic = false;
    private Color originalColor;

    public Vector2Int[] CurrentCells => currentCells;
    public bool IsStatic => isStatic;

    public void Init(Vector2Int[] currentCells)
    {
        this.currentCells = currentCells;
        originalColor = material.color;
    }

    public void UpdateCells(Vector2Int[] newCells)
    {
        currentCells = newCells;
    }

    public void MakeStatic()
    {
        isStatic = true;
    }

    public void SetHover()
    {
        material.color = Color.yellow;
    }
    
    public void ResetHover()
    {
        material.color = originalColor;
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