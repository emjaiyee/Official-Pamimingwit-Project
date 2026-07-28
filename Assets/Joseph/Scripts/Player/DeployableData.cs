using UnityEngine;

[CreateAssetMenu(fileName = "New Deployable", menuName = "Inventory/Deployable")]
public class DeployableData : ItemData
{
    [Header("Deployable Settings")]
    public GameObject worldPrefab; // The actual object that appears in the world
    public float minDistance = 1f;
    public float maxDistance = 4f;
    public bool requireWater = true;
}