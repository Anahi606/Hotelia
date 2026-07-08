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
    public TourismExtraType bestTourismExtra;

    [Header("Budget / Tourism")]
    public string guestProfile;
    public string travelReason;
    public string tourismInterest;
    public int budgetLevel;
    public int clientBudget;

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

        string profile = guestProfile;
        string reason = "The reason for our trip is " + travelReason + ".";
        string interest = "We are interested in " + tourismInterest + ".";
        string budget = "Our maximum budget is $" + clientBudget + ".";

        return new string[]
        {
            "Hello, I am here to check in.",
            profile,
            accessible,
            beds,
            "We are " + guestCount + " guest" + (guestCount > 1 ? "s." : "."),
            meals,
            days,
            reason,
            interest,
            budget,
            "Thank you."
        };
    }
}