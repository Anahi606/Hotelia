using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RoomDoorInteractable : MonoBehaviour
{
    [SerializeField] private RoomData roomData;

    [Header("NPC Spawn")]
    [SerializeField] private Transform npcExitSpawnPoint;

    private bool playerInside;

    private void Update()
    {
        if (!playerInside || roomData == null)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenRoomCleaningScene();
        }
    }

    public string GetRoomId()
    {
        return roomData != null ? roomData.roomId : "";
    }

    public Vector3 GetNPCExitSpawnPosition()
    {
        if (npcExitSpawnPoint != null)
            return npcExitSpawnPoint.position;

        return transform.position;
    }

    private void OpenRoomCleaningScene()
    {
        GuestNPCSceneSaver.SaveAllVisibleGuests();
        PlayerSpawnMemory.SetNextSpawn("RoomInside");
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