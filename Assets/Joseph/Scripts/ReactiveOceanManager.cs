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
    [Tooltip("Define tiers from lowest threshold to highest (e.g., Dead to Pristine).")]
    public List<OceanTier> tiers = new List<OceanTier>();

    [Header("Water Visuals")]
    public Material waterMaterial;
    public string colorPropertyName = "_BaseColor";
    [Tooltip("Optional: If your shader uses a separate float for transparency (e.g., _Opacity). Leave empty to use Color Alpha.")]
    public string transparencyPropertyName = "";
    public float colorTransitionSpeed = 1.5f;

    public Action<OceanTier> OnTierChanged;
    private OceanTier _currentTier;
    private int _colorID;
    private int _alphaID = -1;
    private Color _targetColor;

    void Awake()
    {
        Instance = this;
        _colorID = Shader.PropertyToID(colorPropertyName);
        if (!string.IsNullOrEmpty(transparencyPropertyName))
            _alphaID = Shader.PropertyToID(transparencyPropertyName);
    }

    void Start()
    {
        UpdateTier();
        
        // Initialize target color based on starting tier
        if (_currentTier != null)
        {
            _targetColor = _currentTier.waterColor;

            if (waterMaterial != null)
            {
                // Snap values immediately on start
                waterMaterial.SetColor(_colorID, _targetColor);
                if (_alphaID != -1) waterMaterial.SetFloat(_alphaID, _targetColor.a);
            }
        }

        if (SustainabilityManager.Instance != null)
        {
            SustainabilityManager.Instance.OnSustainabilityChanged.AddListener((val) => UpdateTier());
        }
    }

    void Update()
    {
        if (waterMaterial != null)
        {
            Color currentColor = waterMaterial.GetColor(_colorID);
            
            // If we are not yet at the target color, interpolate toward it
            if (ColorDistance(currentColor, _targetColor) > 0.001f)
            {
                Color nextColor = Color.Lerp(currentColor, _targetColor, Time.deltaTime * colorTransitionSpeed);
                waterMaterial.SetColor(_colorID, nextColor);

                if (_alphaID != -1)
                {
                    float currentA = waterMaterial.GetFloat(_alphaID);
                    waterMaterial.SetFloat(_alphaID, Mathf.Lerp(currentA, _targetColor.a, Time.deltaTime * colorTransitionSpeed));
                }
            }
        }
    }

    private float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
    }

    private void UpdateTier()
    {
        int currentSus = SustainabilityManager.Instance != null ? SustainabilityManager.Instance.CurrentSustainability : 0;
        
        OceanTier newTier = tiers.Count > 0 ? tiers[0] : null;
        foreach (var tier in tiers)
        {
            if (currentSus >= tier.threshold)
                newTier = tier;
        }

        if (newTier != _currentTier)
        {
            _currentTier = newTier;
            _targetColor = _currentTier.waterColor;
            OnTierChanged?.Invoke(_currentTier);
            Debug.Log($"Ocean State Changed to: {_currentTier.tierName}");
        }
    }

    public OceanTier GetCurrentTier() => _currentTier;

    /// <summary>
    /// Returns a random catch from the current tier's pools.
    /// </summary>
    public ItemData GetRandomCatch()
    {
        if (_currentTier == null) return null;

        float roll = UnityEngine.Random.value;

        // 1. Rare Artifact roll (rarest)
        if (_currentTier.artifactPool != null && _currentTier.artifactPool.Length > 0 && roll < _currentTier.artifactProbability)
        {
            return _currentTier.artifactPool[UnityEngine.Random.Range(0, _currentTier.artifactPool.Length)];
        }

        // 2. Junk roll
        if (UnityEngine.Random.value < _currentTier.junkProbability && _currentTier.junkPool.Length > 0)
        {
            return _currentTier.junkPool[UnityEngine.Random.Range(0, _currentTier.junkPool.Length)];
        }

        if (_currentTier.fishPool.Length > 0)
        {
            return _currentTier.fishPool[UnityEngine.Random.Range(0, _currentTier.fishPool.Length)];
        }

        return null;
    }
}