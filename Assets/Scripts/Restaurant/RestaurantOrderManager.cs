using System.Collections.Generic;
using UnityEngine;
using Hotelia.Core;

public class RestaurantOrderManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject restaurantPanel;
    public GameObject noOrdersPanel;
    public GameObject gamePanel;
    public GameObject resultPanel;

    [Header("Ticket UI")]
    public RestaurantTicketUI[] ticketUIs;

    [Header("Selected Slots")]
    public RestaurantSelectedSlotUI[] selectedSlots;

    [Header("Result UI")]
    public RestaurantResultPanelUI resultPanelUI;

    [Header("Player")]
    public PlayerMovement playerMovement;

    [Header("Interaction")]
    [SerializeField] private RestaurantInteractable restaurantInteractable;

    private readonly List<RestaurantOrder> activeOrders = new List<RestaurantOrder>();
    private readonly List<RestaurantOrder> selectedOrders = new List<RestaurantOrder>();

    public void SetRestaurantInteractable(RestaurantInteractable interactable)
    {
        restaurantInteractable = interactable;
    }

    public void OpenRestaurant()
    {
        if (restaurantPanel != null)
            restaurantPanel.SetActive(true);

        if (noOrdersPanel != null)
            noOrdersPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        playerMovement?.SetMovementEnabled(false);

        GenerateOrdersFromOccupiedRooms();

        if (activeOrders.Count == 0)
        {
            if (noOrdersPanel != null)
                noOrdersPanel.SetActive(true);

            Debug.Log("No hay pedidos. No hay habitaciones ocupadas con plan completo.");
            return;
        }

        if (gamePanel != null)
            gamePanel.SetActive(true);

        RefreshTickets();
        RefreshSelectedSlots();
    }

    public void CloseRestaurant()
    {
        if (restaurantPanel != null)
            restaurantPanel.SetActive(false);

        if (noOrdersPanel != null)
            noOrdersPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        selectedOrders.Clear();

        playerMovement?.SetMovementEnabled(true);

        if (restaurantInteractable != null)
        {
            restaurantInteractable.NotifyRestaurantClosed();
        }
        else
        {
            Debug.LogWarning(
                "RestaurantInteractable no está registrado " +
                "en RestaurantOrderManager."
            );
        }

        Debug.Log("Restaurante cerrado y movimiento habilitado.");
    }

    private void GenerateOrdersFromOccupiedRooms()
    {
        activeOrders.Clear();
        selectedOrders.Clear();

        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData en la escena.");
            return;
        }

        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (room == null) continue;

            if (room.state != RoomState.Occupied) continue;
            if (!room.hasGuestData) continue;

            if (!HotelMissionTracker.IsFoodOrderPending(room)) continue;

            RestaurantOrder order = CreateOrderFromRoom(room);
            activeOrders.Add(order);

            if (activeOrders.Count >= 3)
                break;
        }
    }


    private RestaurantOrder CreateOrderFromRoom(RoomRuntimeData room)
    {
        RestaurantOrder order = new RestaurantOrder();

        order.roomId = room.roomId;
        order.segment = room.currentGuestSegment;
        order.mealPlan = room.currentMealPlan;
        order.isRoomService = true;

        order.hasAllergy = Random.value < 0.30f;
        order.isUrgent = Random.value < 0.40f;

        order.dishName = GetDishBySegment(room.currentGuestSegment);
        order.priorityScore =
            RestaurantPriorityRules.CalculatePriority(
                order.hasAllergy,
                order.isUrgent,
                order.isRoomService
            );

        return order;
    }

    private string GetDishBySegment(GuestSegment segment)
    {
        switch (segment)
        {
            case GuestSegment.Couple:
                return "Special dinner";

            case GuestSegment.Family:
                return "Family menu";

            case GuestSegment.Executive:
                return "Quick lunch";

            default:
                return "Dish of the day";
        }
    }
    private RestaurantOrderEvaluation EvaluateSelectedOrders()
    {
        List<int> selectedPriorities =
            new List<int>(selectedOrders.Count);

        foreach (RestaurantOrder order in selectedOrders)
        {
            if (order == null)
                continue;

            selectedPriorities.Add(order.priorityScore);
        }

        return RestaurantPriorityRules.Evaluate(
            selectedPriorities
        );
    }

    private void RefreshTickets()
    {
        int visibleIndex = 0;

        for (int i = 0; i < ticketUIs.Length; i++)
        {
            if (ticketUIs[i] != null)
                ticketUIs[i].Hide();
        }

        foreach (RestaurantOrder order in activeOrders)
        {
            if (selectedOrders.Contains(order))
                continue;

            if (visibleIndex >= ticketUIs.Length)
                break;

            if (ticketUIs[visibleIndex] != null)
            {
                ticketUIs[visibleIndex].Setup(order, this);
            }

            visibleIndex++;
        }
    }

    public void SelectOrder(RestaurantOrder order)
    {
        if (order == null) return;

        if (selectedOrders.Contains(order))
        {
            Debug.Log("Ese pedido ya fue seleccionado.");
            return;
        }

        selectedOrders.Add(order);

        RefreshTickets();
        RefreshSelectedSlots();
    }

    private void RefreshSelectedSlots()
    {
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] == null) continue;

            selectedSlots[i].SetManager(this);

            if (i < selectedOrders.Count)
            {
                selectedSlots[i].SetOrder(i + 1, selectedOrders[i]);
            }
            else
            {
                selectedSlots[i].SetEmpty(i + 1);
            }
        }
    }

    public void ReturnOrderToTickets(RestaurantOrder order)
    {
        if (order == null) return;

        if (!selectedOrders.Contains(order))
            return;

        selectedOrders.Remove(order);

        RefreshTickets();
        RefreshSelectedSlots();

        Debug.Log("Pedido devuelto: Habitación " + order.roomId);
    }

    public void ConfirmOrders()
    {
        if (activeOrders.Count == 0)
        {
            Debug.Log("No hay pedidos para confirmar.");
            return;
        }

        if (selectedOrders.Count < activeOrders.Count)
        {
            Debug.Log("Debes seleccionar todos los pedidos antes de enviar.");
            return;
        }

        RestaurantOrderEvaluation evaluation =
            EvaluateSelectedOrders();

        int correctPositions = evaluation.CorrectPositions;
        int totalOrders = activeOrders.Count;
        int errors = evaluation.Errors;

        float accuracy = totalOrders > 0
            ? correctPositions / (float)totalOrders
            : 0f;

        int satisfaction = Mathf.RoundToInt(accuracy * 100f);
        int timeScore = Mathf.RoundToInt(accuracy * 100f);

        int errorScore = Mathf.RoundToInt(accuracy * 100f);

        int revenue =
            (totalOrders * 100) +
            evaluation.Bonus;

        satisfaction = Mathf.Clamp(satisfaction, 0, 100);
        timeScore = Mathf.Clamp(timeScore, 0, 100);
        errorScore = Mathf.Clamp(errorScore, 0, 100);

        int finalScore = Mathf.RoundToInt(
            (satisfaction * 0.5f) +
            (timeScore * 0.3f) +
            (errorScore * 0.2f)
        );

        finalScore = Mathf.Clamp(finalScore, 0, 100);

        MiniGameResultData result = new MiniGameResultData();

        result.day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
        result.minigameName = "Restaurant";
        result.satisfaction = satisfaction;
        result.revenue = revenue;
        result.errors = errors;
        result.timeScore = timeScore;
        result.finalScore = finalScore;

        result.stpSummary = "Order prioritization based on allergies, urgency, room service, and guest type.";

        result.feedback = errors == 0
            ? "You prioritized the orders correctly."
            : "Review allergies, urgent orders, and room service requests more carefully.";

        DailyResultsManager.Instance?.RegisterResult(result);

        MarkSelectedOrdersAsCompletedToday();

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (noOrdersPanel != null)
            noOrdersPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultPanelUI != null)
            resultPanelUI.Show(satisfaction, timeScore, errors, revenue);
    }

    private void MarkSelectedOrdersAsCompletedToday()
    {
        if (HotelGameData.Instance == null)
            return;

        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        foreach (RestaurantOrder order in selectedOrders)
        {
            if (order == null) continue;

            RoomRuntimeData room = HotelGameData.Instance.GetRoomById(order.roomId);

            if (room == null) continue;

            room.lastRestaurantOrderCompletedDay = currentDay;
        }
    }

    public void ResetSelection()
    {
        selectedOrders.Clear();

        RefreshTickets();
        RefreshSelectedSlots();
    }
}