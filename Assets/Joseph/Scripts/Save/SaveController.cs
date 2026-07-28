using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class SaveController : MonoBehaviour
{
    private string saveLocation;

    public static bool shouldLoadGame = false;
    public static bool loadRequestedFromMenu = false;

    public static System.Collections.Generic.List<string> destroyedObjectIDs = new System.Collections.Generic.List<string>();

    public static void RegisterDestruction(string id)
    {
        if (!string.IsNullOrEmpty(id) && !destroyedObjectIDs.Contains(id))
            destroyedObjectIDs.Add(id);
    }

    public static void ClearStaticData()
    {
        destroyedObjectIDs.Clear();
        // Add any other static lists that need clearing here
    }

    void Awake()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");

        // If we are loading a game, we need to load destroyedObjectIDs synchronously
        // so that other objects can check it in their Awake/Start methods.
        if (shouldLoadGame && loadRequestedFromMenu && File.Exists(saveLocation))
        {
            LoadDestroyedObjectIDsSynchronously();
        }
    }

    void Start()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");

        // 🛑 ONLY RUN LOAD LOGIC IF WE ARE ACTUALLY GOING INTO GAME
        if (SceneManager.GetActiveScene().name == "Palipi Bay")
        {
            StartCoroutine(LoadAfterSceneReady());
        }
    }

    private void LoadDestroyedObjectIDsSynchronously()
    {
        try
        {
            string json = File.ReadAllText(saveLocation);
            SaveData tempSaveData = JsonUtility.FromJson<SaveData>(json);
            destroyedObjectIDs = tempSaveData.destroyedObjectIDs ?? new System.Collections.Generic.List<string>();
            
            if (NarrativeStateManager.Instance != null)
                NarrativeStateManager.Instance.LoadSaveData(tempSaveData.narrativeTriggerStates);
                
            Debug.Log($"[SaveController] Synchronously loaded {destroyedObjectIDs.Count} destroyed object IDs.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveController] Error loading destroyed object IDs synchronously: {e.Message}");
            destroyedObjectIDs.Clear(); // Clear to prevent issues if load fails
        }
    }

    private IEnumerator LoadAfterSceneReady()
    {
        // 🛑 EXTRA SAFETY CHECK
        if (!shouldLoadGame || !loadRequestedFromMenu)
        {
            yield break;
        }

        // Ensure the screen is black immediately if UIManager is ready
        if (UIManager.Instance != null && UIManager.Instance.dayTransitionOverlay != null)
            UIManager.Instance.dayTransitionOverlay.alpha = 1f;

        // Wait slightly longer before starting the load to ensure the scene is stable
        yield return new WaitForSeconds(0.5f);

        LoadGame();

        // Stay black for a bit longer after the load is processed to hide camera warping and object destruction
        yield return new WaitForSeconds(0.5f);

        // Smoothly fade out the black overlay after loading is complete
        if (UIManager.Instance != null && UIManager.Instance.dayTransitionOverlay != null)
        {
            float duration = 1.5f; // Slower fade out for a more polished transition
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                UIManager.Instance.dayTransitionOverlay.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                yield return null;
            }
            UIManager.Instance.dayTransitionOverlay.alpha = 0f;
        }

        shouldLoadGame = false;
        loadRequestedFromMenu = false;
    }

    public void SaveGame()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var confiner = Object.FindAnyObjectByType<CinemachineConfiner2D>();

        SaveData saveData = new SaveData
        {
            playerPosition = player != null ? player.transform.position : Vector3.zero,

            mapBoundary = confiner != null && confiner.BoundingShape2D != null
                ? confiner.BoundingShape2D.gameObject.name
                : "",

            inventorySaveData = Inventory.Instance != null
                ? Inventory.Instance.GetInventoryItems()
                : null,

            coins = PlayerWallet.Instance != null ? PlayerWallet.Instance.coins : 0,
            sustainability = SustainabilityManager.Instance != null ? SustainabilityManager.Instance.CurrentSustainability : 0,
            currentDay = GameManager.Instance != null ? GameManager.Instance.currentDay : 1,
            currentTaxAmount = GameManager.Instance != null ? GameManager.Instance.currentTaxAmount : 50,
            currentStamina = StaminaManager.Instance != null ? StaminaManager.Instance.GetStamina() : 100f,

            questProgressData = QuestController.instance != null
                ? QuestController.instance.GetQuestSaveData() : null,

            narrativeTriggerStates = NarrativeStateManager.Instance != null
                ? NarrativeStateManager.Instance.GetSaveData()
                : null,

            destroyedObjectIDs = new System.Collections.Generic.List<string>(destroyedObjectIDs),

            spawnedBeachTrash = BeachTrashSpawner.Instance != null 
                ? BeachTrashSpawner.Instance.GetSaveData() 
                : new System.Collections.Generic.List<SpawnedTrashData>()
        };

        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData, true));

        Debug.Log("GAME SAVED MANUALLY");
    }

    public void LoadGame()
    {
        // 🛑 HARD STOP: if no file → DO NOTHING, STAY IN MENU FLOW
        if (!File.Exists(saveLocation))
        {
            Debug.LogWarning("NO SAVE FILE FOUND → STAYING IN MAIN MENU");
            return;
        }

        SaveData saveData = JsonUtility.FromJson<SaveData>(
            File.ReadAllText(saveLocation)
        );

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Calculate the teleport distance (delta)
            Vector3 warpDelta = saveData.playerPosition - player.transform.position;
            player.transform.position = saveData.playerPosition;

            // Notify Cinemachine to warp the camera immediately so it doesn't "pan" to the new position
            var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null) vcam.OnTargetObjectWarped(player.transform, warpDelta);
        }

        var confiner = Object.FindAnyObjectByType<CinemachineConfiner2D>();
        if (confiner != null && !string.IsNullOrEmpty(saveData.mapBoundary))
        {
            var boundaryObj = GameObject.Find(saveData.mapBoundary);
            if (boundaryObj != null)
            {
                var collider = boundaryObj.GetComponent<PolygonCollider2D>();
                if (collider != null)
                    confiner.BoundingShape2D = collider;
            }
        }

        if (Inventory.Instance != null)
            Inventory.Instance.SetInventoryItems(saveData.inventorySaveData);

        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.coins = saveData.coins;
            PlayerWallet.Instance.OnCoinsChanged?.Invoke(saveData.coins);
        }

        if (SustainabilityManager.Instance != null)
        {
            // Use Add with a difference to trigger the OnSustainabilityChanged event and ReactiveOcean transitions
            int diff = saveData.sustainability - SustainabilityManager.Instance.CurrentSustainability;
            SustainabilityManager.Instance.Add(diff);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentDay = saveData.currentDay;
            GameManager.Instance.currentTaxAmount = saveData.currentTaxAmount;
            UIManager.Instance?.UpdateDayHUD();
        }

        if (StaminaManager.Instance != null)
            StaminaManager.Instance.SetStamina(saveData.currentStamina);

        if (QuestController.instance != null)
            QuestController.instance.LoadQuestProgress(saveData.questProgressData);

        if (NarrativeStateManager.Instance != null)
            NarrativeStateManager.Instance.LoadSaveData(saveData.narrativeTriggerStates);

        // destroyedObjectIDs is already loaded synchronously in Awake, no need to load again here.
        if (BeachTrashSpawner.Instance != null)
            BeachTrashSpawner.Instance.LoadSaveData(saveData.spawnedBeachTrash);

        Debug.Log("SAVE LOADED SUCCESSFULLY");
    }
}