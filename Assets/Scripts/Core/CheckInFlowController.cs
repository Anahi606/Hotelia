using UnityEngine;

public class CheckInFlowController : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject checkInScreen;
    public GameObject mapPanel;
    //public GameObject reservationPanel;
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

    [Header("STP")]
    public GuestSegment selectedSegment;
    public OfferType selectedOffer;

    [Header("Result UI")]
    public CheckInResultPanelUI resultPanelUI;

    [Header("Guest NPCs")]
    public GameObject[] guestNPCPrefabs;

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
        playerMovement?.SetMovementEnabled(false);

        if (checkInScreen != null) checkInScreen.SetActive(true);
        if (mapPanel != null) mapPanel.SetActive(false);
        //if (reservationPanel != null) reservationPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);

        currentRequest = CheckInRequestGenerator.GenerateRequest(allRooms);

        if (currentRequest == null)
        {
            Debug.Log("No se pudo generar una reserva válida.");
            IsCheckInActive = false;
            playerMovement?.SetMovementEnabled(true);

            if (checkInScreen != null) checkInScreen.SetActive(false);
            if (mapPanel != null) mapPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);

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

        selectedRoom.AssignGuest(currentRequest, DayManager.Instance.CurrentDay);
        AssignNPCToRoom(selectedRoom);

        Debug.Log("Habitación asignada correctamente: " + selectedRoom.roomId);

        selectedSegment = currentRequest.correctSegment;
        selectedOffer = currentRequest.bestOffer;

        bool segmentCorrect = selectedSegment == currentRequest.correctSegment;
        bool offerCorrect = selectedOffer == currentRequest.bestOffer;

        int satisfaction = 50;
        int revenue = 100;

        if (segmentCorrect) satisfaction += 20;
        else satisfaction -= 10;

        if (offerCorrect)
        {
            satisfaction += 20;
            revenue += 50;
        }
        else
        {
            satisfaction -= 10;
        }

        KPIManager.Instance?.RegisterCheckInResult(segmentCorrect, offerCorrect, satisfaction, revenue);

        if (mapPanel != null) mapPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);

        if (resultPanel != null && resultPanelUI != null)
        {
            resultPanel.SetActive(true);
            resultPanelUI.Show(segmentCorrect, offerCorrect, satisfaction, revenue);

            IsCheckInActive = false;
            playerMovement?.SetMovementEnabled(true);

            if (computerHighlight != null) computerHighlight.SetActive(false);
            if (computerInteractable != null) computerInteractable.SetEnabled(false);

            selectedRoom = null;
        }
        else
        {
            CloseCheckIn();
        }
    }

    private void AssignNPCToRoom(RoomData room)
    {
        if (guestNPCPrefabs == null || guestNPCPrefabs.Length == 0)
        {
            Debug.LogWarning("No hay prefabs de NPC asignados en CheckInFlowController.");
            return;
        }

        GameObject selectedGuestPrefab = guestNPCPrefabs[Random.Range(0, guestNPCPrefabs.Length)];

        room.hasGuest = true;
        room.guestPrefab = selectedGuestPrefab;
        room.guestCurrentArea = GuestArea.Room;
        room.hotelDoorSpawnId = "Door_" + room.roomId;

        Debug.Log("NPC asignado a la habitación " + room.roomId);
    }
    public void CloseCheckIn()
    {
        Debug.Log("CloseCheckIn llamado");

        IsCheckInActive = false;

        if (checkInScreen != null) checkInScreen.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);
        if (noRoomsPanel != null) noRoomsPanel.SetActive(false);

        if (computerHighlight != null) computerHighlight.SetActive(false);
        if (computerInteractable != null) computerInteractable.SetEnabled(false);

        selectedRoom = null;

        playerMovement?.SetMovementEnabled(true);

        Debug.Log("Check-in cerrado por completo");
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

    public void OpenReservationPanel()
    {
        if (checkInScreen != null) checkInScreen.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);
        //if (reservationPanel != null) reservationPanel.SetActive(true);
    }

    public void SelectSegment(int segmentIndex)
    {
        selectedSegment = (GuestSegment)segmentIndex;
        Debug.Log("Segmento seleccionado: " + selectedSegment);
    }

    public void SelectOffer(int offerIndex)
    {
        selectedOffer = (OfferType)offerIndex;
        Debug.Log("Oferta seleccionada: " + selectedOffer);
    }
}