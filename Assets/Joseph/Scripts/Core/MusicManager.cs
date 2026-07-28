using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    private static MusicManager Instance;
    private AudioSource audioSource;

    [Header("Menu Music")]
    [SerializeField] private List<AudioClip> menuTracks = new List<AudioClip>();

    [Header("Game Music")]
    [SerializeField] private List<AudioClip> gameTracks = new List<AudioClip>();

    private List<AudioClip> currentPlaylist;
    private int currentTrackIndex = 0;

    [Header("UI Sliders")]
    private Slider menuMusicSlider;
    private Slider gameMusicSlider;

    private float currentVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();

            DontDestroyOnLoad(gameObject);

            audioSource.loop = false;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdatePlaylist(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        audioSource.volume = currentVolume;

        if (!audioSource.isPlaying &&
            currentPlaylist != null &&
            currentPlaylist.Count > 0)
        {
            PlayNextTrack();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdatePlaylist(scene.name);
    }

    private void UpdatePlaylist(string sceneName)
    {
        bool isMenuScene = sceneName == "MenuScene";

        List<AudioClip> newPlaylist = isMenuScene ? menuTracks : gameTracks;

        if (currentPlaylist == newPlaylist)
            return;

        currentPlaylist = newPlaylist;
        currentTrackIndex = 0;

        if (currentPlaylist.Count > 0)
        {
            PlayTrack(currentTrackIndex);
        }
    }

    // -------------------------
    // 🔥 SLIDER REGISTRATION
    // -------------------------

    public void RegisterMenuSlider(Slider slider)
    {
        menuMusicSlider = slider;

        menuMusicSlider.SetValueWithoutNotify(currentVolume);
        menuMusicSlider.onValueChanged.RemoveAllListeners();
        menuMusicSlider.onValueChanged.AddListener(OnMenuSliderChanged);
    }

    public void RegisterGameSlider(Slider slider)
    {
        gameMusicSlider = slider;

        gameMusicSlider.SetValueWithoutNotify(currentVolume);
        gameMusicSlider.onValueChanged.RemoveAllListeners();
        gameMusicSlider.onValueChanged.AddListener(OnGameSliderChanged);
    }

    // -------------------------
    // 🔊 SLIDER EVENTS
    // -------------------------

    private void OnMenuSliderChanged(float value)
    {
        currentVolume = value;
        audioSource.volume = currentVolume;

        if (gameMusicSlider != null)
            gameMusicSlider.SetValueWithoutNotify(value);
    }

    private void OnGameSliderChanged(float value)
    {
        currentVolume = value;
        audioSource.volume = currentVolume;

        if (menuMusicSlider != null)
            menuMusicSlider.SetValueWithoutNotify(value);
    }

    // -------------------------
    // 🎵 MUSIC CONTROL
    // -------------------------

    private void PlayTrack(int index)
    {
        if (currentPlaylist == null || currentPlaylist.Count == 0)
            return;

        audioSource.clip = currentPlaylist[index];
        audioSource.volume = currentVolume;
        audioSource.Play();
    }

    public void PlayNextTrack()
    {
        currentTrackIndex++;

        if (currentTrackIndex >= currentPlaylist.Count)
            currentTrackIndex = 0;

        PlayTrack(currentTrackIndex);
    }

    public void PauseBackgroundMusic()
    {
        audioSource.Pause();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}