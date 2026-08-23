using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class OceanTier
{
    public string tierName;
    [Tooltip("Minimum sustainability value to enter this tier.")]
    public int threshold;
    public FishData[] fishPool;
    public ItemData[] junkPool;
    public ArtifactData[] artifactPool;
    [Range(0, 1)] public float artifactProbability = 0.02f;
    [Range(0, 1)] public float junkProbability;
    [Tooltip("Multiplier for dynamite haul size.")]
    public float haulMultiplier = 1f;
    [Tooltip("The color of the water for this tier.")]
    public Color waterColor = new Color(0, 0.5f, 1f, 1f);
}

public class ReactiveOceanManager : MonoBehaviour
{
    public static ReactiveOceanManager Instance;

    [Header("Ocean Tiers")]
    [Tooltip("Define tiers in any order (sorted internally by threshold).")]
    public List<OceanTier> tiers = new List<OceanTier>();

    [Header("Water Visuals")]
    [SerializeField] private Renderer waterRenderer;
    public string colorPropertyName = "_BaseColor";
    [Tooltip("Optional: Shader property for transparency. Leave empty to use Color Alpha.")]
    public string transparencyPropertyName = "";
    public float colorTransitionSpeed = 1.5f;

    public Action<OceanTier> OnTierChanged;
    
    private OceanTier _currentTier;
    private Material _instancedWaterMaterial;
    private int _colorID;
    private int _alphaID = -1;
    private Color _targetColor;
    private Color _currentColor;

    void Awake()
    {
        Instance = this;
        _colorID = Shader.PropertyToID(colorPropertyName);
        
        if (!string.IsNullOrEmpty(transparencyPropertyName))
            _alphaID = Shader.PropertyToID(transparencyPropertyName);

        if (waterRenderer != null)
        {
            // .material creates an instance copy, preventing project asset mutation
            _instancedWaterMaterial = waterRenderer.material; 
        }
        else
        {
            Debug.LogWarning("ReactiveOceanManager: Water Renderer reference is missing in the Inspector!");
        }
    }

    void OnEnable()
    {
        if (SustainabilityManager.Instance != null)
        {
            SustainabilityManager.Instance.OnSustainabilityChanged.AddListener(OnSustainabilityUpdated);
        }
    }

    void OnDisable()
    {
        if (SustainabilityManager.Instance != null)
        {
            SustainabilityManager.Instance.OnSustainabilityChanged.RemoveListener(OnSustainabilityUpdated);
        }
    }

    void Start()
    {
        UpdateTier();
        
        if (_currentTier != null)
        {
            _targetColor = _currentTier.waterColor;
            _currentColor = _targetColor;

            if (_instancedWaterMaterial != null)
            {
                _instancedWaterMaterial.SetColor(_colorID, _targetColor);
                if (_alphaID != -1) _instancedWaterMaterial.SetFloat(_alphaID, _targetColor.a);
            }
        }
    }

    void Update()
    {
        if (_instancedWaterMaterial == null) return;

        // Compare cached local variables instead of executing native GetColor queries
        if (ColorDistance(_currentColor, _targetColor) > 0.001f)
        {
            _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * colorTransitionSpeed);
            _instancedWaterMaterial.SetColor(_colorID, _currentColor);

            if (_alphaID != -1)
            {
                _instancedWaterMaterial.SetFloat(_alphaID, _currentColor.a);
            }
        }
    }

    private float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
    }

    private void OnSustainabilityUpdated(int val)
    {
        UpdateTier();
    }

    private void UpdateTier()
    {
        int currentSus = SustainabilityManager.Instance != null ? SustainabilityManager.Instance.CurrentSustainability : 0;
        
        OceanTier newTier = null;

        // Evaluates highest qualified threshold regardless of Inspector list arrangement
        for (int i = 0; i < tiers.Count; i++)
        {
            if (currentSus >= tiers[i].threshold)
            {
                if (newTier == null || tiers[i].threshold > newTier.threshold)
                {
                    newTier = tiers[i];
                }
            }
        }

        // Fallback to lowest defined tier if sustainability is below all thresholds
        if (newTier == null && tiers.Count > 0)
        {
            newTier = tiers[0];
            for (int i = 1; i < tiers.Count; i++)
            {
                if (tiers[i].threshold < newTier.threshold)
                    newTier = tiers[i];
            }
        }

        if (newTier != null && newTier != _currentTier)
        {
            _currentTier = newTier;
            _targetColor = _currentTier.waterColor;
            OnTierChanged?.Invoke(_currentTier);
            Debug.Log($"Ocean State Changed to: {_currentTier.tierName}");
        }
    }

    public OceanTier GetCurrentTier() => _currentTier;

    public ItemData GetRandomCatch()
    {
        if (_currentTier == null) return null;

        float roll = UnityEngine.Random.value;

        // Rare Artifact roll (rarest)
        if (_currentTier.artifactPool != null && _currentTier.artifactPool.Length > 0 && roll < _currentTier.artifactProbability)
        {
            return _currentTier.artifactPool[UnityEngine.Random.Range(0, _currentTier.artifactPool.Length)];
        }

        // Junk roll
        if (_currentTier.junkPool != null && _currentTier.junkPool.Length > 0 && UnityEngine.Random.value < _currentTier.junkProbability)
        {
            return _currentTier.junkPool[UnityEngine.Random.Range(0, _currentTier.junkPool.Length)];
        }

        // Fish roll
        if (_currentTier.fishPool != null && _currentTier.fishPool.Length > 0)
        {
            return _currentTier.fishPool[UnityEngine.Random.Range(0, _currentTier.fishPool.Length)];
        }

        return null;
    }

    private void OnDestroy()
    {
        if (_instancedWaterMaterial != null)
        {
            Destroy(_instancedWaterMaterial);
        }
    }
}