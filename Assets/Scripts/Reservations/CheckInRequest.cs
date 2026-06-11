using UnityEngine;

[System.Serializable]
public class CheckInRequest
{
    [Header("Operational")]
    public bool needsAccessibleRoom;
    public BedType bedType;
    public int guestCount;
    public MealPlan mealPlan;
    public int stayDays;

    [Header("STP / Commercial")]
    public GuestSegment correctSegment;
    public OfferType bestOffer;
    public string travelReason;
    public int budgetLevel;

    public string[] GetDialogueLines()
    {
        string accessible = needsAccessibleRoom
            ? "I need an accessible room."
            : "I do not need accessibility features.";

        string beds = bedType == BedType.Double
            ? "I would like a double room."
            : "I would like a room with separate beds.";

        string meals = mealPlan == MealPlan.Full
            ? "I would like the full meal plan."
            : "Accommodation only, please.";

        string days = stayDays == 1
            ? "I will stay for 1 day."
            : "I will stay for " + stayDays + " days.";

        string reason = "I am traveling for " + travelReason + ".";

        return new string[]
        {
            "Hello, I am here to check in.",
            accessible,
            beds,
            "We are " + guestCount + " guest" + (guestCount > 1 ? "s." : "."),
            meals,
            days,
            reason,
            "Thank you."
        };
    }
}