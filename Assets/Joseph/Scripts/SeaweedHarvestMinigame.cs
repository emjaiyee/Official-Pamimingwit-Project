using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SeaweedHarvestMinigame : MonoBehaviour
{
    public static SeaweedHarvestMinigame Instance { get; private set; }

    [Header("Gameplay Settings")]
    [SerializeField] private int seaweedCount = 6;
    [SerializeField] private int trashCount = 4;
    [SerializeField] private float seaweedLifetime = 2.2f;
    [SerializeField] private float gameDuration = 15f;

    [Header("Osu-Style Pacing")]
    [Tooltip("Time delay between target spawns.")]
    [SerializeField] private float spawnInterval = 0.6f;
    [Tooltip("Maximum active targets visible on screen simultaneously.")]
    [SerializeField] private int maxConcurrentTargets = 3;

    [Header("UI References")]
    [SerializeField] private GameObject seaweedPanel;
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private RectTransform playArea;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Prefabs & Assets")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Button targetButtonPrefab;
    [SerializeField] private Sprite[] seaweedSprites;
    [SerializeField] private Sprite[] trashSprites;

    private readonly List<SeaweedHarvestTarget> activeTargets = new List<SeaweedHarvestTarget>();
    private HarvestableDeployable currentFarm;
    private float remainingTime;
    private int collectedCount;
    private int missedCount;
    private bool active;
    
    private Coroutine spawnCoroutine;
    private Coroutine resultHideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private GameObject GetPanel()
    {
        if (seaweedPanel != null) return seaweedPanel;
        if (UIManager.Instance != null && UIManager.Instance.seaweedPanel != null) return UIManager.Instance.seaweedPanel;
        return null;
    }

    private RectTransform GetSpawnArea()
    {
        if (spawnArea != null) return spawnArea;
        if (playArea != null) return playArea;
        return null;
    }

    private GameObject GetTargetPrefab()
    {
        if (targetPrefab != null) return targetPrefab;
        if (targetButtonPrefab != null) return targetButtonPrefab.gameObject;
        return null;
    }

    private void Update()
    {
        if (active)
        {
            remainingTime -= Time.unscaledDeltaTime;
            if (timerText != null) timerText.text = $"Time: {Mathf.Max(0f, remainingTime):F1}s";

            if (remainingTime <= 0f)
            {
                Finish(false, "Time ran out!");
                return;
            }
        }

        GameObject panel = GetPanel();
        if (panel != null && panel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelGame();
            }
        }
    }

    public void StartGame(HarvestableDeployable farm)
    {
        if (active || farm == null) return;
        if (!CanStartGame()) return;

        currentFarm = farm;
        if (resultHideCoroutine != null)
        {
            StopCoroutine(resultHideCoroutine);
            resultHideCoroutine = null;
        }

        active = true;
        remainingTime = gameDuration;
        collectedCount = 0;
        missedCount = 0;
        ClearTargets();

        GameObject panel = GetPanel();
        if (UIManager.Instance != null && panel != null)
        {
            UIManager.Instance.TogglePanelState(panel, true);
        }
        else if (panel != null)
        {
            panel.SetActive(true);
        }

        if (titleText != null) titleText.text = "SEAWEED HARVEST";
        if (instructionText != null) instructionText.text = "Click the targets before they shrink! Avoid the trash.";
        if (resultText != null) resultText.text = string.Empty;
        UpdateProgress();

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private bool CanStartGame()
    {
        GameObject panel = GetPanel();
        RectTransform area = GetSpawnArea();
        GameObject prefab = GetTargetPrefab();

        if (panel == null || area == null || prefab == null)
        {
            Debug.LogError($"[SeaweedHarvestMinigame] Missing UI setup! Panel: {panel != null}, SpawnArea: {area != null}, Prefab: {prefab != null}");
            return false;
        }
        return true;
    }

    private IEnumerator SpawnRoutine()
    {
        // Build sequence deck
        List<bool> targetQueue = new List<bool>();
        for (int i = 0; i < seaweedCount; i++) targetQueue.Add(false); // Seaweed
        for (int i = 0; i < trashCount; i++) targetQueue.Add(true);    // Trash

        // Shuffle deck
        for (int i = 0; i < targetQueue.Count; i++)
        {
            bool temp = targetQueue[i];
            int rand = Random.Range(i, targetQueue.Count);
            targetQueue[i] = targetQueue[rand];
            targetQueue[rand] = temp;
        }

        int queueIndex = 0;

        while (active && (queueIndex < targetQueue.Count || activeTargets.Count > 0))
        {
            // Spawn next target if queue remains and concurrency allows
            if (queueIndex < targetQueue.Count && activeTargets.Count < maxConcurrentTargets)
            {
                SpawnSingleTarget(targetQueue[queueIndex]);
                queueIndex++;
            }

            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    private void SpawnSingleTarget(bool isTrash)
    {
        if (!active) return;
        RectTransform area = GetSpawnArea();
        GameObject prefab = GetTargetPrefab();

        Sprite[] selectedSpritePool = isTrash ? trashSprites : seaweedSprites;
        Sprite chosenSprite = null;

        if (selectedSpritePool != null && selectedSpritePool.Length > 0)
        {
            chosenSprite = selectedSpritePool[Random.Range(0, selectedSpritePool.Length)];
        }

        GameObject targetObject = Instantiate(prefab, area);
        targetObject.name = isTrash ? "FakeTrash" : "Seaweed";

        RectTransform rect = targetObject.GetComponent<RectTransform>();
        if (rect == null) rect = targetObject.GetComponentInChildren<RectTransform>();

        if (rect == null)
        {
            Destroy(targetObject);
            return;
        }

        rect.localScale = Vector3.one;

        // Non-overlapping padding from bounds
        Vector2 randomPos = new Vector2(
            Random.Range(-area.rect.width / 2.4f, area.rect.width / 2.4f),
            Random.Range(-area.rect.height / 2.4f, area.rect.height / 2.4f)
        );
        rect.anchoredPosition = randomPos;

        Image targetImage = targetObject.GetComponent<Image>();
        if (targetImage == null) targetImage = targetObject.GetComponentInChildren<Image>();

        if (targetImage != null && chosenSprite != null)
        {
            targetImage.sprite = chosenSprite;
            if (rect.sizeDelta.x <= 0f || rect.sizeDelta.y <= 0f)
            {
                targetImage.SetNativeSize();
            }
        }

        targetObject.SetActive(true);

        Button button = targetObject.GetComponent<Button>();
        if (button == null) button = targetObject.GetComponentInChildren<Button>();

        if (button == null)
        {
            Destroy(targetObject);
            return;
        }

        SeaweedHarvestTarget target = targetObject.GetComponent<SeaweedHarvestTarget>();
        if (target == null) target = targetObject.AddComponent<SeaweedHarvestTarget>();

        target.Initialize(this, button, isTrash, seaweedLifetime);
        activeTargets.Add(target);
    }

    public void OnTargetClicked(SeaweedHarvestTarget target)
    {
        if (!active || target == null) return;

        activeTargets.Remove(target);
        if (target.IsTrash)
        {
            Finish(false, "You picked trash!");
            return;
        }

        collectedCount++;
        UpdateProgress();
        Destroy(target.gameObject);

        if (collectedCount >= seaweedCount)
        {
            Finish(true, "Perfect Harvest!");
        }
    }

    public void OnSeaweedExpired(SeaweedHarvestTarget target)
    {
        if (!active || target == null) return;

        activeTargets.Remove(target);
        Destroy(target.gameObject);

        if (target.IsTrash)
        {
            missedCount++;
            UpdateProgress();
        }
        else
        {
            Finish(false, "Missed a seaweed!");
        }
    }

    public void CancelGame()
    {
        active = false;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        ClearTargets();

        GameObject panel = GetPanel();
        if (UIManager.Instance != null && panel != null)
        {
            UIManager.Instance.TogglePanelState(panel, false);
        }
        else if (panel != null)
        {
            panel.SetActive(false);
        }

        currentFarm = null;
    }

    private void Finish(bool success, string message)
    {
        if (!active) return;

        active = false;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (resultText != null) resultText.text = message;
        ClearTargets();

        if (success && currentFarm != null)
        {
            if (currentFarm.CompleteHarvest(currentFarm.amount))
            {
                currentFarm.ConsumeAfterHarvest();
            }
        }
        else
        {
            UIManager.Instance?.ShowMessage(message);
        }

        resultHideCoroutine = StartCoroutine(HideResultAfterDelay());
        currentFarm = null;
    }

    private IEnumerator HideResultAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1.2f);

        GameObject panel = GetPanel();
        if (UIManager.Instance != null && panel != null)
        {
            UIManager.Instance.TogglePanelState(panel, false);
        }
        else if (panel != null)
        {
            panel.SetActive(false);
        }

        resultHideCoroutine = null;
    }

    private void ClearTargets()
    {
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            if (activeTargets[i] != null) Destroy(activeTargets[i].gameObject);
        }
        activeTargets.Clear();
    }

    private void UpdateProgress()
    {
        if (progressText != null) progressText.text = $"Seaweed: {collectedCount}/{seaweedCount}  Missed: {missedCount}";
    }
}

public sealed class SeaweedHarvestTarget : MonoBehaviour
{
    public bool IsTrash { get; private set; }

    private SeaweedHarvestMinigame manager;
    private Button button;
    private float lifetime;
    private float age;
    private bool clicked;

    public void Initialize(SeaweedHarvestMinigame targetManager, Button targetButton, bool trash, float duration)
    {
        manager = targetManager;
        button = targetButton;
        IsTrash = trash;
        lifetime = duration;
        age = 0f;
        clicked = false;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        transform.localScale = Vector3.one;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ShrinkAway());
        }
    }

    private void HandleClick()
    {
        if (clicked) return;
        clicked = true;
        manager.OnTargetClicked(this);
    }

    private IEnumerator ShrinkAway()
    {
        Vector3 initialScale = Vector3.one;
        while (age < lifetime && !clicked)
        {
            age += Time.unscaledDeltaTime;
            float normTime = age / lifetime;
            
            // Fast scale-in pop followed by linear scale down
            if (normTime < 0.15f)
            {
                transform.localScale = Vector3.one * (normTime / 0.15f);
            }
            else
            {
                transform.localScale = initialScale * Mathf.Clamp01(1f - ((normTime - 0.15f) / 0.85f));
            }

            yield return null;
        }

        if (!clicked)
        {
            clicked = true;
            manager.OnSeaweedExpired(this);
        }
    }
}