using UnityEngine;

public class RoomData : MonoBehaviour
{
    [Header("Static Data")]
    public string roomId;
    public bool isAccessible;
    public BedType bedType;
    public int bedCount;

    [Header("Dynamic Data")]
    public RoomState state;
    public bool needsCleaning;
    public int reservedUntilDay = -1;

    [Header("Guest Data")]
    public GuestSegment currentGuestSegment;
    public OfferType currentOffer;
    public MealPlan currentMealPlan;
    public int currentGuestCount;
    public bool hasGuestData;

    [Header("Guest NPC")]
    public bool hasGuest;
    public GameObject guestPrefab;
    public GuestArea guestCurrentArea;
    public string hotelDoorSpawnId;

    private void Start()
    {
        if (HotelGameData.Instance != null)
        {
            HotelGameData.Instance.LoadRoomIntoRoomData(this);
        }
    }

    public bool IsReservationActive(int currentDay)
    {
        return currentDay <= reservedUntilDay;
    }

    public void AssignGuest(CheckInRequest request, int currentDay)
    {
        state = RoomState.Ocupada;
        needsCleaning = false;
        reservedUntilDay = currentDay + request.stayDays - 1;

        currentGuestSegment = request.correctSegment;
        currentOffer = request.bestOffer;
        currentMealPlan = request.mealPlan;
        currentGuestCount = request.guestCount;
        hasGuestData = true;

        SaveToGameData();
    }

    public void ClearGuest()
    {
        currentGuestSegment = default;
        currentOffer = OfferType.Ninguna;
        currentMealPlan = MealPlan.SoloAlojamiento;
        currentGuestCount = 0;
        hasGuestData = false;

        SaveToGameData();
    }

    public void SaveToGameData()
    {
        if (HotelGameData.Instance != null)
        {
            HotelGameData.Instance.SaveRoomFromRoomData(this);
        }
    }
}