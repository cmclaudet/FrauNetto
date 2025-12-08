using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public Item prefab;
}