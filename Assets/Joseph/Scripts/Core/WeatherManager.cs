using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

/// <summary>
/// Manages daily weather states for the game. On each new day a weighted random weather
/// (Sunny 50%, Cloudy 30%, Raining 20%) is selected, the global 2D lighting is updated and
/// rain particle/effects are toggled. Other gameplay systems can query the current weather
/// via the public <c>CurrentWeather</c> property.
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    /// <summary>
    /// Current weather state. Read‑only for external code.
    /// </summary>
    public WeatherState CurrentWeather { get; private set; } = WeatherState.Sunny;

    /// <summary>
    /// Possible weather states.
    /// </summary>
    public enum WeatherState
    {
        Sunny,
        Cloudy,
        Raining
    }

    [Header("URP 2D Lighting")]
    [Tooltip("Reference to the scene's Global Light 2D used for ambient lighting adjustments.")]
    [SerializeField] private Light2D globalLight2D;

    [Header("Rain Effects")]
    [Tooltip("Particle system that renders rain drops.")]
    [SerializeField] private ParticleSystem rainParticleSystem;
    [Tooltip("AudioSource that plays rain ambience.")]
    [SerializeField] private AudioSource rainAudioSource;

    [Header("Lighting Profiles (optional)")]
    [Tooltip("Ambient colour for a sunny day.")]
    [SerializeField] private Color sunnyColor = new Color(1f, 0.95f, 0.8f);
    [Tooltip("Ambient colour for a cloudy day.")]
    [SerializeField] private Color cloudyColor = new Color(0.8f, 0.85f, 0.9f);
    [Tooltip("Ambient colour for a rainy day.")]
    [SerializeField] private Color rainyColor = new Color(0.6f, 0.66f, 0.8f);

    [Tooltip("Intensity of the global light during sunny weather.")]
    [SerializeField] private float sunnyIntensity = 1f;
    [Tooltip("Intensity of the global light during cloudy weather.")]
    [SerializeField] private float cloudyIntensity = 0.7f;
    [Tooltip("Intensity of the global light during rainy weather.")]
    [SerializeField] private float rainyIntensity = 0.4f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.OnDayAdvanced += OnNewDay;
    }

    private void OnDisable()
    {
        GameManager.OnDayAdvanced -= OnNewDay;
    }

    /// <summary>
    /// Called each time the day advances. Rolls a new weather state and applies the corresponding effects.
    /// </summary>
    private void OnNewDay()
    {
        RollWeather();
        ApplyWeather();
    }

    /// <summary>
    /// Chooses a weather state based on the defined weighted probabilities.
    /// </summary>
    private void RollWeather()
    {
        // Unity's Random.Range with int is inclusive‑exclusive (min inclusive, max exclusive).
        int roll = UnityEngine.Random.Range(0, 100);
        if (roll < 50) // 0‑49 -> Sunny (50%)
        {
            CurrentWeather = WeatherState.Sunny;
        }
        else if (roll < 80) // 50‑79 -> Cloudy (30%)
        {
            CurrentWeather = WeatherState.Cloudy;
        }
        else // 80‑99 -> Raining (20%)
        {
            CurrentWeather = WeatherState.Raining;
        }
    }

    /// <summary>
    /// Updates lighting and particle/audio based on <c>CurrentWeather</c>.
    /// </summary>
    private void ApplyWeather()
    {
        if (globalLight2D != null)
        {
            switch (CurrentWeather)
            {
                case WeatherState.Sunny:
                    globalLight2D.color = sunnyColor;
                    globalLight2D.intensity = sunnyIntensity;
                    break;
                case WeatherState.Cloudy:
                    globalLight2D.color = cloudyColor;
                    globalLight2D.intensity = cloudyIntensity;
                    break;
                case WeatherState.Raining:
                    globalLight2D.color = rainyColor;
                    globalLight2D.intensity = rainyIntensity;
                    break;
            }
        }

        // Rain particles and audio are only active during the Raining state.
        bool rainActive = CurrentWeather == WeatherState.Raining;
        if (rainParticleSystem != null)
        {
            var emission = rainParticleSystem.emission;
            emission.enabled = rainActive;
            if (rainActive && !rainParticleSystem.isPlaying)
                rainParticleSystem.Play();
            else if (!rainActive && rainParticleSystem.isPlaying)
                rainParticleSystem.Stop();
        }
        if (rainAudioSource != null)
        {
            rainAudioSource.enabled = rainActive;
            if (rainActive && !rainAudioSource.isPlaying)
                rainAudioSource.Play();
            else if (!rainActive && rainAudioSource.isPlaying)
                rainAudioSource.Stop();
        }
    }
}

