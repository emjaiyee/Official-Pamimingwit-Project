using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnedTrashData
{
    public int prefabIndex;
    public Vector3 position;
    public string worldObjectID;
}

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public string mapBoundary;
    
    public int coins;
    public int sustainability;
    public int currentDay;
    public int currentTaxAmount;
    public float currentStamina;

    public List<InventorySaveData> inventorySaveData;
    public List<QuestProgress> questProgressData;
    public List<NarrativeTriggerState> narrativeTriggerStates;

    public List<string> destroyedObjectIDs;
    public List<SpawnedTrashData> spawnedBeachTrash;
}