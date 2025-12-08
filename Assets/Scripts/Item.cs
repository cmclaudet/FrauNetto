using UnityEngine;

public class Item : MonoBehaviour
{
    // assume each vector is relative to the bottom left corner in grid units
    public Vector3Int[] gridDefinition;
    private Vector2Int[] currentCells;
    public Vector2Int[] CurrentCells => currentCells;

    public void Init(Vector2Int[] currentCells)
    {
        this.currentCells = currentCells;
    }

    public void UpdateCells(Vector2Int[] newCells)
    {
        currentCells = newCells;
    }
}