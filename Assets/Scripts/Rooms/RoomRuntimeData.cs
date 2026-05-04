[System.Serializable]
public class RoomRuntimeData
{
    public string roomId;
    public bool isAccessible;
    public BedType bedType;
    public int bedCount;
    public RoomState state;
    public bool needsCleaning;
    public int reservedUntilDay;
    public GuestSegment currentGuestSegment;
    public OfferType currentOffer;
    public MealPlan currentMealPlan;
    public int currentGuestCount;
    public bool hasGuestData;
    public string hotelDoorSpawnId;
}