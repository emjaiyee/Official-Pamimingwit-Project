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

    /// <summary>
    /// Dimming weight contributed by current weather (0 = sunny, higher = darker).
    /// </summary>
    public float WeatherDimWeight { get; private set; } = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ApplyWeather();
    }

    private void OnEnable()
    {
        GameManager.OnDayAdvanced += OnNewDay;
        ApplyWeather();
    }

    private void OnDisable()
    {
        GameManager.OnDayAdvanced -= OnNewDay;
    }

    /// <summary>
    /// Sets the current weather state and immediately applies the matching visual/audio setup.
    /// </summary>
    public void SetWeather(WeatherState newWeather, bool applyImmediately = true)
    {
        CurrentWeather = newWeather;

        if (applyImmediately)
        {
            ApplyWeather();
        }
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
        switch (CurrentWeather)
        {
            case WeatherState.Sunny:
                WeatherDimWeight = 0f;
                break;
            case WeatherState.Cloudy:
                WeatherDimWeight = 0.15f;
                break;
            case WeatherState.Raining:
                WeatherDimWeight = 0.25f;
                break;
        }

        if (globalLight2D != null)
        {
            switch (CurrentWeather)
            {
                case WeatherState.Sunny:
                    globalLight2D.intensity = 1f;
                    break;
                case WeatherState.Cloudy:
                    globalLight2D.intensity = 0.7f;
                    break;
                case WeatherState.Raining:
                    globalLight2D.intensity = 0.4f;
                    break;
            }
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ControlPPV();

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
