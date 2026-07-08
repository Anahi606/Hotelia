using System.Collections.Generic;
using UnityEngine;

public static class CheckInRequestGenerator
{
    public static CheckInRequest GenerateRequest(RoomData[] allRooms)
    {
        List<RoomData> freeRooms = new List<RoomData>();

        foreach (RoomData room in allRooms)
        {
            if (room != null && room.state == RoomState.Available)
            {
                freeRooms.Add(room);
            }
        }

        if (freeRooms.Count == 0)
            return null;

        RoomData randomRoom = freeRooms[UnityEngine.Random.Range(0, freeRooms.Count)];

        CheckInRequest request = new CheckInRequest();

        request.needsAccessibleRoom = randomRoom.isAccessible;
        request.bedType = randomRoom.bedType;

        int maxGuests = GetMaxGuestsForRoom(randomRoom);
        request.guestCount = UnityEngine.Random.Range(1, maxGuests + 1);

        request.mealPlan = UnityEngine.Random.value > 0.5f
            ? MealPlan.Full
            : MealPlan.AccommodationOnly;

        request.stayDays = UnityEngine.Random.Range(1, 4);

        AssignCustomerSegment(request, randomRoom, maxGuests);
        AssignTravelMotivation(request);
        AdjustInvalidCombinations(request);

        return request;
    }

    private static int GetMaxGuestsForRoom(RoomData room)
    {
        if (room == null)
            return 1;

        if (room.bedType == BedType.Double)
            return Mathf.Max(1, room.bedCount * 2);

        return Mathf.Max(1, room.bedCount);
    }

    private static void AssignCustomerSegment(CheckInRequest request, RoomData room, int maxGuests)
    {
        List<GuestSegment> possibleSegments = new List<GuestSegment>();

        //Ejecutivo y viajero económico pueden usar casi cualquier habitacion
        possibleSegments.Add(GuestSegment.Executive);
        possibleSegments.Add(GuestSegment.BudgetTraveler);

        //Pareja solo si la habitacion puede recibir 2 personas y es double
        if (room.bedType == BedType.Double && maxGuests >= 2)
            possibleSegments.Add(GuestSegment.Couple);

        //Familia solo si la habitación puede recibir minimo 2 personas
        if (maxGuests >= 2)
            possibleSegments.Add(GuestSegment.Family);

        GuestSegment selectedSegment = possibleSegments[
            UnityEngine.Random.Range(0, possibleSegments.Count)
        ];

        request.correctSegment = selectedSegment;

        switch (selectedSegment)
        {
            case GuestSegment.Couple:
                request.guestProfile = "We are a couple traveling together.";
                request.guestCount = 2;
                request.bedType = BedType.Double;
                break;

            case GuestSegment.Family:
                request.guestProfile = "We are a family traveling together.";
                request.guestCount = Mathf.Clamp(
                    UnityEngine.Random.Range(2, 5),
                    2,
                    maxGuests
                );
                break;

            case GuestSegment.Executive:
                request.guestProfile = "I am traveling for business.";
                request.guestCount = 1;
                break;

            case GuestSegment.BudgetTraveler:
                request.guestProfile = "I am traveling alone and trying to save money.";
                request.guestCount = 1;
                request.mealPlan = MealPlan.AccommodationOnly;
                break;
        }
    }

    private static void AssignTravelMotivation(CheckInRequest request)
    {
        int motivationCase = UnityEngine.Random.Range(0, 5);

        switch (motivationCase)
        {
            case 0:
                request.travelReason = "a romantic getaway";
                request.tourismInterest = "a quiet and special experience";
                request.bestOffer = OfferType.Romantic;
                request.bestTourismExtra = TourismExtraType.RomanticDinner;
                request.budgetLevel = 3;
                request.clientBudget = 230;
                break;

            case 1:
                request.travelReason = "learning about the local culture";
                request.tourismInterest = "museums, local food and historic places";
                request.bestOffer = OfferType.Cultural;
                request.bestTourismExtra = TourismExtraType.CulturalTour;
                request.budgetLevel = 3;
                request.clientBudget = 240;
                break;

            case 2:
                request.travelReason = "adventure and nature";
                request.tourismInterest = "outdoor activities and nature";
                request.bestOffer = OfferType.Adventure;
                request.bestTourismExtra = TourismExtraType.NatureActivity;
                request.budgetLevel = 3;
                request.clientBudget = 250;
                break;

            case 3:
                request.travelReason = "work and meetings";
                request.tourismInterest = "fast transportation and comfort";
                request.bestOffer = OfferType.Executive;
                request.bestTourismExtra = TourismExtraType.BusinessTransport;
                request.budgetLevel = 2;
                request.clientBudget = 190;
                break;

            default:
                request.travelReason = "saving money during a short city visit";
                request.tourismInterest = "cheap transportation, simple activities and low-cost options";
                request.bestOffer = OfferType.Budget;
                request.bestTourismExtra = TourismExtraType.CityTransport;
                request.budgetLevel = 1;
                request.clientBudget = 130;
                request.mealPlan = MealPlan.AccommodationOnly;
                break;
        }
    }

    private static void AdjustInvalidCombinations(CheckInRequest request)
    {
        if (request.correctSegment == GuestSegment.Family &&
            request.bestOffer == OfferType.Executive)
        {
            request.travelReason = "a family vacation with local experiences";
            request.tourismInterest = "safe activities for the whole family";
            request.bestOffer = OfferType.Family;
            request.bestTourismExtra = TourismExtraType.FamilyActivity;
            request.budgetLevel = 3;
            request.clientBudget = 280;
        }

        if (request.correctSegment == GuestSegment.Executive &&
            request.bestOffer == OfferType.Romantic)
        {
            request.travelReason = "work and meetings";
            request.tourismInterest = "fast transportation and comfort";
            request.bestOffer = OfferType.Executive;
            request.bestTourismExtra = TourismExtraType.BusinessTransport;
            request.budgetLevel = 2;
            request.clientBudget = 190;
        }

        // BudgetTraveler no debe obligar siempre a Budget.
        // Solo ajustamos el presupuesto y meal plan para que sea coherente.
        if (request.correctSegment == GuestSegment.BudgetTraveler)
        {
            request.mealPlan = MealPlan.AccommodationOnly;

            if (request.bestOffer == OfferType.Romantic)
            {
                request.bestOffer = OfferType.Budget;
                request.bestTourismExtra = TourismExtraType.LocalSouvenir;
                request.travelReason = "saving money during a short city visit";
                request.tourismInterest = "simple and affordable local experiences";
                request.budgetLevel = 1;
                request.clientBudget = 120;
            }
            else if (request.bestOffer == OfferType.Cultural)
            {
                request.travelReason = "learning about the city on a low budget";
                request.tourismInterest = "affordable museums, local food and historic places";
                request.bestTourismExtra = TourismExtraType.CulturalTour;
                request.budgetLevel = 2;
                request.clientBudget = 180;
            }
            else if (request.bestOffer == OfferType.Adventure)
            {
                request.travelReason = "finding affordable outdoor activities";
                request.tourismInterest = "low-cost nature and outdoor experiences";
                request.bestTourismExtra = TourismExtraType.NatureActivity;
                request.budgetLevel = 2;
                request.clientBudget = 190;
            }
            else if (request.bestOffer == OfferType.Executive)
            {
                request.bestOffer = OfferType.Budget;
                request.bestTourismExtra = TourismExtraType.CityTransport;
                request.travelReason = "saving money while visiting the city";
                request.tourismInterest = "cheap transportation and practical city access";
                request.budgetLevel = 1;
                request.clientBudget = 130;
            }
            else
            {
                request.bestOffer = OfferType.Budget;
                request.bestTourismExtra = TourismExtraType.LocalSouvenir;
                request.travelReason = "saving money during a short city visit";
                request.tourismInterest = "simple and affordable local experiences";
                request.budgetLevel = 1;
                request.clientBudget = 120;
            }
        }
    }
}