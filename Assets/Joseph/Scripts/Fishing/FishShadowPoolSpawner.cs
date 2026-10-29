using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FishShadowPoolSpawner : MonoBehaviour
{
    private sealed class ShadowPool
    {
        public Transform transform;
        public Vector2 fishingAreaRadius;
        public Vector3 driftDirection;
        public float driftSpeed;
        public float respawnTime;
        public float rippleTimer;
    }

    private const int PoolCount = 4;
    private const int PositionAttempts = 120;
    private const int MinimumDepthSampleCount = 4;
    private const int DefaultDepthSampleCount = 24;
    private const float MinimumRespawnTime = 8f;
    private const float MaximumRespawnTime = 15f;
    private static readonly int SurfaceLevelProperty = Shader.PropertyToID("_SurfaceLevel");
    private static readonly int DepthVerticalScaleProperty = Shader.PropertyToID("_DepthVerticalScale");
    private static readonly Vector2 PoolSpriteHalfSize = new Vector2(0.75f, 0.4375f);

    [Header("Water Placement")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private int poolCount = PoolCount;
    [SerializeField] private int positionAttempts = PositionAttempts;

    [Header("Depth Filter")]
    [Tooltip("Minimum normalized shader depth required for a pool. Matches the water shader's _SurfaceLevel and _DepthVerticalScale.")]
    [SerializeField, Range(0f, 1f)] private float minimumShaderDepth = 0.13f;
    [Tooltip("Fallback minimum distance in world units from the water's edge when its material has no depth shader properties.")]
    [SerializeField, Min(0f)] private float minimumWaterDepth = 2f;
    [Tooltip("Number of points checked around the depth boundary. Higher values follow irregular shorelines more accurately.")]
    [SerializeField, Min(MinimumDepthSampleCount)] private int depthSampleCount = DefaultDepthSampleCount;

    [Header("Fishing Area")]
    [SerializeField] private string sortingLayerName = "WalkInfront";
    [SerializeField] private int sortingOrder = 10;
    [Tooltip("Controls the hidden fishing area radius, independently for each spawned pool.")]
    [SerializeField] private Vector2 poolScaleRange = new Vector2(1.1f, 1.65f);

    [Header("Pool Bubbles")]
    [SerializeField] private GameObject bubbleParticlePrefab;
    [SerializeField, Min(0f)] private float bubbleParticleScale = 1f;

    [Header("Pool Ripples")]
    [SerializeField] private GameObject ripplePrefab;
    [SerializeField, Min(0.05f)] private float rippleInterval = 1.2f;
    [SerializeField, Min(0f)] private float rippleScale = 0.5f;

    [Header("Pool Movement")]
    [SerializeField] private Vector2 driftSpeedRange = new Vector2(0.025f, 0.07f);
    [SerializeField] private Vector2 respawnTimeRange = new Vector2(MinimumRespawnTime, MaximumRespawnTime);

    public static FishShadowPoolSpawner Instance { get; private set; }

    private readonly List<ShadowPool> pools = new List<ShadowPool>();
    private bool initialized;

    private void Awake()
    {
        if (initialized) return;

        initialized = true;
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[FishShadowPoolSpawner] More than one fish-shadow pool spawner is active.");
            enabled = false;
            return;
        }

        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
        StartCoroutine(SpawnPools());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!initialized) return;

        float deltaTime = Time.deltaTime;
        foreach (ShadowPool pool in pools)
        {
            if (pool.transform == null) continue;

            Vector3 nextPosition = pool.transform.position + pool.driftDirection * (pool.driftSpeed * deltaTime);
            if (IsDeepWaterPosition(nextPosition))
            {
                pool.transform.position = nextPosition;
            }
            else
            {
                pool.driftDirection = Random.insideUnitCircle.normalized;
            }

            if (ripplePrefab != null)
            {
                pool.rippleTimer -= deltaTime;
                if (pool.rippleTimer <= 0f)
                {
                    SpawnRipple(pool);
                    pool.rippleTimer = Random.Range(rippleInterval * 0.75f, rippleInterval * 1.25f);
                }
            }

            pool.respawnTime -= deltaTime;
            if (pool.respawnTime <= 0f)
            {
                if (TryFindWaterPosition(out Vector3 position))
                {
                    pool.transform.position = position;
                    pool.driftDirection = Random.insideUnitCircle.normalized;
                    pool.respawnTime = Random.Range(respawnTimeRange.x, respawnTimeRange.y);
                }
                else
                {
                    pool.respawnTime = 2f;
                }
            }
        }
    }

    public bool IsPositionInPool(Vector2 position)
    {
        foreach (ShadowPool pool in pools)
        {
            if (pool.transform == null) continue;

            Vector2 offset = position - (Vector2)pool.transform.position;
            float normalizedX = offset.x / Mathf.Max(pool.fishingAreaRadius.x, 0.01f);
            float normalizedY = offset.y / Mathf.Max(pool.fishingAreaRadius.y, 0.01f);
            if ((normalizedX * normalizedX) + (normalizedY * normalizedY) <= 1f)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator SpawnPools()
    {
        yield return null;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[FishShadowPoolSpawner] A main camera is required to place fish-shadow pools.");
            yield break;
        }
        if (waterLayer.value == 0)
        {
            Debug.LogError("[FishShadowPoolSpawner] Assign the water layer in the Inspector.");
            yield break;
        }

        for (int i = 0; i < Mathf.Max(0, poolCount); i++)
        {
            if (!TryFindWaterPosition(out Vector3 position)) continue;

            GameObject poolObject = new GameObject($"Fish Shadow Pool {i + 1}");
            poolObject.transform.SetParent(transform, true);
            poolObject.transform.position = position;

            float scale = Random.Range(poolScaleRange.x, poolScaleRange.y);
            float heightScale = Random.Range(0.8f, 1.15f);
            Vector2 fishingAreaRadius = Vector2.Scale(PoolSpriteHalfSize, new Vector2(scale, scale * heightScale));

            SpawnBubbleParticles(poolObject.transform);
            ShadowPool pool = new ShadowPool
            {
                transform = poolObject.transform,
                fishingAreaRadius = fishingAreaRadius,
                driftDirection = Random.insideUnitCircle.normalized,
                driftSpeed = Random.Range(driftSpeedRange.x, driftSpeedRange.y),
                respawnTime = Random.Range(respawnTimeRange.x, respawnTimeRange.y),
                rippleTimer = Random.Range(0f, rippleInterval)
            };
            pools.Add(pool);
        }
    }

    private void SpawnRipple(ShadowPool pool)
    {
        GameObject ripple = Instantiate(
            ripplePrefab,
            pool.transform.position,
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

        if (ripple.TryGetComponent<RippleEffect>(out RippleEffect rippleEffect))
        {
            rippleEffect.Initialize(rippleScale);
        }

        SortingGroup sortingGroup = ripple.GetComponent<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName = sortingLayerName;
            sortingGroup.sortingOrder = sortingOrder + 1;
        }
    }

    private void SpawnBubbleParticles(Transform poolTransform)
    {
        if (bubbleParticlePrefab == null) return;

        GameObject bubbles = Instantiate(bubbleParticlePrefab, poolTransform, false);
        bubbles.transform.localPosition = Vector3.zero;
        bubbles.transform.localScale = bubbleParticlePrefab.transform.localScale * bubbleParticleScale;

        ParticleSystemRenderer particleRenderer = bubbles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.sortingLayerName = sortingLayerName;
            particleRenderer.sortingOrder = sortingOrder + 1;
        }
    }

    private bool TryFindWaterPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (mainCamera == null) return false;

        float height = mainCamera.orthographic ? mainCamera.orthographicSize : 5f;
        float width = height * mainCamera.aspect;
        Vector3 cameraPosition = mainCamera.transform.position;

        for (int attempt = 0; attempt < Mathf.Max(1, positionAttempts); attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(cameraPosition.x - width, cameraPosition.x + width),
                Random.Range(cameraPosition.y - height, cameraPosition.y + height),
                0f);

            if (IsDeepWaterPosition(candidate))
            {
                position = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsDeepWaterPosition(Vector2 position)
    {
        Collider2D waterCollider = Physics2D.OverlapCircle(position, 0.1f, waterLayer);
        if (waterCollider == null) return false;

        if (TryGetShaderDepth(waterCollider, position, out float shaderDepth))
        {
            return shaderDepth >= minimumShaderDepth;
        }

        if (minimumWaterDepth <= 0f) return true;

        int sampleCount = Mathf.Max(MinimumDepthSampleCount, depthSampleCount);
        for (int i = 0; i < sampleCount; i++)
        {
            float angle = i * (Mathf.PI * 2f / sampleCount);
            Vector2 sample = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * minimumWaterDepth;
            if (!waterCollider.OverlapPoint(sample)) return false;
        }

        return true;
    }

    private static bool TryGetShaderDepth(Collider2D waterCollider, Vector2 position, out float depth)
    {
        depth = 0f;
        Renderer waterRenderer = waterCollider.GetComponent<Renderer>();
        if (waterRenderer == null) waterRenderer = waterCollider.GetComponentInParent<Renderer>();
        if (waterRenderer == null) return false;

        Material waterMaterial = waterRenderer.sharedMaterial;
        if (waterMaterial == null ||
            !waterMaterial.HasProperty(SurfaceLevelProperty) ||
            !waterMaterial.HasProperty(DepthVerticalScaleProperty))
        {
            return false;
        }

        float depthScale = waterMaterial.GetFloat(DepthVerticalScaleProperty);
        float normalizedDistance = Mathf.Clamp01(
            (waterMaterial.GetFloat(SurfaceLevelProperty) - position.y) / Mathf.Max(0.001f, Mathf.Abs(depthScale)));
        depth = depthScale < 0f ? 1f - normalizedDistance : normalizedDistance;
        return true;
    }

}
