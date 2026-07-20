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

    [Header("Input")]
    [SerializeField] private bool listenForEscapeDirectly = false;
    [SerializeField] private bool pauseGameWhenOpen = true;

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
        new ResolutionOption(3840, 2160), // 4K
        new ResolutionOption(2560, 1440), // 1440p
        new ResolutionOption(1920, 1080), // 1080p
        new ResolutionOption(1600, 900),  // 900p
        new ResolutionOption(1280, 720)   // 720p
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
        {
            settingsPanel.SetActive(false);
        }

        LoadSavedAudioSettings();
        ConfigureListeners();
        LoadSavedVideoSettingsIntoUI();
    }

    private bool IsAnyTutorialBlockingInput()
    {
        return
            CheckInTutorialBookUI.IsBlockingGameInput ||
            RestaurantTutorialBookUI.IsBlockingGameInput ||
            RoomCleaningTutorialBookUI.IsBlockingGameInput;
    }

    private void Update()
    {
        if (!listenForEscapeDirectly)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (!isOpen && IsAnyTutorialBlockingInput())
        {
            Debug.Log(
                "Settings blocked while the tutorial is open."
            );

            return;
        }

        Debug.Log(
            "ESC presionado en el panel de configuración."
        );

        ToggleSettingsPanel();
    }

    private void OnDestroy()
    {
        RemoveListeners();

        if (resolutionCoroutine != null)
        {
            StopCoroutine(resolutionCoroutine);
            resolutionCoroutine = null;
        }

        if (hasPauseRequest)
        {
            HotelGamePause.ReleasePause();
            hasPauseRequest = false;
        }
    }

    private void ConfigureListeners()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(
                PreviewMusicVolume
            );
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(
                UpdateResolutionDropdownState
            );
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(
                ApplySettings
            );
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(
                CloseWithoutApplying
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(
                ReturnToMainMenu
            );
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(
                QuitGame
            );
        }
    }

    private void RemoveListeners()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(
                PreviewMusicVolume
            );
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(
                UpdateResolutionDropdownState
            );
        }

        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(
                ApplySettings
            );
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(
                CloseWithoutApplying
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(
                ReturnToMainMenu
            );
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(
                QuitGame
            );
        }
    }

    private void LoadSavedAudioSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat(
            MusicVolumeKey,
            0.8f
        );

        savedVolume = Mathf.Clamp01(savedVolume);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.wholeNumbers = false;

            musicVolumeSlider.SetValueWithoutNotify(
                savedVolume
            );
        }

        SetMixerMusicVolume(savedVolume);
    }

    private void LoadSavedVideoSettingsIntoUI()
    {
        bool savedFullscreen = PlayerPrefs.GetInt(
            FullscreenKey,
            Screen.fullScreen ? 1 : 0
        ) == 1;

        int savedWidth = PlayerPrefs.GetInt(
            ResolutionWidthKey,
            Screen.width
        );

        int savedHeight = PlayerPrefs.GetInt(
            ResolutionHeightKey,
            Screen.height
        );

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(
                savedFullscreen
            );
        }

        int resolutionIndex;

        if (savedFullscreen)
        {
            Resolution nativeResolution =
                Screen.currentResolution;

            resolutionIndex = GetResolutionIndex(
                nativeResolution.width,
                nativeResolution.height
            );
        }
        else
        {
            resolutionIndex = GetResolutionIndex(
                savedWidth,
                savedHeight
            );
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(
                resolutionIndex
            );

            resolutionDropdown.RefreshShownValue();
        }

        UpdateResolutionDropdownState(savedFullscreen);
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!isOpen && IsAnyTutorialBlockingInput())
        {
            Debug.Log(
                "Pause input blocked while the tutorial is open."
            );

            return;
        }

        ToggleSettingsPanel();
    }

    public void ToggleSettingsPanel()
    {
        if (isOpen)
        {
            CloseWithoutApplying();
            return;
        }

        if (IsAnyTutorialBlockingInput())
        {
            Debug.Log(
                "Settings cannot be opened while a tutorial is active."
            );

            return;
        }

        OpenSettingsPanel();
    }

    public void OpenSettingsPanel()
    {
        if (IsAnyTutorialBlockingInput())
        {
            Debug.Log(
                "Settings cannot be opened while a tutorial is active."
            );

            return;
        }

        if (isOpen)
            return;

        if (settingsPanel == null)
        {
            Debug.LogError(
                "GameSettingsPausePanelUI: Settings Panel no está asignado."
            );

            return;
        }

        isOpen = true;

        CaptureOriginalSettings();
        LoadOriginalSettingsIntoUI();

        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();

        if (pauseGameWhenOpen && !hasPauseRequest)
        {
            HotelGamePause.RequestPause();
            hasPauseRequest = true;
        }
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
        {
            settingsPanel.SetActive(false);
        }

        if (pauseGameWhenOpen && hasPauseRequest)
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

        int resolutionIndex;

        if (originalFullscreen)
        {
            Resolution nativeResolution =
                Screen.currentResolution;

            resolutionIndex = GetResolutionIndex(
                nativeResolution.width,
                nativeResolution.height
            );
        }
        else
        {
            resolutionIndex = GetResolutionIndex(
                originalResolutionWidth,
                originalResolutionHeight
            );
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(
                resolutionIndex
            );

            resolutionDropdown.RefreshShownValue();
        }

        UpdateResolutionDropdownState(
            originalFullscreen
        );

        isLoadingUI = false;
    }

    private void RestoreOriginalSettings()
    {
        SetMixerMusicVolume(
            originalMusicVolume
        );

        int width = originalResolutionWidth;
        int height = originalResolutionHeight;

        if (originalFullscreen)
        {
            Resolution nativeResolution =
                Screen.currentResolution;

            width = nativeResolution.width;
            height = nativeResolution.height;
        }

        StartResolutionChange(
            width,
            height,
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

        bool fullscreen = fullscreenToggle.isOn;

        int width;
        int height;

        if (fullscreen)
        {
            Resolution nativeResolution =
                Screen.currentResolution;

            width = nativeResolution.width;
            height = nativeResolution.height;

            int nativeIndex = GetResolutionIndex(
                width,
                height
            );

            resolutionDropdown.SetValueWithoutNotify(
                nativeIndex
            );

            resolutionDropdown.RefreshShownValue();
        }
        else
        {
            int selectedIndex =
                resolutionDropdown.value;

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

            width = selectedResolution.width;
            height = selectedResolution.height;
        }

        PlayerPrefs.SetInt(
            ResolutionWidthKey,
            width
        );

        PlayerPrefs.SetInt(
            ResolutionHeightKey,
            height
        );

        PlayerPrefs.SetInt(
            FullscreenKey,
            fullscreen ? 1 : 0
        );

        StartResolutionChange(
            width,
            height,
            fullscreen
        );

        UpdateResolutionDropdownState(
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

    private void UpdateResolutionDropdownState(
        bool fullscreen
    )
    {
        if (resolutionDropdown == null)
            return;
        resolutionDropdown.interactable = !fullscreen;

        if (!fullscreen)
            return;

        Resolution nativeResolution =
            Screen.currentResolution;

        int nativeIndex = GetResolutionIndex(
            nativeResolution.width,
            nativeResolution.height
        );

        resolutionDropdown.SetValueWithoutNotify(
            nativeIndex
        );

        resolutionDropdown.RefreshShownValue();
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

#if UNITY_2022_2_OR_NEWER

        RefreshRate currentRefreshRate =
            Screen.currentResolution.refreshRateRatio;

        Screen.SetResolution(
            width,
            height,
            screenMode,
            currentRefreshRate
        );

#else

        int currentRefreshRate =
            Screen.currentResolution.refreshRate;

        Screen.SetResolution(
            width,
            height,
            screenMode,
            currentRefreshRate
        );

#endif

        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            $"Resolution applied: {Screen.width} x {Screen.height} | " +
            $"Requested: {width} x {height} | " +
            $"Mode: {screenMode} | " +
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
            bool isSupported =
                supportedResolutions.Any(
                    resolution =>
                        resolution.width == allowed.width &&
                        resolution.height == allowed.height
                );

            if (isSupported)
            {
                AddResolutionIfMissing(
                    allowed.width,
                    allowed.height
                );
            }
        }
        Resolution nativeResolution =
            Screen.currentResolution;

        AddResolutionIfMissing(
            nativeResolution.width,
            nativeResolution.height
        );

        if (resolutionOptions.Count == 0)
        {
            AddResolutionIfMissing(
                Screen.width,
                Screen.height
            );
        }

        resolutionOptions.Sort(
            (first, second) =>
            {
                long firstPixels =
                    (long)first.width * first.height;

                long secondPixels =
                    (long)second.width * second.height;

                return secondPixels.CompareTo(
                    firstPixels
                );
            }
        );

        List<string> dropdownOptions =
            resolutionOptions
                .Select(
                    option =>
                        $"{option.width} x {option.height}"
                )
                .ToList();

        resolutionDropdown.AddOptions(
            dropdownOptions
        );

        resolutionDropdown.RefreshShownValue();
    }

    private void AddResolutionIfMissing(
        int width,
        int height
    )
    {
        bool alreadyExists =
            resolutionOptions.Any(
                option =>
                    option.width == width &&
                    option.height == height
            );

        if (alreadyExists)
            return;

        resolutionOptions.Add(
            new ResolutionOption(
                width,
                height
            )
        );
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
            if (resolutionOptions[i].width == width &&
                resolutionOptions[i].height == height)
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
            if (resolutionOptions[i].width == Screen.width &&
                resolutionOptions[i].height == Screen.height)
            {
                return i;
            }
        }

        return 0;
    }

    private void ReturnToMainMenu()
    {
        RestoreOriginalSettings();

        if (pauseGameWhenOpen && hasPauseRequest)
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