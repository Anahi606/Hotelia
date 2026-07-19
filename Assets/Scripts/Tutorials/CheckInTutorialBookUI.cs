using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CheckInTutorialBookUI : MonoBehaviour
{
    public static bool IsBlockingGameInput { get; private set; }
    private const string TutorialCompletedKey = "Hotelia_CheckInTutorial_Completed";
    private const string OldTutorialShownKey = "Hotelia_CheckInTutorial_Shown";
    private static bool shownThisSession;

    [Header("Scene")]
    [Tooltip("Exact name of the hotel scene where the tutorial can appear.")]
    [SerializeField] private string tutorialSceneName;

    [Header("Tutorial Canvas")]
    [SerializeField] private Canvas tutorialCanvas;
    [SerializeField] private GraphicRaycaster tutorialGraphicRaycaster;
    [SerializeField] private int tutorialSortingOrder = 32760;

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private CanvasGroup tutorialCanvasGroup;

    [Tooltip("Full-screen background image that blocks clicks behind the tutorial.")]
    [SerializeField] private Image inputBlocker;

    [Header("Page Content")]
    [SerializeField] private Image pageImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Navigation Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    [Header("Optional Page Indicator")]
    [SerializeField] private TMP_Text pageIndicatorText;

    [Header("Tutorial Pages")]
    [SerializeField] private TutorialPageData[] pages;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;

    private int currentPageIndex;

    public bool IsOpen
    {
        get
        {
            return tutorialPanel != null &&
                   tutorialPanel.activeSelf;
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        shownThisSession = false;
    }

    private void Awake()
    {
        FindMissingComponents();
        ConfigureTutorialCanvas();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 1f;
            tutorialCanvasGroup.interactable = false;
            tutorialCanvasGroup.blocksRaycasts = false;
        }

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
            closeButton.interactable = false;
        }

        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousPage);

        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextPage);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseTutorial);
    }

    private IEnumerator Start()
    {
        float timeout = 10f;

        while (DayManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.25f);

        TryOpenAutomatically();
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(ShowPreviousPage);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextPage);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseTutorial);

        IsBlockingGameInput = false;
    }

    private void FindMissingComponents()
    {
        if (tutorialCanvas == null)
            tutorialCanvas = GetComponent<Canvas>();

        if (tutorialGraphicRaycaster == null)
            tutorialGraphicRaycaster = GetComponent<GraphicRaycaster>();

        if (tutorialCanvasGroup == null && tutorialPanel != null)
        {
            tutorialCanvasGroup =
                tutorialPanel.GetComponent<CanvasGroup>();
        }
    }

    private void ConfigureTutorialCanvas()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.overrideSorting = true;
            tutorialCanvas.sortingOrder = tutorialSortingOrder;
        }

        if (tutorialGraphicRaycaster != null)
            tutorialGraphicRaycaster.enabled = true;

        if (inputBlocker != null)
        {
            inputBlocker.raycastTarget = true;
            inputBlocker.transform.SetAsFirstSibling();
        }
    }

    private void TryOpenAutomatically()
    {
        if (!ShouldShowTutorial())
            return;

        OpenTutorial();
    }

    private bool ShouldShowTutorial()
    {
        if (!string.IsNullOrWhiteSpace(tutorialSceneName) &&
            SceneManager.GetActiveScene().name != tutorialSceneName)
        {
            return false;
        }

        if (DayManager.Instance == null)
        {
            Debug.LogWarning(
                "[Check-In Tutorial] DayManager is not available."
            );

            return false;
        }

        if (DayManager.Instance.CurrentDay != 1)
            return false;
        if (shownThisSession)
            return false;
        if (WasCompleted())
            return false;

        if (HasCompletedCheckIn())
        {
            MarkAsCompleted();
            return false;
        }

        return true;
    }

    private bool HasCompletedCheckIn()
    {
        if (DailyResultsManager.Instance == null)
            return false;

        List<MiniGameResultData> savedResults =
            DailyResultsManager.Instance.GetSavedHistory();

        if (savedResults == null)
            return false;

        foreach (MiniGameResultData result in savedResults)
        {
            if (result == null)
                continue;

            if (string.Equals(
                result.minigameName,
                "Check-in",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void OpenTutorial()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning(
                "There are no Check-In tutorial pages assigned."
            );

            return;
        }
        shownThisSession = true;
        IsBlockingGameInput = true;
        currentPageIndex = 0;

        ConfigureTutorialCanvas();

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPanel.transform.SetAsLastSibling();
        }

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 1f;
            tutorialCanvasGroup.interactable = true;
            tutorialCanvasGroup.blocksRaycasts = true;
        }

        if (inputBlocker != null)
        {
            inputBlocker.gameObject.SetActive(true);
            inputBlocker.raycastTarget = true;
            inputBlocker.transform.SetAsFirstSibling();
        }

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(false);

        RefreshPage();
    }

    private void ShowPreviousPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (currentPageIndex <= 0)
            return;

        currentPageIndex--;
        RefreshPage();
    }

    private void ShowNextPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (currentPageIndex >= pages.Length - 1)
            return;

        currentPageIndex++;
        RefreshPage();
    }

    private void RefreshPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        TutorialPageData currentPage =
            pages[currentPageIndex];

        if (pageImage != null)
        {
            pageImage.sprite = currentPage.image;
            pageImage.enabled = currentPage.image != null;
        }

        if (titleText != null)
            titleText.text = currentPage.title;

        if (descriptionText != null)
            descriptionText.text = currentPage.description;

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text =
                (currentPageIndex + 1) +
                " / " +
                pages.Length;
        }

        bool isFirstPage =
            currentPageIndex == 0;

        bool isLastPage =
            currentPageIndex == pages.Length - 1;

        if (previousButton != null)
            previousButton.interactable = !isFirstPage;

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!isLastPage);
            nextButton.interactable = !isLastPage;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(isLastPage);
            closeButton.interactable = isLastPage;
        }
    }

    public void CloseTutorial()
    {
        if (!IsOpen)
            return;

        IsBlockingGameInput = false;

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.interactable = false;
            tutorialCanvasGroup.blocksRaycasts = false;
        }

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);
    }

    public static bool WasCompleted()
    {
        return PlayerPrefs.GetInt(
            TutorialCompletedKey,
            0
        ) == 1;
    }

    public static void MarkAsCompleted()
    {
        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        Debug.Log(
            "[Check-In Tutorial] Check-In completed. " +
            "The tutorial will not appear again."
        );
    }

    public static void ResetForNewGame()
    {
        PlayerPrefs.DeleteKey(TutorialCompletedKey);
        PlayerPrefs.DeleteKey(OldTutorialShownKey);
        PlayerPrefs.Save();

        IsBlockingGameInput = false;
        shownThisSession = false;

        Debug.Log(
            "[Check-In Tutorial] Reset for the new game."
        );
    }

    [ContextMenu("Reset Check-In Tutorial")]
    private void ResetTutorialFromInspector()
    {
        ResetForNewGame();
    }
}