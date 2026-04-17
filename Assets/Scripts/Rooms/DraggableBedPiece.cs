using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableBedPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private BedPieceType pieceType;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private Vector2 startAnchoredPosition;
    private Transform startParent;
    private Image image;

    public BedPieceType PieceType => pieceType;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        startAnchoredPosition = rectTransform.anchoredPosition;
        startParent = transform.parent;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ResetPiece()
    {
        transform.SetParent(startParent, false);
        rectTransform.anchoredPosition = startAnchoredPosition;
    }

    public void HidePiece()
    {
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
        transform.SetParent(startParent, false);
        rectTransform.anchoredPosition = startAnchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        bool placed = BedPuzzleUI.Instance.TryPlacePiece(this, eventData.position);

        if (!placed)
            ResetPiece();
    }
}