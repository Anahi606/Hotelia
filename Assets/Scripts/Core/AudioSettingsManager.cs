using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    private const string MusicVolumeKey = "MusicVolume";

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    public float CurrentMusicVolume { get; private set; } = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        CurrentMusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f));
        ApplyMusicVolume(CurrentMusicVolume);
    }

    public void PreviewMusicVolume(float volume)
    {
        CurrentMusicVolume = Mathf.Clamp01(volume);
        ApplyMusicVolume(CurrentMusicVolume);
    }

    public void SetMusicVolume(float volume)
    {
        CurrentMusicVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(MusicVolumeKey, CurrentMusicVolume);
        PlayerPrefs.Save();

        ApplyMusicVolume(CurrentMusicVolume);
    }

    private void ApplyMusicVolume(float volume)
    {
        if (musicSource == null)
        {
            Debug.LogWarning("AudioSettingsManager: No Music Source assigned.");
            return;
        }

        musicSource.volume = volume;
    }
}