using TMPro;
using UnityEngine;

public class BedPuzzleUI : MonoBehaviour
{
    public static BedPuzzleUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Pieces")]
    [SerializeField] private DraggableBedPiece sheetsPiece;
    [SerializeField] private DraggableBedPiece pillowsPiece;
    [SerializeField] private DraggableBedPiece coverPiece;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Cameras")]
    [SerializeField] private Camera worldCamera;

    private BedSpot currentBed;
    private BedPieceType expectedPiece;

    public Camera WorldCamera => worldCamera;
    public BedSpot CurrentBed => currentBed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panel != null)
            panel.SetActive(false);
    }

    public void OpenForBed(BedSpot bed)
    {
        if (bed == null) return;
        if (bed.IsCompleted) return;

        currentBed = bed;
        expectedPiece = BedPieceType.Sheets;

        if (panel != null)
            panel.SetActive(true);

        ResetPieces();
        UpdateText();
    }

    public void ClosePanel()
    {
        ResetPieces();

        if (panel != null)
            panel.SetActive(false);

        currentBed = null;
    }

    public bool TryPlacePiece(DraggableBedPiece piece, Vector2 screenPosition)
    {
        if (currentBed == null) return false;
        if (piece.PieceType != expectedPiece) return false;
        if (!currentBed.IsScreenPointOverDropTarget(screenPosition, worldCamera)) return false;

        currentBed.PlacePiece(piece.PieceType);
        piece.HidePiece();

        if (expectedPiece == BedPieceType.Sheets)
        {
            expectedPiece = BedPieceType.Pillows;
            UpdateText();
            return true;
        }

        if (expectedPiece == BedPieceType.Pillows)
        {
            expectedPiece = BedPieceType.Cover;
            UpdateText();
            return true;
        }

        if (expectedPiece == BedPieceType.Cover)
        {
            ClosePanel();
            return true;
        }

        return false;
    }

    private void ResetPieces()
    {
        sheetsPiece.RestorePiece();
        pillowsPiece.RestorePiece();
        coverPiece.RestorePiece();
    }

    private void UpdateText()
    {
        if (instructionText == null) return;

        switch (expectedPiece)
        {
            case BedPieceType.Sheets:
                instructionText.text = "Arrastra la sábana a la cama";
                break;
            case BedPieceType.Pillows:
                instructionText.text = "Arrastra las almohadas a la cama";
                break;
            case BedPieceType.Cover:
                instructionText.text = "Arrastra el cobertor a la cama";
                break;
        }
    }
}