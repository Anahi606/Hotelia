using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSettingsPausePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio")]
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParameter = "MusicVolume";

    [Header("Video")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private struct ResolutionOption
    {
        public int width;
        public int height;

        public ResolutionOption(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
    }

    private static readonly ResolutionOption[] allowed16By9Resolutions =
    {
        new ResolutionOption(3840, 2160), //4K
        new ResolutionOption(2560, 1440), //1440p
        new ResolutionOption(1920, 1080), //1080p
        new ResolutionOption(1600, 900),  //900p
        new ResolutionOption(1280, 720)   //720p
    };

    private readonly List<ResolutionOption> resolutionOptions =
        new List<ResolutionOption>();

    private const string MusicVolumeKey = "MusicVolume";
    private const string ResolutionWidthKey = "ResolutionWidth";
    private const string ResolutionHeightKey = "ResolutionHeight";
    private const string FullscreenKey = "Fullscreen";

    private float originalMusicVolume;
    private int originalResolutionWidth;
    private int originalResolutionHeight;
    private bool originalFullscreen;

    private bool isOpen;
    private bool isLoadingUI;
    private bool hasPauseRequest;

    private Coroutine resolutionCoroutine;

    private void Awake()
    {
        BuildResolutionDropdown();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        float savedVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.wholeNumbers = false;
            musicVolumeSlider.SetValueWithoutNotify(savedVolume);

            musicVolumeSlider.onValueChanged.AddListener(
                PreviewMusicVolume
            );
        }

        SetMixerMusicVolume(savedVolume);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);

        if (backButton != null)
            backButton.onClick.AddListener(CloseWithoutApplying);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDestroy()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(
                PreviewMusicVolume
            );
        }

        if (applyButton != null)
            applyButton.onClick.RemoveListener(ApplySettings);

        if (backButton != null)
            backButton.onClick.RemoveListener(CloseWithoutApplying);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);

        if (hasPauseRequest)
        {
            HotelGamePause.ReleasePause();
            hasPauseRequest = false;
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        ToggleSettingsPanel();
    }

    public void ToggleSettingsPanel()
    {
        if (isOpen)
            CloseWithoutApplying();
        else
            OpenSettingsPanel();
    }

    public void OpenSettingsPanel()
    {
        if (isOpen)
            return;

        isOpen = true;

        CaptureOriginalSettings();
        LoadOriginalSettingsIntoUI();

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        HotelGamePause.RequestPause();
        hasPauseRequest = true;
    }

    public void CloseWithoutApplying()
    {
        if (!isOpen)
            return;

        RestoreOriginalSettings();
        ClosePanelOnly();
    }

    private void ClosePanelOnly()
    {
        isOpen = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (hasPauseRequest)
        {
            HotelGamePause.ReleasePause();
            hasPauseRequest = false;
        }
    }

    private void CaptureOriginalSettings()
    {
        originalMusicVolume = PlayerPrefs.GetFloat(
            MusicVolumeKey,
            0.8f
        );

        originalResolutionWidth = PlayerPrefs.GetInt(
            ResolutionWidthKey,
            Screen.width
        );

        originalResolutionHeight = PlayerPrefs.GetInt(
            ResolutionHeightKey,
            Screen.height
        );

        originalFullscreen = PlayerPrefs.GetInt(
            FullscreenKey,
            Screen.fullScreen ? 1 : 0
        ) == 1;
    }

    private void LoadOriginalSettingsIntoUI()
    {
        isLoadingUI = true;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(
                originalMusicVolume
            );
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(
                originalFullscreen
            );
        }

        int resolutionIndex = GetResolutionIndex(
            originalResolutionWidth,
            originalResolutionHeight
        );

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(
                resolutionIndex
            );

            resolutionDropdown.RefreshShownValue();
        }

        isLoadingUI = false;
    }

    private void RestoreOriginalSettings()
    {
        SetMixerMusicVolume(originalMusicVolume);

        StartResolutionChange(
            originalResolutionWidth,
            originalResolutionHeight,
            originalFullscreen
        );

        LoadOriginalSettingsIntoUI();

        Debug.Log(
            "Settings restored. Unsaved changes were discarded."
        );
    }

    private void ApplySettings()
    {
        ApplyAudioSettings();

        if (!ApplyVideoSettings())
            return;

        PlayerPrefs.Save();

        CaptureOriginalSettings();

        Debug.Log("Gameplay settings applied.");
    }

    private void ApplyAudioSettings()
    {
        if (musicVolumeSlider == null)
            return;

        float volume = Mathf.Clamp01(
            musicVolumeSlider.value
        );

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            volume
        );

        SetMixerMusicVolume(volume);
    }

    private bool ApplyVideoSettings()
    {
        if (resolutionDropdown == null ||
            fullscreenToggle == null)
        {
            Debug.LogError(
                "GameSettingsPausePanelUI: Missing resolution dropdown or fullscreen toggle."
            );

            return false;
        }

        int selectedIndex = resolutionDropdown.value;

        if (selectedIndex < 0 ||
            selectedIndex >= resolutionOptions.Count)
        {
            Debug.LogError(
                "GameSettingsPausePanelUI: Invalid resolution selected."
            );

            return false;
        }

        ResolutionOption selectedResolution =
            resolutionOptions[selectedIndex];

        bool fullscreen = fullscreenToggle.isOn;

        PlayerPrefs.SetInt(
            ResolutionWidthKey,
            selectedResolution.width
        );

        PlayerPrefs.SetInt(
            ResolutionHeightKey,
            selectedResolution.height
        );

        PlayerPrefs.SetInt(
            FullscreenKey,
            fullscreen ? 1 : 0
        );

        StartResolutionChange(
            selectedResolution.width,
            selectedResolution.height,
            fullscreen
        );

        return true;
    }

    private void PreviewMusicVolume(float value)
    {
        if (isLoadingUI)
            return;

        value = Mathf.Clamp01(value);

        SetMixerMusicVolume(value);
    }

    private void StartResolutionChange(
        int width,
        int height,
        bool fullscreen
    )
    {
        if (resolutionCoroutine != null)
        {
            StopCoroutine(resolutionCoroutine);
            resolutionCoroutine = null;
        }

        resolutionCoroutine = StartCoroutine(
            ApplyResolutionRoutine(
                width,
                height,
                fullscreen
            )
        );
    }

    private IEnumerator ApplyResolutionRoutine(
    int width,
    int height,
    bool fullscreen
)
    {
        FullScreenMode screenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(
            width,
            height,
            screenMode
        );

        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            $"Resolution applied: {width} x {height} | " +
            $"Fullscreen: {fullscreen}"
        );

        resolutionCoroutine = null;
    }

    private void SetMixerMusicVolume(float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning(
                "GameSettingsPausePanelUI: No asignaste el AudioMixer en el Inspector."
            );

            return;
        }

        volume = Mathf.Clamp01(volume);

        float volumeDb = volume <= 0.0001f
            ? -80f
            : Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(
            musicVolumeParameter,
            volumeDb
        );
    }

    private void BuildResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogError(
                "GameSettingsPausePanelUI: Resolution Dropdown is missing."
            );

            return;
        }

        resolutionDropdown.ClearOptions();
        resolutionOptions.Clear();

        Resolution[] supportedResolutions =
            Screen.resolutions;

        foreach (
            ResolutionOption allowed
            in allowed16By9Resolutions
        )
        {
            bool isSupported = supportedResolutions.Any(
                resolution =>
                    resolution.width == allowed.width &&
                    resolution.height == allowed.height
            );

            if (isSupported)
            {
                resolutionOptions.Add(allowed);
            }
        }

        // Protección para que nunca quede vacío.
        if (resolutionOptions.Count == 0)
        {
            resolutionOptions.Add(
                new ResolutionOption(
                    Screen.width,
                    Screen.height
                )
            );
        }

        List<string> dropdownOptions =
            resolutionOptions
                .Select(
                    option =>
                        option.width +
                        " x " +
                        option.height
                )
                .ToList();

        resolutionDropdown.AddOptions(
            dropdownOptions
        );

        resolutionDropdown.RefreshShownValue();
    }

    private int GetResolutionIndex(
        int width,
        int height
    )
    {
        for (
            int i = 0;
            i < resolutionOptions.Count;
            i++
        )
        {
            if (
                resolutionOptions[i].width == width &&
                resolutionOptions[i].height == height
            )
            {
                return i;
            }
        }

        for (
            int i = 0;
            i < resolutionOptions.Count;
            i++
        )
        {
            if (
                resolutionOptions[i].width == Screen.width &&
                resolutionOptions[i].height == Screen.height
            )
            {
                return i;
            }
        }

        return 0;
    }

    private void ReturnToMainMenu()
    {
        RestoreOriginalSettings();

        if (hasPauseRequest)
        {
            HotelGamePause.ReleasePause();
            hasPauseRequest = false;
        }

        HotelGamePause.ForceResume();

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }

    private void QuitGame()
    {
        HotelGamePause.ForceResume();

#if UNITY_EDITOR
        Debug.Log(
            "QuitGame called. Application.Quit only works in a build."
        );
#else
        Application.Quit();
#endif
    }
}