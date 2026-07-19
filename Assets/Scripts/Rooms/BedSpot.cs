using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
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

    private void Update()
    {
        if (completed)
            return;

        if (!playerInside)
            return;

        if (BedPuzzleUI.Instance == null)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (TrashItem.IsPlayerNearAnyTrash ||
            TrashItem.ConsumedInteractionThisFrame)
        {
            return;
        }

        BedPuzzleUI.Instance.OpenForBed(this);
    }

    private void LateUpdate()
    {
        if (!completed)
            return;

        EnsureCompletedVisuals();
    }

    public void PlacePiece(BedPieceType pieceType)
    {
        switch (pieceType)
        {
            case BedPieceType.Sheets:
                SetVisualActive(sheetsPlaced, true);
                break;

            case BedPieceType.Pillows:
                SetVisualActive(pillowsPlaced, true);
                break;

            case BedPieceType.Cover:
                completed = true;
                EnsureCompletedVisuals();
                break;
        }
    }

    public void SetBedCompletedVisual(bool isCompleted)
    {
        if (completed && !isCompleted)
        {
            EnsureCompletedVisuals();
            return;
        }

        completed = isCompleted;

        SetVisualActive(sheetsPlaced, isCompleted);
        SetVisualActive(pillowsPlaced, isCompleted);
        SetVisualActive(coverPlaced, isCompleted);

        if (dropTarget != null)
            dropTarget.enabled = !isCompleted;
    }

    public void EnsureCompletedVisuals()
    {
        if (!completed)
            return;

        SetVisualActive(sheetsPlaced, true);
        SetVisualActive(pillowsPlaced, true);
        SetVisualActive(coverPlaced, true);

        if (dropTarget != null)
            dropTarget.enabled = false;
    }

    private void SetVisualActive(
        GameObject visual,
        bool active
    )
    {
        if (visual == null)
            return;

        if (visual.activeSelf != active)
            visual.SetActive(active);

        if (!active)
            return;

        SpriteRenderer spriteRenderer =
            visual.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.forceRenderingOff = false;
        }
    }

    public bool IsScreenPointOverDropTarget(
        Vector2 screenPoint,
        Camera cam
    )
    {
        if (dropTarget == null || cam == null)
            return false;

        if (!dropTarget.enabled)
            return false;

        float distanceToScene = Mathf.Abs(
            cam.transform.position.z -
            dropTarget.transform.position.z
        );

        Vector3 screenPointWithDepth = new Vector3(
            screenPoint.x,
            screenPoint.y,
            distanceToScene
        );

        Vector3 worldPoint =
            cam.ScreenToWorldPoint(screenPointWithDepth);

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