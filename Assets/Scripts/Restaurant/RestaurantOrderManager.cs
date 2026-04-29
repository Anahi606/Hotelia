using System.Collections.Generic;
using UnityEngine;

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

    private readonly List<RestaurantOrder> activeOrders = new List<RestaurantOrder>();
    private readonly List<RestaurantOrder> selectedOrders = new List<RestaurantOrder>();

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

            if (room.state != RoomState.Ocupada) continue;
            if (!room.hasGuestData) continue;

            if (room.currentMealPlan != MealPlan.Completo) continue;

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
        order.priorityScore = CalculatePriority(order);

        return order;
    }

    private string GetDishBySegment(GuestSegment segment)
    {
        switch (segment)
        {
            case GuestSegment.Pareja:
                return "Cena especial";

            case GuestSegment.Familiar:
                return "Menú familiar";

            case GuestSegment.Ejecutivo:
                return "Almuerzo rápido";

            case GuestSegment.AdultoMayor:
                return "Menú ligero";

            case GuestSegment.Mochilero:
                return "Menú económico";

            default:
                return "Plato del día";
        }
    }

    private int CalculatePriority(RestaurantOrder order)
    {
        int score = 0;

        if (order.hasAllergy) score += 5;
        if (order.isUrgent) score += 4;
        if (order.isRoomService) score += 2;

        if (order.segment == GuestSegment.Ejecutivo) score += 2;
        if (order.segment == GuestSegment.AdultoMayor) score += 2;

        return score;
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

        int correctPositions = CalculateCorrectPositions();
        int errors = activeOrders.Count - correctPositions;

        int satisfaction = 60 + correctPositions * 15 - errors * 10;
        int timeScore = 60 + correctPositions * 10 - errors * 10;
        int revenue = activeOrders.Count * 100;

        if (errors == 0)
            revenue += 100;

        satisfaction = Mathf.Clamp(satisfaction, 0, 100);
        timeScore = Mathf.Clamp(timeScore, 0, 100);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (noOrdersPanel != null)
            noOrdersPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultPanelUI != null)
            resultPanelUI.Show(satisfaction, timeScore, errors, revenue);
    }

    private int CalculateCorrectPositions()
    {
        List<RestaurantOrder> correctOrder = new List<RestaurantOrder>(activeOrders);

        correctOrder.Sort((a, b) => b.priorityScore.CompareTo(a.priorityScore));

        int correct = 0;

        for (int i = 0; i < selectedOrders.Count; i++)
        {
            if (selectedOrders[i] == correctOrder[i])
                correct++;
        }

        return correct;
    }

    public void ResetSelection()
    {
        selectedOrders.Clear();

        RefreshTickets();
        RefreshSelectedSlots();
    }
}