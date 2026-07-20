using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class TrashItem : MonoBehaviour
{
    private static int nearbyTrashCount;
    private static int trashInteractionFrame = -1;

    public static bool IsPlayerNearAnyTrash =>
        nearbyTrashCount > 0;

    public static bool ConsumedInteractionThisFrame =>
        trashInteractionFrame == Time.frameCount;

    private TrashSpawner trashSpawner;

    private bool playerNear;
    private bool alreadyCleaned;

    public void Setup(TrashSpawner spawner)
    {
        trashSpawner = spawner;
    }

    private void OnMouseDown()
    {
        if (alreadyCleaned)
            return;

        trashInteractionFrame = Time.frameCount;
        CleanTrash();
    }

    private void Update()
    {
        if (!playerNear || alreadyCleaned)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        trashInteractionFrame = Time.frameCount;

        CleanTrash();
    }

    private void CleanTrash()
    {
        if (alreadyCleaned)
            return;

        alreadyCleaned = true;

        if (trashSpawner != null)
        {
            trashSpawner.RemoveTrash(this);
        }
        else
        {
            Destroy(gameObject);
        }

        if (RoomCleaningKPIManager.Instance != null)
        {
            RoomCleaningKPIManager.Instance.RegisterTrashCleaned();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playerNear)
            return;

        playerNear = true;
        nearbyTrashCount++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        RemoveNearbyState();
    }

    private void OnDisable()
    {
        RemoveNearbyState();
    }

    private void RemoveNearbyState()
    {
        if (!playerNear)
            return;

        playerNear = false;

        nearbyTrashCount = Mathf.Max(
            0,
            nearbyTrashCount - 1
        );
    }
}