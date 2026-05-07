using UnityEngine;
using TMPro;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    public int CurrentDay { get; private set; } = 1;

    [Header("References")]
    [SerializeField] private RoomData[] allRooms;
    [SerializeField] private TextMeshProUGUI dayText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void EndDay()
    {
        CurrentDay++;

        foreach (RoomData room in allRooms)
        {
            if (room == null) continue;

            if (room.state == RoomState.Ocupada && !room.IsReservationActive(CurrentDay))
            {
<<<<<<< Updated upstream
                room.state = RoomState.Sucia;
                room.needsCleaning = true;
                room.ClearGuest();
=======
                EndReservation(room);
            }
        }
    }

    private void EndReservation(RoomRuntimeData room)
    {
        string npcId = "Guest_" + room.roomId;
>>>>>>> Stashed changes

        // 1. Borra la memoria del NPC para que no reaparezca luego.
        GuestNPCMemory.RemoveState(npcId);

        // 2. Si el NPC está visible en la escena actual, lo destruye.
        DestroyVisibleGuest(room.roomId);

        // 3. La habitación pasa a sucia.
        room.state = RoomState.Sucia;
        room.needsCleaning = true;

        // 4. Limpia datos del huésped.
        room.hasGuestData = false;
        room.currentGuestCount = 0;
        room.currentOffer = OfferType.Ninguna;
        room.currentMealPlan = MealPlan.SoloAlojamiento;
        room.guestSpriteName = "";
        room.hotelDoorSpawnId = "";

        Debug.Log("La habitación " + room.roomId + " terminó su reserva, el NPC se eliminó y ahora está sucia.");
    }

    private void DestroyVisibleGuest(string roomId)
    {
        GuestNPC[] visibleGuests = Object.FindObjectsByType<GuestNPC>(FindObjectsSortMode.None);

        foreach (GuestNPC guest in visibleGuests)
        {
            if (guest == null) continue;

            if (guest.assignedRoomId == roomId)
            {
                guest.DisableSaveOnDestroy();
                Destroy(guest.gameObject);

                Debug.Log("NPC visible de la habitación " + roomId + " destruido.");
            }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (dayText != null)
            dayText.text = "Día " + CurrentDay;
    }
}