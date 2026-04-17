using UnityEngine;

public class CheckInFlowController : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject checkInScreen;
    public GameObject mapPanel;
    public GameObject reservationPanel;
    public GameObject resultPanel;
    public GameObject noRoomsPanel;

    [Header("Room Info Panel")]
    public RoomInfoPanelUI roomInfoPanel;

    [Header("Controllers")]
    public CheckInDialogueController dialogueController;

    [Header("Interactive Objects")]
    public GameObject computerHighlight;
    public ComputerInteractable computerInteractable;

    [Header("Rooms")]
    public RoomData[] allRooms;

    [Header("Player")]
    public PlayerMovement playerMovement;

    public bool IsCheckInActive { get; private set; }

    private CheckInRequest currentRequest;
    private RoomData selectedRoom;

    [Header("Room Buttons")]
    public RoomButtonUI[] roomButtons;

    public void StartCheckIn()
    {
        if (IsCheckInActive)
        {
            Debug.Log("El check-in ya está activo.");
            return;
        }

        if (!HasFreeRooms())
        {
            Debug.Log("No hay habitaciones disponibles.");
            if (noRoomsPanel != null) noRoomsPanel.SetActive(true);
            return;
        }

        IsCheckInActive = true;
        UIManager.Instance?.RegisterPanelOpen();
        playerMovement?.StopPlayer();

        if (checkInScreen != null) checkInScreen.SetActive(true);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (reservationPanel != null) reservationPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);

        currentRequest = CheckInRequestGenerator.GenerateRequest(allRooms);

        if (currentRequest == null)
        {
            Debug.Log("No se pudo generar una reserva válida.");
            IsCheckInActive = false;
            return;
        }

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished = OnDialogueFinished;
            dialogueController.StartCheckIn(currentRequest.GetDialogueLines());
        }
        else
        {
            Debug.LogWarning("DialogueController no asignado.");
        }

        if (computerHighlight != null) computerHighlight.SetActive(false);
        if (computerInteractable != null) computerInteractable.SetEnabled(false);

        selectedRoom = null;
    }

    void OnDialogueFinished()
    {
        if (computerHighlight != null) computerHighlight.SetActive(true);
        if (computerInteractable != null) computerInteractable.SetEnabled(true);
    }

    public void OpenMap()
    {
        if (mapPanel != null)
            mapPanel.SetActive(true);

        if (roomInfoPanel != null)
            roomInfoPanel.gameObject.SetActive(false);

        if (roomButtons != null)
        {
            foreach (RoomButtonUI roomButton in roomButtons)
            {
                if (roomButton != null)
                    roomButton.Refresh();
            }
        }
    }

    public void SelectRoom(RoomData room)
    {
        selectedRoom = room;
        Debug.Log("Seleccionaste habitación: " + room.roomId);
    }

    public void ShowRoomInfo(RoomData room)
    {
        if (roomInfoPanel == null)
        {
            Debug.LogWarning("RoomInfoPanel no asignado.");
            return;
        }

        roomInfoPanel.Show(room, this);
    }

    public void ConfirmRoomSelection()
    {
        if (selectedRoom == null)
        {
            Debug.Log("No has seleccionado habitación.");
            return;
        }

        if (!IsRoomValid(selectedRoom, currentRequest))
        {
            Debug.Log("Habitación incorrecta para esta reserva.");
            return;
        }

        selectedRoom.state = RoomState.Ocupada;
        selectedRoom.needsCleaning = false;
        selectedRoom.reservedUntilDay = DayManager.Instance.CurrentDay + currentRequest.stayDays - 1;

        Debug.Log("Habitación asignada correctamente: " + selectedRoom.roomId +
                  " | Días: " + currentRequest.stayDays +
                  " | Ocupada hasta el día: " + selectedRoom.reservedUntilDay);

        CloseCheckIn();
    }

    public void CloseCheckIn()
    {
        IsCheckInActive = false;
        UIManager.Instance?.RegisterPanelClose();

        if (checkInScreen != null) checkInScreen.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);

        selectedRoom = null;
    }

    bool HasFreeRooms()
    {
        foreach (RoomData room in allRooms)
        {
            if (room != null && room.state == RoomState.Libre)
                return true;
        }

        return false;
    }

    bool IsRoomValid(RoomData room, CheckInRequest request)
    {
        if (room.state != RoomState.Libre) return false;
        if (request.needsAccessibleRoom && !room.isAccessible) return false;
        if (request.bedType != room.bedType) return false;
        if (room.bedCount < request.guestCount) return false;

        return true;
    }
}