using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomCleaningTutorialBookUI : MonoBehaviour
{
    public static bool IsBlockingGameInput { get; private set; }

    private const string TutorialCompletedKey = "Hotelia_RoomCleaningTutorial_Completed";

    private static bool shownThisSession;

    [Header("Scene")]
    [Tooltip("Exact name of the hotel scene where this tutorial appears.")]
    [SerializeField] private string tutorialSceneName;

    [Header("Tutorial Canvas")]
    [SerializeField] private Canvas tutorialCanvas;
    [SerializeField] private GraphicRaycaster tutorialGraphicRaycaster;
    [SerializeField] private int tutorialSortingOrder = 32760;

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private CanvasGroup tutorialCanvasGroup;

    [Tooltip("Full-screen image that blocks clicks behind the tutorial.")]
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
        IsBlockingGameInput = false;
    }

    private void Awake()
    {
        FindMissingComponents();
        ConfigureCanvas();

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
        if (!string.IsNullOrWhiteSpace(tutorialSceneName) &&
            SceneManager.GetActiveScene().name != tutorialSceneName)
        {
            yield break;
        }

        float timeout = 10f;

        while ((DayManager.Instance == null ||
                DailyResultsManager.Instance == null) &&
               timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        while (!shownThisSession && !WasCompleted())
        {
            if (ShouldShowTutorial())
            {
                OpenTutorial();
                yield break;
            }

            if (DayManager.Instance != null &&
                DayManager.Instance.CurrentDay > 2)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }
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
            tutorialGraphicRaycaster =
                GetComponent<GraphicRaycaster>();

        if (tutorialCanvasGroup == null &&
            tutorialPanel != null)
        {
            tutorialCanvasGroup =
                tutorialPanel.GetComponent<CanvasGroup>();
        }
    }

    private void ConfigureCanvas()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            tutorialCanvas.overrideSorting = true;
            tutorialCanvas.sortingOrder =
                tutorialSortingOrder;
        }

        if (tutorialGraphicRaycaster != null)
            tutorialGraphicRaycaster.enabled = true;

        if (inputBlocker != null)
        {
            inputBlocker.raycastTarget = true;
            inputBlocker.transform.SetAsFirstSibling();
        }
    }

    private bool ShouldShowTutorial()
    {
        if (DayManager.Instance == null)
            return false;

        if (DayManager.Instance.CurrentDay != 2)
            return false;

        if (shownThisSession)
            return false;

        if (WasCompleted())
            return false;

        if (HasCompletedRoomCleaning())
        {
            MarkAsCompleted();
            return false;
        }

        return true;
    }

    private bool HasCompletedRoomCleaning()
    {
        if (DailyResultsManager.Instance == null)
            return false;

        List<MiniGameResultData> results =
            DailyResultsManager.Instance.GetSavedHistory();

        if (results == null)
            return false;

        foreach (MiniGameResultData result in results)
        {
            if (result == null)
                continue;

            bool isRoomCleaning =
                string.Equals(
                    result.minigameName,
                    "Room",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    result.minigameName,
                    "Habitación",
                    StringComparison.OrdinalIgnoreCase
                );

            if (isRoomCleaning)
                return true;
        }

        return false;
    }

    private void OpenTutorial()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning(
                "There are no Room Cleaning tutorial pages assigned."
            );

            return;
        }

        shownThisSession = true;
        IsBlockingGameInput = true;
        currentPageIndex = 0;

        ConfigureCanvas();

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
        PlayerPrefs.SetInt(
            TutorialCompletedKey,
            1
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[Room Cleaning Tutorial] Completed."
        );
    }

    public static void ResetForNewGame()
    {
        PlayerPrefs.DeleteKey(TutorialCompletedKey);
        PlayerPrefs.Save();

        shownThisSession = false;
        IsBlockingGameInput = false;

        Debug.Log(
            "[Room Cleaning Tutorial] Reset for the new game."
        );
    }

    [ContextMenu("Reset Room Cleaning Tutorial")]
    private void ResetTutorialFromInspector()
    {
        ResetForNewGame();
    }
}