using System.Collections.Generic;
using UnityEngine;

public class itemDictionary : MonoBehaviour
{
    public List<ItemData> itemPrefabs;
    private Dictionary<int, GameObject> _itemDictionary;

    private void Awake()
    {
        _itemDictionary = new Dictionary<int, GameObject>();

        foreach (ItemData item in itemPrefabs)
        {
            if (item == null)
                continue;

            if (item.prefab == null)
            {
                Debug.LogWarning($"Item '{item.name}' has no prefab assigned.");
                continue;
            }

            if (_itemDictionary.ContainsKey(item.ID))
            {
                Debug.LogError($"Duplicate Item ID detected: {item.ID} on '{item.name}'");
                continue;
            }

            _itemDictionary[item.ID] = item.prefab;
        }
    }

    public GameObject GetItemPrefab(int itemID)
    {
        _itemDictionary.TryGetValue(itemID, out GameObject prefab);

        if (prefab == null)
        {
            Debug.LogWarning($"Item with ID {itemID} not found in Dictionary");
        }

        return prefab;
    }
}