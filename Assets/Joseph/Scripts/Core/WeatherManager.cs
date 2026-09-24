using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    public WeatherState CurrentWeather { get; private set; } = WeatherState.Sunny;

    public enum WeatherState
    {
        Sunny,
        Cloudy,
        Raining
    }

    [Header("URP 2D Lighting")]
    [SerializeField] private Light2D globalLight2D;

    [Header("Rain Effects")]
    [SerializeField] private ParticleSystem rainParticleSystem;
    [SerializeField] private AudioSource rainAudioSource;

    [Header("Transition Settings")]
    [Tooltip("Duration in seconds for lighting and audio fade transitions.")]
    [SerializeField] private float transitionDuration = 2.5f;

    [Header("Target Intensity Values")]
    [SerializeField] private float sunnyLightIntensity = 1.0f;
    [SerializeField] private float cloudyLightIntensity = 0.7f;
    [SerializeField] private float rainyLightIntensity = 0.4f;

    [Header("Target Audio Volumes")]
    [SerializeField] private float maxRainVolume = 0.8f;

    public float WeatherDimWeight { get; private set; } = 0f;

    private Coroutine transitionCoroutine;

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

    public void SetWeather(WeatherState newWeather, bool applyImmediately = false)
    {
        CurrentWeather = newWeather;
        ApplyWeather(applyImmediately);
    }

    private void OnNewDay()
    {
        RollWeather();
        ApplyWeather(false);
    }

    private void RollWeather()
    {
        int roll = Random.Range(0, 100);
        if (roll < 50)
            CurrentWeather = WeatherState.Sunny;
        else if (roll < 80)
            CurrentWeather = WeatherState.Cloudy;
        else
            CurrentWeather = WeatherState.Raining;
    }

    private void ApplyWeather(bool immediate)
    {
        float targetIntensity = sunnyLightIntensity;
        float targetVolume = 0f;
        bool isRaining = CurrentWeather == WeatherState.Raining;

        switch (CurrentWeather)
        {
            case WeatherState.Sunny:
                WeatherDimWeight = 0f;
                targetIntensity = sunnyLightIntensity;
                targetVolume = 0f;
                break;

            case WeatherState.Cloudy:
                WeatherDimWeight = 0.15f;
                targetIntensity = cloudyLightIntensity;
                targetVolume = 0f;
                break;

            case WeatherState.Raining:
                WeatherDimWeight = 0.25f;
                targetIntensity = rainyLightIntensity;
                targetVolume = maxRainVolume;
                break;
        }

        // Particle System Emission Control
        if (rainParticleSystem != null)
        {
            var emission = rainParticleSystem.emission;
            emission.enabled = isRaining;

            if (isRaining && !rainParticleSystem.isPlaying)
                rainParticleSystem.Play();
        }

        // Interrupt active transitions to prevent blending conflicts
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        if (immediate)
        {
            if (globalLight2D != null) globalLight2D.intensity = targetIntensity;
            if (rainAudioSource != null)
            {
                rainAudioSource.volume = targetVolume;
                if (isRaining && !rainAudioSource.isPlaying) rainAudioSource.Play();
                else if (!isRaining) rainAudioSource.Stop();
            }
        }
        else
        {
            transitionCoroutine = StartCoroutine(TransitionWeatherRoutine(targetIntensity, targetVolume, isRaining));
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ControlPPV();
    }

    private IEnumerator TransitionWeatherRoutine(float targetIntensity, float targetVolume, bool isRaining)
    {
        float elapsedTime = 0f;

        float startIntensity = globalLight2D != null ? globalLight2D.intensity : targetIntensity;
        float startVolume = rainAudioSource != null ? rainAudioSource.volume : 0f;

        if (isRaining && rainAudioSource != null && !rainAudioSource.isPlaying)
        {
            rainAudioSource.volume = 0f;
            rainAudioSource.Play();
        }

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (globalLight2D != null)
            {
                globalLight2D.intensity = Mathf.Lerp(startIntensity, targetIntensity, smoothT);
            }

            if (rainAudioSource != null)
            {
                rainAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, smoothT);
            }

            yield return null;
        }

        if (globalLight2D != null) globalLight2D.intensity = targetIntensity;
        if (rainAudioSource != null)
        {
            rainAudioSource.volume = targetVolume;
            if (!isRaining && rainAudioSource.volume <= 0.01f)
            {
                rainAudioSource.Stop();
            }
        }

        transitionCoroutine = null;
    }
}