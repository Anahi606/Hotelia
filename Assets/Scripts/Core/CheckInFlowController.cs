using System;
using TMPro;
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
    public TourismExtraType selectedTourismExtra;

    private GuestSegment pendingSegment;
    private OfferType pendingOffer;
    private TourismExtraType pendingTourismExtra;

    private bool pendingSegmentSelected;
    private bool pendingOfferSelected;
    private bool pendingTourismExtraSelected;

    private bool segmentSelected;
    private bool offerSelected;
    private bool tourismExtraSelected;

    private bool dialogueFinished;

    [Header("Budget Rejection Rules")]
    [SerializeField] private float severeBudgetOverPercent = 0.25f;

    private bool pendingRoomCorrect;
    private bool pendingSegmentCorrect;
    private bool pendingOfferCorrect;
    private bool pendingTourismExtraCorrect;
    private bool pendingBudgetCorrect;
    private bool pendingGuestAccepted;

    private int pendingPackageCost;
    private int pendingSatisfaction;
    private int pendingRevenue;

    private const int MaxFullMealPlanGuests = 3;

    [Header("Result UI")]
    public CheckInResultPanelUI resultPanelUI;

    [Header("Guest NPCs")]
    public GameObject[] guestNPCPrefabs;

    [Header("STP Buttons UI")]
    [SerializeField] private Button[] segmentButtons;
    [SerializeField] private Button[] offerButtons;

    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Tourism Extra Buttons UI")]
    [SerializeField] private Button[] tourismExtraButtons;

    [Header("Pricing Settings")]
    [SerializeField] private float vatRate = 0.13f;
    [SerializeField] private bool applyHotelServiceFee = true;
    [SerializeField] private float serviceFeeRate = 0.10f;

    [Header("Breakdown UI - Dollar Values")]
    [SerializeField] private TMP_Text dollarsBudgetText;
    [SerializeField] private TMP_Text dollarsRoomText;
    [SerializeField] private TMP_Text dollarsOfferText;
    [SerializeField] private TMP_Text dollarsExtraText;
    [SerializeField] private TMP_Text dollarsSubtotalText;
    [SerializeField] private TMP_Text dollarsVATText;
    [SerializeField] private TMP_Text dollarsServiceFeeText;
    [SerializeField] private TMP_Text dollarsTotalText;
    [SerializeField] private TMP_Text dollarsRemainingFeeText;

    [Header("Breakdown UI - Status")]
    [SerializeField] private TMP_Text statusText;

    [Header("Button Dollar Texts")]
    [SerializeField] private TMP_Text[] offerDollarTexts;
    [SerializeField] private TMP_Text[] tourismExtraDollarTexts;

    [Header("Optional Colors")]
    [SerializeField] private Color goodMoneyColor = new Color(0.1f, 0.45f, 0.1f);
    [SerializeField] private Color badMoneyColor = new Color(0.75f, 0.1f, 0.1f);
    [SerializeField] private Color normalMoneyColor = Color.black;

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
        pendingTourismExtraSelected = false;

        segmentSelected = false;
        offerSelected = false;
        tourismExtraSelected = false;

        ResetSTPButtonsVisual();

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

        EnsureCurrentRequestBudgetCanAffordBestPackage();

        UpdatePriceList();
        UpdateBudgetPreview();

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

        UpdatePriceList();
        UpdateBudgetPreview();

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

        if (!pendingTourismExtraSelected)
        {
            Debug.Log("Debes elegir un extra turístico antes de continuar.");
            return;
        }

        selectedSegment = pendingSegment;
        selectedOffer = pendingOffer;
        selectedTourismExtra = pendingTourismExtra;

        segmentSelected = true;
        offerSelected = true;
        tourismExtraSelected = true;

        if (stpOfferPanel != null)
            stpOfferPanel.SetActive(false);

        ShowMapPanel();

        Debug.Log(
            "STP confirmado: " + selectedSegment +
            " / Oferta confirmada: " + selectedOffer +
            " / Extra turístico confirmado: " + selectedTourismExtra
        );
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

        if (!tourismExtraSelected)
        {
            Debug.Log("Debes confirmar un extra turístico antes de asignar habitación.");
            return;
        }

        if (selectedRoom.state != RoomState.Available)
        {
            Debug.Log("La habitación no está disponible.");
            return;
        }

        if (RequestUsesFullMealPlan(currentRequest))
        {
            int currentFullMealPlanGuests = GetCurrentFullMealPlanGuestCount();
            int newGuests = Mathf.Max(1, currentRequest.guestCount);

            if (currentFullMealPlanGuests + newGuests > MaxFullMealPlanGuests)
            {
                Debug.Log(
                    "Full meal plan limit reached. Current full meal guests: " +
                    currentFullMealPlanGuests +
                    " / New guests: " +
                    newGuests +
                    " / Limit: " +
                    MaxFullMealPlanGuests
                );

                return;
            }
        }

        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        bool roomCorrect = IsRoomCorrectForRequest(selectedRoom, currentRequest);
        bool segmentCorrect = selectedSegment == currentRequest.correctSegment;
        bool offerCorrect = selectedOffer == currentRequest.bestOffer;
        bool tourismExtraCorrect = selectedTourismExtra == currentRequest.bestTourismExtra;

        PriceBreakdown finalBreakdown = CalculatePriceBreakdown(selectedRoom, true);

        int packageCost = Mathf.RoundToInt(finalBreakdown.total);
        bool budgetCorrect = finalBreakdown.withinBudget;

        bool severeBudgetOver = IsSevereBudgetOver(finalBreakdown);
        bool guestAccepted = !severeBudgetOver;

        int satisfaction = 50;
        int revenue = packageCost / 2;

        if (roomCorrect)
            satisfaction += 15;
        else
            satisfaction -= 20;

        if (segmentCorrect)
            satisfaction += 15;
        else
            satisfaction -= 15;

        if (offerCorrect)
            satisfaction += 15;
        else
            satisfaction -= 15;

        if (tourismExtraCorrect)
            satisfaction += 15;
        else
            satisfaction -= 10;

        if (budgetCorrect)
        {
            satisfaction += 10;
        }
        else
        {
            satisfaction -= 25;
            revenue -= 30;
        }

        if (!guestAccepted)
        {
            satisfaction -= 20;
            revenue = 0;
        }

        satisfaction = Mathf.Clamp(satisfaction, 0, 100);
        revenue = Mathf.Max(0, revenue);

        pendingRoomCorrect = roomCorrect;
        pendingSegmentCorrect = segmentCorrect;
        pendingOfferCorrect = offerCorrect;
        pendingTourismExtraCorrect = tourismExtraCorrect;
        pendingBudgetCorrect = budgetCorrect;
        pendingGuestAccepted = guestAccepted;

        pendingPackageCost = packageCost;
        pendingSatisfaction = satisfaction;
        pendingRevenue = revenue;

        if (guestAccepted)
        {
            selectedRoom.AssignGuest(currentRequest, currentDay);
            AssignNPCToRoom(selectedRoom);

            Debug.Log("Habitación asignada: " + selectedRoom.roomId);
        }
        else
        {
            Debug.Log("El cliente rechazó el paquete. No se ocupa habitación y no se gana dinero.");
        }

        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (stpOfferPanel != null)
            stpOfferPanel.SetActive(false);

        if (roomInfoPanel != null)
            roomInfoPanel.gameObject.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (checkInScreen != null)
            checkInScreen.SetActive(true);

        if (computerHighlight != null)
            computerHighlight.SetActive(false);

        if (computerInteractable != null)
            computerInteractable.SetEnabled(false);

        if (computerButton != null)
            computerButton.interactable = false;

        string[] finalDialogueLines = GetFinalCheckInDialogueLines(
            guestAccepted,
            budgetCorrect,
            severeBudgetOver,
            satisfaction
        );

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished = OnFinalCheckInDialogueFinished;
            dialogueController.StartFollowUpDialogue(finalDialogueLines);
        }
        else
        {
            ShowResultPanelAfterFinalDialogue();
        }
    }

    private bool IsSevereBudgetOver(PriceBreakdown breakdown)
    {
        if (breakdown.withinBudget)
            return false;

        if (breakdown.budget <= 0)
            return true;

        float maxAllowedTotal = breakdown.budget * (1f + severeBudgetOverPercent);

        return breakdown.total > maxAllowedTotal;
    }

    private string[] GetFinalCheckInDialogueLines(
        bool guestAccepted,
        bool budgetCorrect,
        bool severeBudgetOver,
        int satisfaction
    )
    {
        if (!guestAccepted || severeBudgetOver)
        {
            return new string[]
            {
            "This is way over our budget.",
            "Honestly, we are very disappointed.",
            "We are not going to stay."
            };
        }

        if (!budgetCorrect)
        {
            return new string[]
            {
            "The package is a little over our budget.",
            "We are not very happy about that, but thank you.",
            "We will accept the room."
            };
        }

        if (satisfaction < 60)
        {
            return new string[]
            {
            "Thank you.",
            "The offer does not fully meet our expectations.",
            "But we will stay this time."
            };
        }

        return new string[]
        {
        "Perfect, thank you very much.",
        "Everything looks good.",
        "We will take the room."
        };
    }

    private void OnFinalCheckInDialogueFinished()
    {
        ShowResultPanelAfterFinalDialogue();
    }

    private void ShowResultPanelAfterFinalDialogue()
    {
        KPIManager.Instance?.RegisterCheckInResult(
            pendingSegmentCorrect,
            pendingOfferCorrect,
            pendingSatisfaction,
            pendingRevenue
        );

        RegisterDailyCheckInResult(
            pendingSegmentCorrect,
            pendingOfferCorrect,
            pendingRoomCorrect,
            pendingTourismExtraCorrect,
            pendingBudgetCorrect,
            pendingPackageCost,
            pendingSatisfaction,
            pendingRevenue,
            pendingGuestAccepted
        );

        if (checkInScreen != null)
            checkInScreen.SetActive(false);

        if (stpOfferPanel != null)
            stpOfferPanel.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (roomInfoPanel != null)
            roomInfoPanel.gameObject.SetActive(false);

        if (resultPanel != null && resultPanelUI != null)
        {
            resultPanel.SetActive(true);

            resultPanelUI.Show(
                pendingSegmentCorrect,
                pendingOfferCorrect,
                pendingRoomCorrect,
                pendingSatisfaction,
                pendingRevenue
            );
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

        if (!RoomHasEnoughCapacity(room, request))
            return false;

        return true;
    }

    private void RegisterDailyCheckInResult(bool segmentCorrect,bool offerCorrect,bool roomCorrect,bool tourismExtraCorrect,bool budgetCorrect,int packageCost,int satisfaction,int revenue,bool guestAccepted)
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
            (!offerCorrect ? 1 : 0) +
            (!tourismExtraCorrect ? 1 : 0) +
            (!budgetCorrect ? 1 : 0);

        result.timeScore = 100;
        result.finalScore = satisfaction;

        result.clientBudget = currentRequest.clientBudget;
        result.packageCost = packageCost;

        result.selectedSegment = selectedSegment;
        result.selectedOffer = selectedOffer;
        result.selectedTourismExtra = selectedTourismExtra;
        result.hasDetailedCheckInScores = true;
        result.roomCorrect = roomCorrect;
        result.segmentCorrect = segmentCorrect;
        result.offerCorrect = offerCorrect;
        result.tourismExtraCorrect = tourismExtraCorrect;
        result.budgetCorrect = budgetCorrect;

        result.roomScore = roomCorrect ? 100 : 0;
        result.stpScore = segmentCorrect ? 100 : 0;
        result.offerScore = offerCorrect ? 100 : 0;
        result.tourismExtraScore = tourismExtraCorrect ? 100 : 0;
        result.budgetScore = budgetCorrect ? 100 : 0;

        result.stpSummary =
             "Selected segment: " + selectedSegment +
             " / Selected offer: " + selectedOffer +
             " / Tourism extra: " + selectedTourismExtra +
             " / Room: " + selectedRoom.roomId +
             " / Client budget: $" + currentRequest.clientBudget +
             " / Package cost: $" + packageCost +
             " / Hotel revenue: $" + revenue +
             " / Guest accepted: " + (guestAccepted ? "Yes" : "No");
        if (!guestAccepted)
        {
            result.feedback = "The package exceeded the client's budget too much. The guest decided not to stay.";
        }
        else if (roomCorrect && segmentCorrect && offerCorrect && tourismExtraCorrect && budgetCorrect)
        {
            result.feedback = "Excellent. The room, segment, offer, tourism extra and budget were all correctly matched.";
        }
        else if (!budgetCorrect)
        {
            result.feedback = "The package exceeded the client's budget. The guest accepted, but satisfaction was reduced.";
        }
        else if (!tourismExtraCorrect)
        {
            result.feedback = "The tourism extra did not match the customer's travel interest.";
        }
        else if (!segmentCorrect)
        {
            result.feedback = "The selected STP segment did not match the customer's travel reason.";
        }
        else if (!offerCorrect)
        {
            result.feedback = "The selected offer did not match the customer's needs.";
        }
        else
        {
            result.feedback = "There were some errors. Review the room, segment, offer, tourism extra and budget.";
        }

        if (roomCorrect && segmentCorrect && offerCorrect && tourismExtraCorrect && budgetCorrect)
        {
            result.feedback = "Excellent. The room, segment, offer, tourism extra and budget were all correctly matched.";
        }
        else if (!budgetCorrect)
        {
            result.feedback = "The package exceeded the client's budget. Choose a cheaper offer or tourism extra.";
        }
        else if (!tourismExtraCorrect)
        {
            result.feedback = "The tourism extra did not match the customer's travel interest.";
        }
        else if (!segmentCorrect)
        {
            result.feedback = "The selected STP segment did not match the customer's travel reason.";
        }
        else if (!offerCorrect)
        {
            result.feedback = "The selected offer did not match the customer's needs.";
        }
        else
        {
            result.feedback = "There were some errors. Review the room, segment, offer, tourism extra and budget.";
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

        if (HotelGameData.Instance != null)
        {
            RoomRuntimeData runtimeRoom = HotelGameData.Instance.GetRoomById(room.roomId);

            if (runtimeRoom != null)
                runtimeRoom.lastRestaurantOrderCompletedDay = -1;
        }

        Debug.Log("NPC asignado a la habitación " + room.roomId);
    }

    public CheckInRequest GetCurrentRequest()
    {
        return currentRequest;
    }

    public bool HasConfirmedPackage()
    {
        return segmentSelected && offerSelected && tourismExtraSelected;
    }

    public int GetTotalCostWithRoom(RoomData room)
    {
        if (currentRequest == null)
            return 0;

        if (!HasConfirmedPackage())
            return 0;

        PriceBreakdown breakdown = CalculatePriceBreakdown(room, true);
        return Mathf.RoundToInt(breakdown.total);
    }

    public int GetSelectedOfferPrice()
    {
        if (!offerSelected)
            return 0;

        return PackagePricing.GetOfferPrice(selectedOffer);
    }

    public int GetSelectedTourismExtraPrice()
    {
        if (!tourismExtraSelected)
            return 0;

        return PackagePricing.GetTourismExtraPrice(selectedTourismExtra);
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
        pendingTourismExtraSelected = false;

        segmentSelected = false;
        offerSelected = false;
        tourismExtraSelected = false;

        ResetSTPButtonsVisual();

        dialogueFinished = false;

        if (computerButton != null)
            computerButton.interactable = false;

        playerMovement?.SetMovementEnabled(true);

        ClearBreakdownUI();

        Debug.Log("Check-in cerrado por completo");
    }

    public void CloseResultAndFinishCheckIn()
    {
        CloseCheckIn();
    }

    private bool HasFreeRooms()
    {
        if (allRooms == null) return false;

        foreach (RoomData room in allRooms)
        {
            if (room != null && room.state == RoomState.Available)
                return true;
        }

        return false;
    }

    private bool IsRoomValid(RoomData room, CheckInRequest request)
    {
        if (room == null) return false;
        if (request == null) return false;

        if (room.state != RoomState.Available) return false;
        if (request.needsAccessibleRoom && !room.isAccessible) return false;
        if (request.bedType != room.bedType) return false;
        if (!RoomHasEnoughCapacity(room, request)) return false;

        return true;
    }

    public void OpenReservationPanel()
    {
        if (checkInScreen != null) checkInScreen.SetActive(false);
        if (stpOfferPanel != null) stpOfferPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (roomInfoPanel != null) roomInfoPanel.gameObject.SetActive(false);
    }

    //Estos botones solo seleccionan temporalmente. La selección real se confirma en ContinueSTPAndOpenMap().
    public void SelectSegment(int segmentIndex)
    {
        pendingSegment = (GuestSegment)segmentIndex;
        pendingSegmentSelected = true;

        UpdateSegmentButtonsVisual(segmentIndex);
        UpdateBudgetPreview();

        Debug.Log("Segmento pendiente seleccionado: " + pendingSegment);
    }

    public void SelectOffer(int offerIndex)
    {
        pendingOffer = (OfferType)offerIndex;
        pendingOfferSelected = true;

        UpdateOfferButtonsVisual(offerIndex);
        UpdateBudgetPreview();

        Debug.Log("Oferta pendiente seleccionada: " + pendingOffer);
    }

    public void SelectTourismExtra(int extraIndex)
    {
        pendingTourismExtra = (TourismExtraType)extraIndex;
        pendingTourismExtraSelected = true;

        UpdateTourismExtraButtonsVisual(extraIndex);
        UpdateBudgetPreview();

        Debug.Log("Extra turístico pendiente seleccionado: " + pendingTourismExtra);
    }

    private void UpdateSegmentButtonsVisual(int selectedIndex)
    {
        if (segmentButtons == null) return;

        for (int i = 0; i < segmentButtons.Length; i++)
        {
            if (segmentButtons[i] == null) continue;

            Image buttonImage = segmentButtons[i].targetGraphic as Image;

            if (buttonImage != null)
            {
                buttonImage.color = i == selectedIndex
                    ? selectedButtonColor
                    : normalButtonColor;
            }
        }
    }

    private void UpdateTourismExtraButtonsVisual(int selectedIndex)
    {
        if (tourismExtraButtons == null) return;

        for (int i = 0; i < tourismExtraButtons.Length; i++)
        {
            if (tourismExtraButtons[i] == null) continue;

            Image buttonImage = tourismExtraButtons[i].targetGraphic as Image;

            if (buttonImage != null)
            {
                buttonImage.color = i == selectedIndex
                    ? selectedButtonColor
                    : normalButtonColor;
            }
        }
    }

    private struct PriceBreakdown
    {
        public float budget;

        public float roomPrice;
        public float mealPrice;
        public float roomAndMeal;

        public float offerPrice;
        public float extraPrice;

        public float subtotal;
        public float vat;
        public float serviceFee;
        public float total;
        public float remaining;

        public bool withinBudget;
    }

    private PriceBreakdown CalculatePriceBreakdown(RoomData roomForCost, bool useConfirmedSelections)
    {
        PriceBreakdown breakdown = new PriceBreakdown();

        if (currentRequest == null)
            return breakdown;

        breakdown.budget = currentRequest.clientBudget;

        BedType bedTypeForCost = currentRequest.bedType;

        if (roomForCost != null)
            bedTypeForCost = roomForCost.bedType;

        breakdown.roomPrice = PackagePricing.GetBaseRoomPrice(
            bedTypeForCost,
            currentRequest.stayDays
        );

        breakdown.mealPrice = PackagePricing.GetMealPlanPrice(
            currentRequest.mealPlan,
            currentRequest.guestCount,
            currentRequest.stayDays
        );

        breakdown.roomAndMeal = breakdown.roomPrice + breakdown.mealPrice;

        bool hasOffer = useConfirmedSelections ? offerSelected : pendingOfferSelected;
        bool hasExtra = useConfirmedSelections ? tourismExtraSelected : pendingTourismExtraSelected;

        OfferType offerToUse = useConfirmedSelections ? selectedOffer : pendingOffer;
        TourismExtraType extraToUse = useConfirmedSelections ? selectedTourismExtra : pendingTourismExtra;

        breakdown.offerPrice = hasOffer
            ? PackagePricing.GetOfferPrice(offerToUse)
            : 0;

        breakdown.extraPrice = hasExtra
            ? PackagePricing.GetTourismExtraPrice(extraToUse)
            : 0;

        breakdown.subtotal =
            breakdown.roomAndMeal +
            breakdown.offerPrice +
            breakdown.extraPrice;

        breakdown.vat = breakdown.subtotal * vatRate;
        breakdown.serviceFee = breakdown.subtotal * serviceFeeRate;

        breakdown.total =
            breakdown.subtotal +
            breakdown.vat +
            breakdown.serviceFee;

        breakdown.remaining = breakdown.budget - breakdown.total;
        breakdown.withinBudget = breakdown.remaining >= 0;

        return breakdown;
    }

    private void EnsureCurrentRequestBudgetCanAffordBestPackage()
    {
        if (currentRequest == null)
            return;

        float bestPackageTotal = CalculateTotalForPackage(
            currentRequest.bedType,
            currentRequest.mealPlan,
            currentRequest.guestCount,
            currentRequest.stayDays,
            currentRequest.bestOffer,
            currentRequest.bestTourismExtra
        );

        int minimumBudget = Mathf.CeilToInt(bestPackageTotal);

        if (currentRequest.clientBudget >= minimumBudget)
            return;

        int oldBudget = currentRequest.clientBudget;

        int extraMargin = UnityEngine.Random.Range(0, 31);

        currentRequest.clientBudget = minimumBudget + extraMargin;

        Debug.Log(
            "Budget adjusted because the generated request was impossible. " +
            "Old budget: $" + oldBudget +
            " / Minimum required: $" + minimumBudget +
            " / New budget: $" + currentRequest.clientBudget
        );
    }

    private float CalculateTotalForPackage(
        BedType bedType,
        MealPlan mealPlan,
        int guestCount,
        int stayDays,
        OfferType offer,
        TourismExtraType extra
    )
    {
        float roomPrice = PackagePricing.GetBaseRoomPrice(
            bedType,
            stayDays
        );

        float mealPrice = PackagePricing.GetMealPlanPrice(
            mealPlan,
            Mathf.Max(1, guestCount),
            stayDays
        );

        float offerPrice = PackagePricing.GetOfferPrice(offer);
        float extraPrice = PackagePricing.GetTourismExtraPrice(extra);

        float subtotal = roomPrice + mealPrice + offerPrice + extraPrice;

        float vat = subtotal * vatRate;
        float serviceFee = subtotal * serviceFeeRate;

        return subtotal + vat + serviceFee;
    }

    private string Money(float value)
    {
        string sign = value < 0 ? "-" : "";
        float absoluteValue = Mathf.Abs(value);

        bool hasCents = Mathf.Abs(absoluteValue - Mathf.Round(absoluteValue)) > 0.001f;

        return sign + "$" + absoluteValue.ToString(hasCents ? "0.00" : "0");
    }

    private string AddedMoney(float value)
    {
        return "+" + Money(value);
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private void SetTextColor(TMP_Text text, Color color)
    {
        if (text != null)
            text.color = color;
    }

    private void UpdateBudgetPreview()
    {
        if (currentRequest == null)
        {
            ClearBreakdownUI();
            return;
        }

        PriceBreakdown breakdown = CalculatePriceBreakdown(null, false);

        SetText(dollarsBudgetText, Money(breakdown.budget));
        SetText(dollarsRoomText, Money(breakdown.roomAndMeal));
        SetText(dollarsOfferText, AddedMoney(breakdown.offerPrice));
        SetText(dollarsExtraText, AddedMoney(breakdown.extraPrice));
        SetText(dollarsSubtotalText, Money(breakdown.subtotal));
        SetText(dollarsVATText, Money(breakdown.vat));
        SetText(dollarsServiceFeeText, Money(breakdown.serviceFee));
        SetText(dollarsTotalText, Money(breakdown.total));
        SetText(dollarsRemainingFeeText, Money(breakdown.remaining));

        SetText(statusText, breakdown.withinBudget ? "Within Budget" : "Over Budget");

        SetTextColor(dollarsTotalText, breakdown.withinBudget ? goodMoneyColor : badMoneyColor);
        SetTextColor(dollarsRemainingFeeText, breakdown.withinBudget ? goodMoneyColor : badMoneyColor);
        SetTextColor(statusText, breakdown.withinBudget ? goodMoneyColor : badMoneyColor);
    }

    private void ClearBreakdownUI()
    {
        SetText(dollarsBudgetText, "$0");
        SetText(dollarsRoomText, "$0");
        SetText(dollarsOfferText, "+$0");
        SetText(dollarsExtraText, "+$0");
        SetText(dollarsSubtotalText, "$0");
        SetText(dollarsVATText, "$0");
        SetText(dollarsServiceFeeText, "$0");
        SetText(dollarsTotalText, "$0");
        SetText(dollarsRemainingFeeText, "$0");
        SetText(statusText, "-");
    }

    private string GetShortOfferName(OfferType offer)
    {
        switch (offer)
        {
            case OfferType.Romantic:
                return "Rom";

            case OfferType.Family:
                return "Fam";

            case OfferType.Executive:
                return "Exec";

            case OfferType.Cultural:
                return "Cult";

            case OfferType.Adventure:
                return "Adv";

            case OfferType.Budget:
                return "Bud";

            default:
                return offer.ToString();
        }
    }

    private string GetShortExtraName(TourismExtraType extra)
    {
        switch (extra)
        {
            case TourismExtraType.None:
                return "None";

            case TourismExtraType.CulturalTour:
                return "Tour";

            case TourismExtraType.NatureActivity:
                return "Nature";

            case TourismExtraType.CityTransport:
                return "Transport";

            case TourismExtraType.RomanticDinner:
                return "Dinner";

            case TourismExtraType.FamilyActivity:
                return "Family";

            case TourismExtraType.BusinessTransport:
                return "Business";

            case TourismExtraType.LocalSouvenir:
                return "Souvenir";

            default:
                return extra.ToString();
        }
    }

    private void UpdatePriceList()
    {
        RefreshChoiceButtonTexts();
    }

    private void RefreshChoiceButtonTexts()
    {
        SetDollarText(
            offerDollarTexts,
            0,
            PackagePricing.GetOfferPrice(OfferType.Romantic)
        );

        SetDollarText(
            offerDollarTexts,
            1,
            PackagePricing.GetOfferPrice(OfferType.Family)
        );

        SetDollarText(
            offerDollarTexts,
            2,
            PackagePricing.GetOfferPrice(OfferType.Executive)
        );

        SetDollarText(
            offerDollarTexts,
            3,
            PackagePricing.GetOfferPrice(OfferType.Cultural)
        );

        SetDollarText(
            offerDollarTexts,
            4,
            PackagePricing.GetOfferPrice(OfferType.Adventure)
        );

        SetDollarText(
            offerDollarTexts,
            5,
            PackagePricing.GetOfferPrice(OfferType.Budget)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            0,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.None)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            1,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.CulturalTour)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            2,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.NatureActivity)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            3,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.CityTransport)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            4,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.RomanticDinner)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            5,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.FamilyActivity)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            6,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.BusinessTransport)
        );

        SetDollarText(
            tourismExtraDollarTexts,
            7,
            PackagePricing.GetTourismExtraPrice(TourismExtraType.LocalSouvenir)
        );
    }

    private void SetDollarText(TMP_Text[] texts, int index, float price)
    {
        if (texts == null)
            return;

        if (index < 0 || index >= texts.Length)
            return;

        if (texts[index] == null)
            return;

        texts[index].text = AddedMoney(price);
    }

    private void UpdateOfferButtonsVisual(int selectedIndex)
    {
        if (offerButtons == null) return;

        for (int i = 0; i < offerButtons.Length; i++)
        {
            if (offerButtons[i] == null) continue;

            Image buttonImage = offerButtons[i].targetGraphic as Image;

            if (buttonImage != null)
            {
                buttonImage.color = i == selectedIndex
                    ? selectedButtonColor
                    : normalButtonColor;
            }
        }
    }

    private bool RoomHasEnoughCapacity(RoomData room, CheckInRequest request)
    {
        if (room == null || request == null)
            return false;

        int guestCount = Mathf.Max(1, request.guestCount);

        if (room.bedType == BedType.Double && room.bedCount >= 1)
        {
            return guestCount <= room.bedCount * 2;
        }

        // Para camas separadas/simples, cada cama cuenta como 1 persona.
        return room.bedCount >= guestCount;
    }

    private bool RequestUsesFullMealPlan(CheckInRequest request)
    {
        if (request == null)
            return false;

        // Usa el meal plan real de la solicitud
        return request.mealPlan == MealPlan.Full;
    }

    private int GetCurrentFullMealPlanGuestCount()
    {
        int count = 0;

        if (HotelGameData.Instance == null || HotelGameData.Instance.rooms == null)
            return count;

        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (room == null) continue;
            if (room.state != RoomState.Occupied) continue;
            if (!room.hasGuestData) continue;
            if (room.currentMealPlan != MealPlan.Full) continue;

            count += Mathf.Max(1, room.currentGuestCount);
        }

        return count;
    }

    private void ResetSTPButtonsVisual()
    {
        if (segmentButtons != null)
        {
            foreach (Button button in segmentButtons)
            {
                if (button == null) continue;

                Image buttonImage = button.targetGraphic as Image;

                if (buttonImage != null)
                    buttonImage.color = normalButtonColor;
            }
        }

        if (offerButtons != null)
        {
            foreach (Button button in offerButtons)
            {
                if (button == null) continue;

                Image buttonImage = button.targetGraphic as Image;

                if (buttonImage != null)
                    buttonImage.color = normalButtonColor;
            }
        }

        if (tourismExtraButtons != null)
        {
            foreach (Button button in tourismExtraButtons)
            {
                if (button == null) continue;

                Image buttonImage = button.targetGraphic as Image;

                if (buttonImage != null)
                    buttonImage.color = normalButtonColor;
            }
        }
    }
}

//Tengo sueño...