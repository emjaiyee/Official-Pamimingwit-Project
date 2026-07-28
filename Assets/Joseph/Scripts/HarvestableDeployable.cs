using UnityEngine;

public enum DeployableType { Cage, Farm }

public class HarvestableDeployable : MonoBehaviour, IInteractable
{
    [Header("Harvest Settings")]
    public DeployableType deployableType;
    public ItemData resultItem;
    public int amount = 4;
    public float readyTime = 60f;
    [Tooltip("Positive for sustainable farms, negative for illegal cages.")]
    public int sustainabilityEffect = 0;

    [Header("Visuals")]
    public Sprite growingSprite;
    public Sprite readySprite;
    
    [Header("Juice - General")]
    public GameObject readyIndicatorPrefab;
    private GameObject spawnedIndicator;
    public Vector3 indicatorOffset = new Vector3(0, 1.2f, 0);
    public float pulseSpeed = 5f;
    public float pulseAmount = 0.15f;

    [Header("Juice - Water")]
    public GameObject ripplePrefab;
    public float bobSpeed = 2f;
    public float bobAmount = 0.05f;
    public float rippleInterval = 2f;

    private SpriteRenderer sr;
    private float timer;
    private bool isReady;
    private Vector3 basePosition;
    private bool isInWater;
    private float rippleTimer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && growingSprite != null) sr.sprite = growingSprite;
        //if (readyIndicator != null) readyIndicator.gameObject.SetActive(false);

        basePosition = transform.position;

        // Check if placed in water using the layer defined in FishingManager
        if (FishingManager.Instance != null)
        {
            isInWater = Physics2D.OverlapCircle(transform.position, 0.1f, FishingManager.Instance.waterLayer);
        }
    }

    void Update()
    {
        // Handle water visuals regardless of readiness
        if (isInWater)
        {
            // Gentle vertical bobbing
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = basePosition + new Vector3(0, yOffset, 0);

            // Spawning ripples
            rippleTimer += Time.deltaTime;
            if (rippleTimer >= rippleInterval)
            {
                rippleTimer = 0;
                if (ripplePrefab != null)
                {
                    Instantiate(ripplePrefab, basePosition, Quaternion.identity);
                }
            }
        }

        if (isReady)
        {
            if (spawnedIndicator != null)
            {
                float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                spawnedIndicator.transform.localScale = new Vector3(s, s, 1f);
            }
            return;
        }

        timer += Time.deltaTime;
        if (timer >= readyTime)
        {
            isReady = true;
            if (sr != null && readySprite != null) sr.sprite = readySprite;
            
            if (readyIndicatorPrefab != null && spawnedIndicator == null)
            {
                spawnedIndicator = Instantiate(readyIndicatorPrefab, transform.position + indicatorOffset, Quaternion.identity);
            }
        
        }
    }

    public void Interact()
    {
        if (!isReady) return;

        // Random yield/haul: minimum of 1 and max of 4
        int finalHaul = Random.Range(1, 5);

        bool anyAdded = false;
        for (int i = 0; i < finalHaul; i++)
        {
            // Try to get a random catch from the ocean manager
            ItemData caught = (ReactiveOceanManager.Instance != null) ? ReactiveOceanManager.Instance.GetRandomCatch() : null;
            
            // Fallback to the resultItem if the ocean catch failed (e.g. uninitialized tier or empty pool)
            if (caught == null) caught = resultItem;

            if (caught != null && Inventory.Instance != null)
            {
                // Always grant Bronze quality for deployable hauls as requested
                if (Inventory.Instance.AddItem(caught, 1, FishQuality.Bronze))
                    anyAdded = true;
            }
        }

        if (anyAdded)
        {
            if (sustainabilityEffect != 0) SustainabilityManager.Instance?.Add(sustainabilityEffect);
            UIManager.Instance?.ShowMessage($"{deployableType} haul harvested!");
            
            // REUSABLE: Reset state instead of destroying the object
            isReady = false;
            timer = 0;
            if (sr != null && growingSprite != null) sr.sprite = growingSprite;
            //if (readyIndicator != null) readyIndicator.gameObject.SetActive(false);

            if (spawnedIndicator != null)
            {
                Destroy(spawnedIndicator);
            }
        }
        else
        {
            UIManager.Instance?.ShowMessage("Inventory Full!");
        }
    }

    public string GetInteractPrompt()
    {
        return isReady ? $"Harvest {deployableType} [E]" : $"Growing... ({Mathf.Ceil(readyTime - timer)}s)";
    }
}