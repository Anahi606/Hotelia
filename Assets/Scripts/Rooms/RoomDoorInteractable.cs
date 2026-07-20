using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RoomDoorInteractable : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private RoomData roomData;

    [Header("NPC Spawn")]
    [SerializeField] private Transform npcExitSpawnPoint;

    [Header("Interaction Prompt")]
    [Tooltip("Objeto 2D con SpriteRenderer que muestra la tecla E.")]
    [SerializeField] private GameObject interactionPrompt;

    private bool playerInside;
    private bool openingRoom;

    private void Start()
    {
        HideInteractionPrompt();
    }

    private void Update()
    {
        if (!playerInside || openingRoom)
            return;

        if (roomData == null)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
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
        if (roomData == null)
        {
            Debug.LogWarning(
                "RoomData no está asignado en RoomDoorInteractable."
            );

            return;
        }

        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData.");
            return;
        }

        RoomRuntimeData runtimeRoom =
            HotelGameData.Instance.GetRoomById(roomData.roomId);

        if (runtimeRoom == null)
        {
            Debug.LogWarning(
                "No se encontró la habitación " +
                roomData.roomId +
                " en HotelGameData."
            );

            return;
        }

        GuestNPCSceneSaver.SaveAllVisibleGuests();

        if (runtimeRoom.bedCount <= 0)
        {
            runtimeRoom.isAccessible = roomData.isAccessible;
            runtimeRoom.bedType = roomData.bedType;
            runtimeRoom.bedCount = roomData.bedCount;

            Debug.Log(
                "Se corrigieron datos estáticos de habitación " +
                roomData.roomId +
                " desde RoomData. Camas: " +
                roomData.bedCount +
                " / Tipo cama: " +
                roomData.bedType
            );
        }

        int currentDay =
            DayManager.Instance != null
                ? DayManager.Instance.CurrentDay
                : 1;

        PlayerSpawnMemory.SetNextSpawn("RoomInside");

        RoomCleaningSession.selectedRoomId =
            runtimeRoom.roomId;

        RoomCleaningSession.selectedBedCount =
            runtimeRoom.bedCount;

        RoomCleaningSession.selectedBedType =
            runtimeRoom.bedType;

        RoomCleaningSession.selectedNeedsCleaning =
            runtimeRoom.needsCleaning ||
            runtimeRoom.state == RoomState.Dirty;

        RoomCleaningSession.selectedReservationStillActive =
            runtimeRoom.hasGuestData &&
            runtimeRoom.reservedUntilDay >= currentDay;

        Debug.Log(
            "Entrando a habitación " +
            runtimeRoom.roomId +
            " / Día actual: " +
            currentDay +
            " / Reservado hasta: " +
            runtimeRoom.reservedUntilDay +
            " / Estado runtime: " +
            runtimeRoom.state +
            " / NeedsCleaning runtime: " +
            runtimeRoom.needsCleaning +
            " / Session Sucia: " +
            RoomCleaningSession.selectedNeedsCleaning +
            " / Reserva activa: " +
            RoomCleaningSession.selectedReservationStillActive
        );

        openingRoom = true;
        HideInteractionPrompt();

        SceneManager.LoadScene("03 - Bedroom");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (!openingRoom)
            ShowInteractionPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        HideInteractionPrompt();
    }

    private void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Interaction Prompt no está asignado en RoomDoorInteractable."
            );
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OnDisable()
    {
        HideInteractionPrompt();
    }
}