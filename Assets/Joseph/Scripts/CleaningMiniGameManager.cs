using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CleaningMiniGameManager : MonoBehaviour
{
    public static CleaningMiniGameManager Instance;

    [Header("Settings")]
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private GameObject trashPrefab; // A UI Prefab with an Image and a Drag script
    [SerializeField] private Sprite[] trashSprites;
    [SerializeField] private int trashCount = 5;
    [SerializeField] private int coinReward = 20;

    [Header("Audio")]
    [SerializeField] private AudioClip binnedSFX;

    private List<GameObject> activeTrash = new List<GameObject>();
    private GameObject currentWorldTrash;
    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // Allow players to cancel the minigame by pressing Escape
        if (UIManager.Instance != null && UIManager.Instance.cleaningPanel != null && UIManager.Instance.cleaningPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelGame();
            }
        }
    }

    public void StartGame(GameObject worldTrash)
    {
        currentWorldTrash = worldTrash;

        if (UIManager.Instance == null)
        {
            Debug.LogError("CleaningMiniGameManager: UIManager Instance not found!");
            return;
        }

        if (spawnArea == null || trashPrefab == null)
        {
            Debug.LogError("CleaningMiniGameManager: spawnArea or trashPrefab is not assigned in the Inspector!");
            return;
        }

        if (UIManager.Instance.cleaningPanel != null)
        {
            UIManager.Instance.TogglePanelState(UIManager.Instance.cleaningPanel, true);
            
            SpawnTrash();
        }
        else
        {
            Debug.LogError("CleaningMiniGameManager: Cleaning Panel is not assigned in the UIManager Inspector!");
        }
    }

    private void SpawnTrash()
    {
        for (int i = 0; i < trashCount; i++)
        {
            GameObject t = Instantiate(trashPrefab, spawnArea);
            
            // Set random position within the spawn area
            Vector2 randomPos = new Vector2(
                Random.Range(-spawnArea.rect.width / 2, spawnArea.rect.width / 2),
                Random.Range(-spawnArea.rect.height / 2, spawnArea.rect.height / 2)
            );
            t.GetComponent<RectTransform>().anchoredPosition = randomPos;

            // Set random sprite
            if (trashSprites.Length > 0) 
            {
                TrashUIDrag dragScript = t.GetComponent<TrashUIDrag>();
                // Priority: Use the specific trashImage reference if assigned, otherwise fallback to root Image
                Image targetImage = (dragScript != null && dragScript.trashImage != null) 
                    ? dragScript.trashImage 
                    : t.GetComponent<Image>();

                if (targetImage != null) targetImage.sprite = trashSprites[Random.Range(0, trashSprites.Length)];
            }

            activeTrash.Add(t);
        }
    }

    public void OnTrashBinned(GameObject trash)
    {
        activeTrash.Remove(trash);
        Destroy(trash);

        if (binnedSFX != null) audioSource.PlayOneShot(binnedSFX);

        if (activeTrash.Count <= 0)
        {
            FinishGame();
        }
    }

    private void CancelGame()
    {
        // Cleanup any remaining spawned UI trash so they don't stay on screen
        foreach (GameObject t in activeTrash)
        {
            if (t != null) Destroy(t);
        }
        activeTrash.Clear();

        // Exit the UI and restore standard game states
        if (UIManager.Instance != null)
            UIManager.Instance.TogglePanelState(UIManager.Instance.cleaningPanel, false);

        currentWorldTrash = null;
    }

    private void FinishGame()
    {
        Debug.Log("Cleaning Complete! Sustainability increased.");
        if (SustainabilityManager.Instance != null)
            SustainabilityManager.Instance.Add(3);

        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.AddCoins(coinReward);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.TogglePanelState(UIManager.Instance.cleaningPanel, false);

        if (currentWorldTrash != null)
        {
            // Register the destruction with the save system so it stays gone on reload
            var trash = currentWorldTrash.GetComponent<TrashModule>();
            if (trash != null)
            {
                SaveController.RegisterDestruction(trash.worldObjectID);
            }

            ObjectiveCutsceneTrigger.NotifyProgress(currentWorldTrash);
            Destroy(currentWorldTrash);
        }
    }
}