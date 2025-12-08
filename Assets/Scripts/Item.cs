using UnityEngine;

public class Item : MonoBehaviour
{
    private Vector3Int[] gridDefinition;
    private Vector2Int[] currentCells;
    public Vector3Int[] GridDefinition => gridDefinition;
    public Vector2Int[] CurrentCells => currentCells;

    public void Init(Vector3Int[] gridDefinition, Vector2Int[] currentCells)
    {
        this.gridDefinition = gridDefinition;
        this.currentCells = currentCells;
    }

    public void UpdateCells(Vector2Int[] newCells)
    {
        currentCells = newCells;
    }
}