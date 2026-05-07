using UnityEngine;
using UnityEngine.UI;

public class CheckInFlowController : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject checkInScreen;
    public GameObject stpOfferPanel;
    public GameObject mapPanel;
    public GameObject resultPanel;
    public GameObject noRoomsPanel;

    [Header("Room Info Panel")]
    public RoomInfoPanelUI roomInfoPanel;

    [Header("Controllers")]
    public CheckInDialogueController dialogueController;

    [Header("Interactive Objects")]
    public GameObject computerHighlight;
    public ComputerInteractable computerInteractable;

    [Header("Computer Button")]
    public Button computerButton;

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

    private GuestSegment pendingSegment;
    private OfferType pendingOffer;

    private bool pendingSegmentSelected;
    private bool pendingOfferSelected;

    private bool segmentSelected;
    private bool offerSelected;

    private bool dialogueFinished;

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

            if (noRoomsPanel != null)
                noRoomsPanel.SetActive(true);

            return;
        }

        IsCheckInActive = true;
        playerMovement?.SetMovementEnabled(false);

        if (checkInScreen != null) checkInScreen.SetActive(true);
        if (stpOfferPanel != null) stpOfferPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);
        if (noRoomsPanel != null) noRoomsPanel.SetActive(false);

        selectedRoom = null;

        pendingSegmentSelected = false;
        pendingOfferSelected = false;

        segmentSelected = false;
        offerSelected = false;

        dialogueFinished = false;

        if (computerButton != null)
            computerButton.interactable = false;

        currentRequest = CheckInRequestGenerator.GenerateRequest(allRooms);

        if (currentRequest == null)
        {
            Debug.Log("No se pudo generar una reserva válida.");

            IsCheckInActive = false;
            playerMovement?.SetMovementEnabled(true);

            if (checkInScreen != null) checkInScreen.SetActive(false);
            if (stpOfferPanel != null) stpOfferPanel.SetActive(false);
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
    }

    private void OnDialogueFinished()
    {
        dialogueFinished = true;

        if (computerHighlight != null)
            computerHighlight.SetActive(true);

        if (computerInteractable != null)
            computerInteractable.SetEnabled(true);

        if (computerButton != null)
            computerButton.interactable = true;
    }

    public void OpenMap()
    {
        OpenSTPOfferPanel();
    }

    public void OpenSTPOfferPanel()
    {
        if (!dialogueFinished)
        {
            Debug.Log("Primero debes terminar el diálogo con el huésped.");
            return;
        }

        if (currentRequest == null)
        {
            Debug.LogWarning("No hay solicitud activa para seleccionar STP y oferta.");
            return;
        }

        if (stpOfferPanel != null)
            stpOfferPanel.SetActive(true);

        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (roomInfoPanel != null)
            roomInfoPanel.gameObject.SetActive(false);

        Debug.Log("Panel STP y Oferta abierto.");
    }

    public void ContinueSTPAndOpenMap()
    {
        if (!pendingSegmentSelected)
        {
            Debug.Log("Debes elegir un segmento STP antes de continuar.");
            return;
        }

        if (!pendingOfferSelected)
        {
            Debug.Log("Debes elegir una oferta antes de continuar.");
            return;
        }

        selectedSegment = pendingSegment;
        selectedOffer = pendingOffer;

        segmentSelected = true;
        offerSelected = true;

        if (stpOfferPanel != null)
            stpOfferPanel.SetActive(false);

        ShowMapPanel();

        Debug.Log("STP confirmado: " + selectedSegment + " | Oferta confirmada: " + selectedOffer);
    }

    private void ShowMapPanel()
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

        if (room != null)
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
        if (currentRequest == null)
        {
            Debug.LogWarning("No existe una solicitud de check-in activa.");
            return;
        }

        if (selectedRoom == null)
        {
            Debug.Log("No has seleccionado habitación.");
            return;
        }

        if (!segmentSelected)
        {
            Debug.Log("Debes confirmar un segmento STP antes de asignar habitación.");
            return;
        }

        if (!offerSelected)
        {
            Debug.Log("Debes confirmar una oferta antes de asignar habitación.");
            return;
        }

        // Esto sí se bloquea: no puedes asignar una habitación ocupada o sucia.
        if (selectedRoom.state != RoomState.Libre)
        {
            Debug.Log("La habitación no está disponible.");
            return;
        }

        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        bool roomCorrect = IsRoomCorrectForRequest(selectedRoom, currentRequest);
        bool segmentCorrect = selectedSegment == currentRequest.correctSegment;
        bool offerCorrect = selectedOffer == currentRequest.bestOffer;

        int satisfaction = 50;
        int revenue = 100;

        // Habitación
        if (roomCorrect)
            satisfaction += 20;
        else
            satisfaction -= 20;

        // STP
        if (segmentCorrect)
            satisfaction += 15;
        else
            satisfaction -= 15;

        // Oferta
        if (offerCorrect)
        {
            satisfaction += 15;
            revenue += 50;
        }
        else
        {
            satisfaction -= 15;
            revenue -= 20;
        }

        satisfaction = Mathf.Clamp(satisfaction, 0, 100);
        revenue = Mathf.Max(0, revenue);

        selectedRoom.AssignGuest(currentRequest, currentDay);

        AssignNPCToRoom(selectedRoom);

        Debug.Log("Habitación asignada: " + selectedRoom.roomId);
        Debug.Log("Room correcta: " + roomCorrect + " | STP correcto: " + segmentCorrect + " | Oferta correcta: " + offerCorrect);

        KPIManager.Instance?.RegisterCheckInResult(
            segmentCorrect,
            offerCorrect,
            satisfaction,
            revenue
        );

        RegisterDailyCheckInResult(segmentCorrect, offerCorrect, roomCorrect, satisfaction, revenue);

        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (stpOfferPanel != null)
            stpOfferPanel.SetActive(false);

        if (roomInfoPanel != null)
            roomInfoPanel.gameObject.SetActive(false);

        if (resultPanel != null && resultPanelUI != null)
        {
            resultPanel.SetActive(true);
            resultPanelUI.Show(segmentCorrect, offerCorrect, roomCorrect, satisfaction, revenue);

            IsCheckInActive = false;
            playerMovement?.SetMovementEnabled(true);

            if (computerHighlight != null)
                computerHighlight.SetActive(false);

            if (computerInteractable != null)
                computerInteractable.SetEnabled(false);

            if (computerButton != null)
                computerButton.interactable = false;

            selectedRoom = null;
        }
        else
        {
            CloseCheckIn();
        }
    }

    private bool IsRoomCorrectForRequest(RoomData room, CheckInRequest request)
    {
        if (room == null) return false;
        if (request == null) return false;

        if (request.needsAccessibleRoom && !room.isAccessible)
            return false;

        if (request.bedType != room.bedType)
            return false;

        if (room.bedCount < request.guestCount)
            return false;

        return true;
    }

    private void RegisterDailyCheckInResult(bool segmentCorrect, bool offerCorrect, bool roomCorrect, int satisfaction, int revenue)
    {
        if (DailyResultsManager.Instance == null)
        {
            Debug.LogWarning("No existe DailyResultsManager. No se pudo guardar el resultado diario de Check-in.");
            return;
        }

        MiniGameResultData result = new MiniGameResultData();

        result.day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
        result.minigameName = "Check-in";

        result.satisfaction = satisfaction;
        result.revenue = revenue;

        result.errors =
            (!roomCorrect ? 1 : 0) +
            (!segmentCorrect ? 1 : 0) +
            (!offerCorrect ? 1 : 0);

        result.timeScore = 100;
        result.finalScore = satisfaction;

        result.stpSummary =
            "Segmentación elegida: " + selectedSegment +
            " / Oferta elegida: " + selectedOffer +
            " / Habitación asignada: " + selectedRoom.roomId;

        if (roomCorrect && segmentCorrect && offerCorrect)
        {
            result.feedback = "Excelente. La habitación, el segmento y la oferta fueron adecuados.";
        }
        else
        {
            result.feedback = "Hubo errores en la asignación. Revisa habitación, segmento STP y oferta.";
        }

        DailyResultsManager.Instance.RegisterResult(result);
    }

    private void AssignNPCToRoom(RoomData room)
    {
        if (room == null)
            return;

        room.hasGuest = true;
        room.guestCurrentArea = GuestArea.Room;
        room.hotelDoorSpawnId = "Door_" + room.roomId;

        if (dialogueController != null &&
            !string.IsNullOrEmpty(dialogueController.SelectedCustomerSpriteName))
        {
            room.guestSpriteName = dialogueController.SelectedCustomerSpriteName;

            Debug.Log(
                "Sprite guardado en habitación " +
                room.roomId +
                ": " +
                room.guestSpriteName
            );
        }
        else
        {
            room.guestSpriteName = "";

            Debug.LogWarning(
                "No se pudo guardar guestSpriteName en habitación " +
                room.roomId +
                ". El CheckInDialogueController no tiene sprite seleccionado."
            );
        }

        room.SaveToGameData();

        Debug.Log("NPC asignado a la habitación " + room.roomId);
    }

    public void CloseCheckIn()
    {
        Debug.Log("CloseCheckIn llamado");

        IsCheckInActive = false;

        if (checkInScreen != null) checkInScreen.SetActive(false);
        if (stpOfferPanel != null) stpOfferPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);
        if (noRoomsPanel != null) noRoomsPanel.SetActive(false);

        if (computerHighlight != null)
            computerHighlight.SetActive(false);

        if (computerInteractable != null)
            computerInteractable.SetEnabled(false);

        selectedRoom = null;
        currentRequest = null;

        pendingSegmentSelected = false;
        pendingOfferSelected = false;

        segmentSelected = false;
        offerSelected = false;

        dialogueFinished = false;

        if (computerButton != null)
            computerButton.interactable = false;

        playerMovement?.SetMovementEnabled(true);

        Debug.Log("Check-in cerrado por completo");
    }

    private bool HasFreeRooms()
    {
        if (allRooms == null) return false;

        foreach (RoomData room in allRooms)
        {
            if (room != null && room.state == RoomState.Libre)
                return true;
        }

        return false;
    }

    private bool IsRoomValid(RoomData room, CheckInRequest request)
    {
        if (room == null) return false;
        if (request == null) return false;

        if (room.state != RoomState.Libre) return false;
        if (request.needsAccessibleRoom && !room.isAccessible) return false;
        if (request.bedType != room.bedType) return false;
        if (room.bedCount < request.guestCount) return false;

        return true;
    }

    public void OpenReservationPanel()
    {
        if (checkInScreen != null) checkInScreen.SetActive(false);
        if (stpOfferPanel != null) stpOfferPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);
    }

    // Estos botones SOLO seleccionan temporalmente.
    // La selección real se confirma en ContinueSTPAndOpenMap().
    public void SelectSegment(int segmentIndex)
    {
        pendingSegment = (GuestSegment)segmentIndex;
        pendingSegmentSelected = true;

        Debug.Log("Segmento pendiente seleccionado: " + pendingSegment);
    }

    public void SelectOffer(int offerIndex)
    {
        pendingOffer = (OfferType)offerIndex;
        pendingOfferSelected = true;

        Debug.Log("Oferta pendiente seleccionada: " + pendingOffer);
    }
}

//Tengo sueño...