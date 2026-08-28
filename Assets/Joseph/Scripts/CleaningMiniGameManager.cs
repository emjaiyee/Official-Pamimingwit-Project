using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class CleaningMiniGameManager : MonoBehaviour
{
    public static CleaningMiniGameManager Instance;

    [Header("Settings & Spawn Area")]
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private GameObject trashPrefab;
    [SerializeField] private Sprite[] recyclableSprites;
    [SerializeField] private Sprite[] organicSprites;
    [SerializeField] private int trashCount = 6;
    [SerializeField] private int baseCoinReward = 15;

    [Header("Bonus Loot Drops")]
    [SerializeField] private ItemData[] bonusLootPool;
    [SerializeField] private float bonusLootChance = 0.35f;

    [Header("UI & Combo Displays")]
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Combo Pop Animation Settings")]
    [SerializeField] private float comboPopScale = 1.4f;
    [SerializeField] private float comboPopDuration = 0.18f;

    [Header("Audio SFX")]
    [SerializeField] private AudioClip correctSortSFX;
    [SerializeField] private AudioClip wrongSortSFX;
    [SerializeField] private AudioClip completeSFX;

    private List<GameObject> activeTrash = new List<GameObject>();
    private GameObject currentWorldTrash;
    private AudioSource audioSource;

    private int comboCount = 0;
    private int totalCoinsEarned = 0;
    private float gameTimer = 0f;
    private bool isGameActive = false;

    private Coroutine comboAnimationCoroutine;
    private Vector3 originalComboScale = Vector3.one;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (comboText != null)
        {
            originalComboScale = comboText.transform.localScale;
        }
    }

    void Update()
    {
        if (isGameActive)
        {
            gameTimer += Time.deltaTime;
            if (timerText != null) timerText.text = $"Time: {gameTimer:F1}s";
        }

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

        if (UIManager.Instance == null || spawnArea == null || trashPrefab == null)
        {
            Debug.LogError("[CleaningMiniGameManager] Missing essential references!");
            return;
        }

        if (UIManager.Instance.cleaningPanel != null)
        {
            UIManager.Instance.TogglePanelState(UIManager.Instance.cleaningPanel, true);
            
            comboCount = 0;
            totalCoinsEarned = 0;
            gameTimer = 0f;
            isGameActive = true;

            if (comboText != null)
            {
                comboText.text = "";
                comboText.transform.localScale = originalComboScale;
            }

            SpawnTrash();
        }
    }

    private void SpawnTrash()
    {
        for (int i = 0; i < trashCount; i++)
        {
            GameObject t = Instantiate(trashPrefab, spawnArea);
            
            Vector2 randomPos = new Vector2(
                Random.Range(-spawnArea.rect.width / 2.3f, spawnArea.rect.width / 2.3f),
                Random.Range(-spawnArea.rect.height / 2.3f, spawnArea.rect.height / 2.3f)
            );
            t.GetComponent<RectTransform>().anchoredPosition = randomPos;

            TrashUIDrag dragScript = t.GetComponent<TrashUIDrag>();
            
            bool isRecyclable = Random.value > 0.5f;
            Sprite selectedSprite = null;

            if (isRecyclable && recyclableSprites.Length > 0)
            {
                selectedSprite = recyclableSprites[Random.Range(0, recyclableSprites.Length)];
            }
            else if (!isRecyclable && organicSprites.Length > 0)
            {
                selectedSprite = organicSprites[Random.Range(0, organicSprites.Length)];
            }

            if (dragScript != null)
            {
                dragScript.isRecyclable = isRecyclable;
                if (dragScript.trashImage != null && selectedSprite != null)
                {
                    dragScript.trashImage.sprite = selectedSprite;
                }
            }

            activeTrash.Add(t);
        }
    }

    public void OnTrashSorted(GameObject trash, bool droppedInRecycleBin)
    {
        TrashUIDrag dragScript = trash.GetComponent<TrashUIDrag>();
        if (dragScript == null) return;

        bool isCorrect = (dragScript.isRecyclable == droppedInRecycleBin);

        if (isCorrect)
        {
            comboCount++;
            int rewardThisItem = baseCoinReward + (comboCount * 2);
            totalCoinsEarned += rewardThisItem;

            if (correctSortSFX != null && audioSource != null)
            {
                audioSource.pitch = Mathf.Clamp(1.0f + (comboCount * 0.05f), 1.0f, 1.8f);
                audioSource.PlayOneShot(correctSortSFX);
            }

            TriggerComboPop();
        }
        else
        {
            comboCount = 0;
            totalCoinsEarned += Mathf.Max(1, baseCoinReward / 2);

            if (wrongSortSFX != null && audioSource != null)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(wrongSortSFX);
            }

            if (comboText != null) comboText.text = "";
        }

        activeTrash.Remove(trash);
        Destroy(trash);

        if (activeTrash.Count <= 0)
        {
            FinishGame();
        }
    }

    private void TriggerComboPop()
    {
        if (comboText == null) return;

        // Show text for any combo >= 1
        comboText.text = comboCount > 1 ? $"{comboCount}x COMBO!" : "GOOD!";

        if (comboAnimationCoroutine != null) StopCoroutine(comboAnimationCoroutine);
        comboAnimationCoroutine = StartCoroutine(AnimateComboPop());
    }

    private IEnumerator AnimateComboPop()
    {
        Transform textTransform = comboText.transform;
        Vector3 targetScale = originalComboScale * comboPopScale;

        // Punch Up
        float elapsed = 0f;
        while (elapsed < comboPopDuration)
        {
            elapsed += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(originalComboScale, targetScale, elapsed / comboPopDuration);
            yield return null;
        }

        // Elastic Bounce Down
        elapsed = 0f;
        while (elapsed < comboPopDuration)
        {
            elapsed += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(targetScale, originalComboScale, elapsed / comboPopDuration);
            yield return null;
        }

        textTransform.localScale = originalComboScale;
    }

    private void CancelGame()
    {
        isGameActive = false;
        foreach (GameObject t in activeTrash)
        {
            if (t != null) Destroy(t);
        }
        activeTrash.Clear();

        if (UIManager.Instance != null)
            UIManager.Instance.TogglePanelState(UIManager.Instance.cleaningPanel, false);

        currentWorldTrash = null;
    }

    private void FinishGame()
    {
        isGameActive = false;
        if (audioSource != null) audioSource.pitch = 1.0f;

        int speedBonus = (gameTimer < 5.0f) ? 25 : (gameTimer < 8.0f ? 10 : 0);
        totalCoinsEarned += speedBonus;

        int sustainabilityGain = (comboCount >= trashCount) ? 5 : 3;
        SustainabilityManager.Instance?.Add(sustainabilityGain);

        PlayerWallet.Instance?.AddCoins(totalCoinsEarned);

        if (Random.value < bonusLootChance && bonusLootPool.Length > 0)
        {
            ItemData bonusItem = bonusLootPool[Random.Range(0, bonusLootPool.Length)];
            if (Inventory.Instance != null && bonusItem != null)
            {
                Inventory.Instance.AddItem(bonusItem);
                UIManager.Instance?.ShowMessage($"Cleaned up! Bonus loot found: {bonusItem.itemName}");
            }
        }
        else
        {
            UIManager.Instance?.ShowMessage($"Cleaned up! +{totalCoinsEarned} Coins");
        }

        if (completeSFX != null && audioSource != null)
            audioSource.PlayOneShot(completeSFX);

        if (UIManager.Instance != null)
            UIManager.Instance.TogglePanelState(UIManager.Instance.cleaningPanel, false);

        if (currentWorldTrash != null)
        {
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