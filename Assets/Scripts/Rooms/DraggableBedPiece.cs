using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableBedPiece : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private BedPieceType pieceType;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private Vector2 startAnchoredPosition;
    private Transform startParent;
    private bool initialized;

    public BedPieceType PieceType => pieceType;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        startParent = transform.parent;
        startAnchoredPosition = rectTransform.anchoredPosition;

        initialized = true;
    }

    public void ResetPiece()
    {
        Initialize();

        transform.SetParent(startParent, false);
        rectTransform.anchoredPosition = startAnchoredPosition;
    }

    public void HidePiece()
    {
        Initialize();
        ResetPiece();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void RestorePiece()
    {
        Initialize();

        transform.SetParent(startParent, false);
        rectTransform.anchoredPosition = startAnchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Initialize();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Initialize();

        if (canvas == null)
            return;

        rectTransform.anchoredPosition +=
            eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Initialize();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        bool placed =
            BedPuzzleUI.Instance != null &&
            BedPuzzleUI.Instance.TryPlacePiece(
                this,
                eventData.position
            );

        if (!placed)
            ResetPiece();
    }
}