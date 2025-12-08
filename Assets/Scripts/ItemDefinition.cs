using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    // assume each vector is relative to the bottom left corner in grid units
    public Vector3Int[] gridDefinition;
    public GameObject prefab;
}