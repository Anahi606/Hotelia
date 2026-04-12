using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RoomDoorInteractable : MonoBehaviour
{
    [SerializeField] private RoomData roomData;

    private bool playerInside;

    private void Update()
    {
        if (!playerInside || roomData == null) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenRoomCleaningScene();
        }
    }

    private void OpenRoomCleaningScene()
    {
        RoomCleaningSession.selectedRoomId = roomData.roomId;
        RoomCleaningSession.selectedBedCount = roomData.bedCount;
        RoomCleaningSession.selectedBedType = roomData.bedType;
        RoomCleaningSession.selectedNeedsCleaning = roomData.needsCleaning;
        RoomCleaningSession.selectedReservationStillActive =
            roomData.IsReservationActive(DayManager.Instance.CurrentDay);

        SceneManager.LoadScene("03 - Bedroom");
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