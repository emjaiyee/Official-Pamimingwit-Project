using UnityEngine;

public enum ItemType { Tool, Material, Fish, Junk, Bait, Deployable, Artifact }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public int ID; // stable ID (IMPORTANT)
    public GameObject prefab;

    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType itemType;
    public bool stackable;
    public int maxStack = 99;
    public int price = 10;
}