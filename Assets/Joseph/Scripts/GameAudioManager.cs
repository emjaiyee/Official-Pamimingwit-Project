using UnityEngine;

public class GameAudioManager : MonoBehaviour
{

    public static GameAudioManager Instance { get; private set; }

    [Header("Audio")]
    public AudioClip panelOpenSFX;
    public AudioClip panelCloseSFX;
    public AudioClip typewriterSFX;
    public AudioClip roosterSFX;
    public AudioClip taxPaidSFX;
    public AudioClip taxFailedSFX;
    public AudioClip morningAmbienceSFX;
    private AudioSource sfxSource;
    private AudioSource ambienceSource;
    private AudioSource musicSource;
    private AudioSource typewriterSource;

    private void Awake()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        ambienceSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        typewriterSource = gameObject.AddComponent<AudioSource>();
         
        ambienceSource.loop = true;
    }

    private void OnEnable() => GameEvents.OnPlaySound += PlayAudio;
    private void OnDisable() => GameEvents.OnPlaySound -= PlayAudio;

    private void PlayAudio(SoundType soundType)
    {
        switch (soundType)
        {
            case SoundType.PanelOpen:
                sfxSource.PlayOneShot(panelOpenSFX);
                break;
            case SoundType.PanelClose:
                sfxSource.PlayOneShot(panelCloseSFX);
                break;
                break;
            case SoundType.Rooster:
                sfxSource.PlayOneShot(roosterSFX);
                break;
            case SoundType.TaxPaid:
                sfxSource.PlayOneShot(taxPaidSFX);
                break;
            case SoundType.TaxFailed:
                sfxSource.PlayOneShot(taxFailedSFX);
                break;
            case SoundType.MorningAmbience:
                PlayAmbience(morningAmbienceSFX);
                break;
            case SoundType.StopTransitionAudio:
                if (ambienceSource != null)
                {
                    ambienceSource.Stop();
                    ambienceSource.clip = null;
                    ambienceSource.loop = false;
                    ambienceSource.volume = 1f;
                }
                break;
        }
    }

    private void PlayAmbience(AudioClip clip)
    {
        if (ambienceSource.clip == clip && ambienceSource.isPlaying) return;
        ambienceSource.clip = clip;
        ambienceSource.Play();
    }

}
