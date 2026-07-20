using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestaurantTutorialBookUI : MonoBehaviour
{
    public static bool IsBlockingGameInput { get; private set; }

    private const string TutorialSeenKey =
        "Hotelia_RestaurantTutorial_Seen";

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

    public bool OpenIfNeeded()
    {
        if (WasSeen())
            return false;

        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning(
                "There are no Restaurant tutorial pages assigned."
            );

            return false;
        }

        OpenTutorial();
        return true;
    }

    private void OpenTutorial()
    {
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

        MarkAsSeen();
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

    public static bool WasSeen()
    {
        return PlayerPrefs.GetInt(
            TutorialSeenKey,
            0
        ) == 1;
    }

    private static void MarkAsSeen()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();

        Debug.Log(
            "[Restaurant Tutorial] Tutorial completed."
        );
    }

    public static void ResetForNewGame()
    {
        PlayerPrefs.DeleteKey(TutorialSeenKey);
        PlayerPrefs.Save();

        IsBlockingGameInput = false;

        Debug.Log(
            "[Restaurant Tutorial] Reset for the new game."
        );
    }

    [ContextMenu("Reset Restaurant Tutorial")]
    private void ResetTutorialFromInspector()
    {
        ResetForNewGame();
    }
}