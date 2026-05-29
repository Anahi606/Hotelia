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

        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData.");
            return;
        }

        RoomRuntimeData runtimeRoom = HotelGameData.Instance.GetRoomById(roomData.roomId);

        if (runtimeRoom == null)
        {
            Debug.LogWarning("No se encontró la habitación " + roomData.roomId + " en HotelGameData.");
            return;
        }

        if (runtimeRoom.bedCount <= 0)
        {
            runtimeRoom.isAccessible = roomData.isAccessible;
            runtimeRoom.bedType = roomData.bedType;
            runtimeRoom.bedCount = roomData.bedCount;

            Debug.Log("Se corrigieron datos estáticos de habitación " + roomData.roomId +
                      " desde RoomData. Camas: " + roomData.bedCount +
                      " / Tipo cama: " + roomData.bedType);
        }

        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        PlayerSpawnMemory.SetNextSpawn("RoomInside");

        RoomCleaningSession.selectedRoomId = runtimeRoom.roomId;
        RoomCleaningSession.selectedBedCount = runtimeRoom.bedCount;
        RoomCleaningSession.selectedBedType = runtimeRoom.bedType;

        RoomCleaningSession.selectedNeedsCleaning =
            runtimeRoom.needsCleaning || runtimeRoom.state == RoomState.Sucia;

        RoomCleaningSession.selectedReservationStillActive =
            runtimeRoom.hasGuestData && runtimeRoom.reservedUntilDay >= currentDay;

        Debug.Log("Entrando a habitación " + runtimeRoom.roomId +
                  " / Día actual: " + currentDay +
                  " / Reservado hasta: " + runtimeRoom.reservedUntilDay +
                  " / Estado runtime: " + runtimeRoom.state +
                  " / NeedsCleaning runtime: " + runtimeRoom.needsCleaning +
                  " / Session Sucia: " + RoomCleaningSession.selectedNeedsCleaning +
                  " / Reserva activa: " + RoomCleaningSession.selectedReservationStillActive);

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