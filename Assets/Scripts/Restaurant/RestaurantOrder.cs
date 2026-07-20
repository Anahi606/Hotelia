[System.Serializable]
public class RestaurantOrder
{
    public string roomId;
    public GuestSegment segment;
    public MealPlan mealPlan;
    public string dishName;

    public bool isUrgent;
    public bool hasAllergy;
    public bool isRoomService;

    public int priorityScore;
}