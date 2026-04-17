using UnityEngine;
using UnityEngine.InputSystem;

public class BedSpot : MonoBehaviour
{
    [Header("Placed Visuals")]
    [SerializeField] private GameObject sheetsPlaced;
    [SerializeField] private GameObject pillowsPlaced;
    [SerializeField] private GameObject coverPlaced;

    [Header("Drop")]
    [SerializeField] private Collider2D dropTarget;

    private bool playerInside;
    private bool completed;

    public bool IsCompleted => completed;

    private void Start()
    {
        if (sheetsPlaced != null) sheetsPlaced.SetActive(false);
        if (pillowsPlaced != null) pillowsPlaced.SetActive(false);
        if (coverPlaced != null) coverPlaced.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside || completed) return;
        if (BedPuzzleUI.Instance == null) return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            BedPuzzleUI.Instance.OpenForBed(this);
        }
    }

    public void PlacePiece(BedPieceType pieceType)
    {
        switch (pieceType)
        {
            case BedPieceType.Sheets:
                if (sheetsPlaced != null) sheetsPlaced.SetActive(true);
                break;

            case BedPieceType.Pillows:
                if (pillowsPlaced != null) pillowsPlaced.SetActive(true);
                break;

            case BedPieceType.Cover:
                if (coverPlaced != null) coverPlaced.SetActive(true);
                completed = true;

                if (dropTarget != null)
                    dropTarget.enabled = false;
                break;
        }
    }

    public bool IsScreenPointOverDropTarget(Vector2 screenPoint, Camera cam)
    {
        if (dropTarget == null || cam == null) return false;
        if (!dropTarget.enabled) return false;

        float distanceToScene = Mathf.Abs(cam.transform.position.z - dropTarget.transform.position.z);

        Vector3 screenPointWithDepth = new Vector3(
            screenPoint.x,
            screenPoint.y,
            distanceToScene
        );

        Vector3 worldPoint = cam.ScreenToWorldPoint(screenPointWithDepth);
        return dropTarget.OverlapPoint(worldPoint);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }
}