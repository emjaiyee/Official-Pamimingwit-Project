using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns interactive trash objects along the beach area.
/// Density and frequency are determined by the current Ocean Tier.
/// </summary>
public class BeachTrashSpawner : MonoBehaviour
{
    [System.Serializable]
    public class TrashTierConfig
    {
        public string tierName;
        public int maxTrashCount = 5;
        public float spawnCooldown = 30f;
    }

    [Header("References")]
    [Tooltip("The collider defining the walkable beach area where trash can spawn.")]
    [SerializeField] private Collider2D spawnArea;
    [Tooltip("Prefabs with TrashModule/Interactable components.")]
    [SerializeField] private GameObject[] worldTrashPrefabs;

    public static BeachTrashSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("Tier Settings")]
    [SerializeField] private List<TrashTierConfig> tierConfigs = new List<TrashTierConfig>();
    [SerializeField] private TrashTierConfig defaultConfig = new TrashTierConfig();

    private List<GameObject> activeTrash = new List<GameObject>();
    private float spawnTimer;

    private void Start()
    {
        if (ReactiveOceanManager.Instance != null)
        {
            ReactiveOceanManager.Instance.OnTierChanged += HandleTierChanged;
        }
        
        // Initial check to populate the beach on start
        RefreshTrashSpawning();
    }

    private void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            RefreshTrashSpawning();
        }
    }

    private void HandleTierChanged(OceanTier newTier)
    {
        // Environment changed; immediately check if we need to adjust trash levels
        RefreshTrashSpawning();
    }

    private void RefreshTrashSpawning()
    {
        // Clean up null references from cleaned/destroyed trash
        activeTrash.RemoveAll(t => t == null);

        TrashTierConfig config = GetCurrentConfig();
        spawnTimer = config.spawnCooldown;

        if (activeTrash.Count < config.maxTrashCount)
        {
            SpawnTrash();
        }
    }

    private TrashTierConfig GetCurrentConfig()
    {
        if (ReactiveOceanManager.Instance == null) return defaultConfig;

        OceanTier current = ReactiveOceanManager.Instance.GetCurrentTier();
        if (current == null) return defaultConfig;

        var config = tierConfigs.Find(c => c.tierName == current.tierName);
        return config ?? defaultConfig;
    }

    private void SpawnTrash()
    {
        if (worldTrashPrefabs == null || worldTrashPrefabs.Length == 0 || spawnArea == null) return;

        Bounds b = spawnArea.bounds;
        Vector2 randomPoint = new Vector2(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y)
        );

        // Verify the point is actually inside the collider (useful for complex polygon shapes)
        if (!spawnArea.OverlapPoint(randomPoint)) return;

        int prefabIndex = Random.Range(0, worldTrashPrefabs.Length);
        GameObject prefab = worldTrashPrefabs[prefabIndex];
        GameObject trash = Instantiate(prefab, randomPoint, Quaternion.identity);
        
        // We prefix the name with the index so we can retrieve it easily in GetSaveData
        trash.name = $"{prefabIndex}_{trash.name}";

        // Generate a unique ID for this procedural instance
        TrashModule tm = trash.GetComponent<TrashModule>();
        if (tm != null)
        {
            tm.worldObjectID = $"BeachTrash_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        activeTrash.Add(trash);
    }

    public List<SpawnedTrashData> GetSaveData()
    {
        List<SpawnedTrashData> data = new List<SpawnedTrashData>();
        foreach (var t in activeTrash)
        {
            if (t == null) continue;

            // Retrieve the prefab index we stored in the name during SpawnTrash
            int pIndex = 0;
            string[] nameParts = t.name.Split('_');
            if (nameParts.Length > 0) int.TryParse(nameParts[0], out pIndex);

            TrashModule tm = t.GetComponent<TrashModule>();

            data.Add(new SpawnedTrashData { 
                position = t.transform.position, 
                prefabIndex = pIndex,
                worldObjectID = tm != null ? tm.worldObjectID : ""
            }); 
        }
        return data;
    }

    public void LoadSaveData(List<SpawnedTrashData> data)
    {
        if (data == null) return;

        // Clear current procedurally spawned trash
        foreach (var t in activeTrash) if (t != null) Destroy(t);
        activeTrash.Clear();

        // Recreate from save
        foreach (var d in data)
        {
            if (worldTrashPrefabs.Length > d.prefabIndex)
            {
                GameObject trash = Instantiate(worldTrashPrefabs[d.prefabIndex], d.position, Quaternion.identity);
                trash.name = $"{d.prefabIndex}_{trash.name}";

                // Restore the unique ID from the save file
                TrashModule tm = trash.GetComponent<TrashModule>();
                if (tm != null) tm.worldObjectID = d.worldObjectID;

                activeTrash.Add(trash);
            }
        }
        spawnTimer = GetCurrentConfig().spawnCooldown;
    }
}