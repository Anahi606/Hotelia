using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [Header("Menu Controller")]
    [SerializeField] private PlayfabManager playfabManager;

    [Header("Audio")]
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Video")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

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

    private readonly List<ResolutionOption> resolutionOptions = new List<ResolutionOption>();

    private const string MusicVolumeKey = "MusicVolume";
    private const string ResolutionWidthKey = "ResolutionWidth";
    private const string ResolutionHeightKey = "ResolutionHeight";
    private const string FullscreenKey = "Fullscreen";

    private float originalMusicVolume;
    private int originalResolutionWidth;
    private int originalResolutionHeight;
    private bool originalFullscreen;

    private bool isLoadingUI;

    private void Awake()
    {
        BuildResolutionDropdown();

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(PreviewMusicVolume);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);

        if (backButton != null)
            backButton.onClick.AddListener(BackButtonClicked);
    }

    private void OnEnable()
    {
        CaptureOriginalSettings();
        LoadSavedSettingsIntoUI();
    }

    private void OnDestroy()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(PreviewMusicVolume);

        if (applyButton != null)
            applyButton.onClick.RemoveListener(ApplySettings);

        if (backButton != null)
            backButton.onClick.RemoveListener(BackButtonClicked);
    }

    private void CaptureOriginalSettings()
    {
        originalMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
        originalResolutionWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        originalResolutionHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        originalFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    }

    private void BuildResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogError("SettingsPanelUI: Resolution Dropdown is missing.");
            return;
        }

        resolutionDropdown.ClearOptions();
        resolutionOptions.Clear();

        HashSet<string> addedResolutions = new HashSet<string>();

        foreach (Resolution resolution in Screen.resolutions)
        {
            string key = resolution.width + "x" + resolution.height;

            if (addedResolutions.Add(key))
            {
                resolutionOptions.Add(new ResolutionOption(resolution.width, resolution.height));
            }
        }

        if (resolutionOptions.Count == 0)
        {
            resolutionOptions.Add(new ResolutionOption(Screen.width, Screen.height));
        }

        List<ResolutionOption> sortedOptions = resolutionOptions
            .OrderByDescending(r => r.width * r.height)
            .ThenByDescending(r => r.width)
            .ToList();

        resolutionOptions.Clear();
        resolutionOptions.AddRange(sortedOptions);

        List<string> dropdownOptions = new List<string>();

        foreach (ResolutionOption option in resolutionOptions)
        {
            dropdownOptions.Add(option.width + " x " + option.height);
        }

        resolutionDropdown.AddOptions(dropdownOptions);
        resolutionDropdown.RefreshShownValue();
    }

    private void LoadSavedSettingsIntoUI()
    {
        isLoadingUI = true;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(originalMusicVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(originalFullscreen);
        }

        int resolutionIndex = GetResolutionIndex(
            originalResolutionWidth,
            originalResolutionHeight
        );

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        isLoadingUI = false;
    }

    private int GetResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            if (resolutionOptions[i].width == width &&
                resolutionOptions[i].height == height)
            {
                return i;
            }
        }

        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            if (resolutionOptions[i].width == Screen.width &&
                resolutionOptions[i].height == Screen.height)
            {
                return i;
            }
        }

        return 0;
    }

    private void PreviewMusicVolume(float value)
    {
        if (isLoadingUI)
            return;

        value = Mathf.Clamp01(value);

        if (AudioSettingsManager.Instance != null)
        {
            AudioSettingsManager.Instance.PreviewMusicVolume(value);
        }
    }

    public void ApplySettings()
    {
        ApplyAudioSettings();
        ApplyVideoSettings();

        PlayerPrefs.Save();

        CaptureOriginalSettings();

        Debug.Log("Settings applied.");
    }

    private void ApplyAudioSettings()
    {
        if (musicVolumeSlider == null)
            return;

        float volume = Mathf.Clamp01(musicVolumeSlider.value);

        PlayerPrefs.SetFloat(MusicVolumeKey, volume);

        if (AudioSettingsManager.Instance != null)
        {
            AudioSettingsManager.Instance.SetMusicVolume(volume);
        }
    }

    private void ApplyVideoSettings()
    {
        if (resolutionDropdown == null || fullscreenToggle == null)
        {
            Debug.LogError("SettingsPanelUI: Missing resolution dropdown or fullscreen toggle.");
            return;
        }

        int selectedIndex = resolutionDropdown.value;

        if (selectedIndex < 0 || selectedIndex >= resolutionOptions.Count)
        {
            Debug.LogError("SettingsPanelUI: Invalid resolution selected.");
            return;
        }

        ResolutionOption selectedResolution = resolutionOptions[selectedIndex];
        bool fullscreen = fullscreenToggle.isOn;

        FullScreenMode screenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(
            selectedResolution.width,
            selectedResolution.height,
            screenMode
        );

        PlayerPrefs.SetInt(ResolutionWidthKey, selectedResolution.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, selectedResolution.height);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
    }

    private void BackButtonClicked()
    {
        RestoreOriginalSettings();

        if (playfabManager == null)
        {
            playfabManager = FindFirstObjectByType<PlayfabManager>();
        }

        if (playfabManager == null)
        {
            Debug.LogError("SettingsPanelUI: PlayfabManager is missing.");
            return;
        }

        playfabManager.BackFromSettingsPanel();
    }

    private void RestoreOriginalSettings()
    {
        if (AudioSettingsManager.Instance != null)
        {
            AudioSettingsManager.Instance.PreviewMusicVolume(originalMusicVolume);
        }

        FullScreenMode screenMode = originalFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(
            originalResolutionWidth,
            originalResolutionHeight,
            screenMode
        );

        LoadSavedSettingsIntoUI();

        Debug.Log("Settings restored. Changes were not saved.");
    }
}