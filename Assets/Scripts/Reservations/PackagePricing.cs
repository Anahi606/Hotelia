using UnityEngine;

public static class PackagePricing
{
    public static int GetBaseRoomPrice(BedType bedType, int stayDays)
    {
        int pricePerDay = bedType == BedType.Double ? 55 : 45;
        return pricePerDay * Mathf.Max(1, stayDays);
    }

    public static int GetRoomPrice(RoomData room, int stayDays)
    {
        if (room == null)
            return 0;

        return GetBaseRoomPrice(room.bedType, stayDays);
    }

    public static int GetMealPlanPrice(MealPlan mealPlan, int guestCount, int stayDays)
    {
        if (mealPlan == MealPlan.AccommodationOnly)
            return 0;

        int safeGuestCount = Mathf.Max(1, guestCount);
        int safeStayDays = Mathf.Max(1, stayDays);

        return 25 * safeGuestCount * safeStayDays;
    }

    public static int GetOfferPrice(OfferType offer)
    {
        switch (offer)
        {
            case OfferType.Romantic:
                return 45;

            case OfferType.Family:
                return 55;

            case OfferType.Executive:
                return 40;

            case OfferType.Cultural:
                return 50;

            case OfferType.Adventure:
                return 60;

            case OfferType.Budget:
                return 15;

            default:
                return 0;
        }
    }

    public static int GetTourismExtraPrice(TourismExtraType extra)
    {
        switch (extra)
        {
            case TourismExtraType.CulturalTour:
                return 35;

            case TourismExtraType.NatureActivity:
                return 45;

            case TourismExtraType.CityTransport:
                return 25;

            case TourismExtraType.RomanticDinner:
                return 40;

            case TourismExtraType.FamilyActivity:
                return 35;

            case TourismExtraType.BusinessTransport:
                return 30;

            case TourismExtraType.LocalSouvenir:
                return 10;

            case TourismExtraType.None:
            default:
                return 0;
        }
    }

    public static int CalculatePackageCost(CheckInRequest request, OfferType offer, TourismExtraType extra)
    {
        if (request == null)
            return 0;

        int roomPrice = GetBaseRoomPrice(request.bedType, request.stayDays);
        int mealPrice = GetMealPlanPrice(request.mealPlan, request.guestCount, request.stayDays);
        int offerPrice = GetOfferPrice(offer);
        int extraPrice = GetTourismExtraPrice(extra);

        return roomPrice + mealPrice + offerPrice + extraPrice;
    }

    public static int CalculatePackageCostWithRoom(
        CheckInRequest request,
        RoomData room,
        OfferType offer,
        TourismExtraType extra
    )
    {
        if (request == null)
            return 0;

        int roomPrice = GetRoomPrice(room, request.stayDays);
        int mealPrice = GetMealPlanPrice(request.mealPlan, request.guestCount, request.stayDays);
        int offerPrice = GetOfferPrice(offer);
        int extraPrice = GetTourismExtraPrice(extra);

        return roomPrice + mealPrice + offerPrice + extraPrice;
    }

    public static int CalculateHotelRevenue(CheckInRequest request, OfferType offer, TourismExtraType extra)
    {
        int totalPrice = CalculatePackageCost(request, offer, extra);
        return totalPrice / 2;
    }

    public static string GetOfferDisplayName(OfferType offer)
    {
        switch (offer)
        {
            case OfferType.Romantic:
                return "Romantic Package";

            case OfferType.Family:
                return "Family Package";

            case OfferType.Executive:
                return "Executive Package";

            case OfferType.Cultural:
                return "Cultural Package";

            case OfferType.Adventure:
                return "Adventure Package";

            case OfferType.Budget:
                return "Budget Package";

            default:
                return offer.ToString();
        }
    }

    public static string GetTourismExtraDisplayName(TourismExtraType extra)
    {
        switch (extra)
        {
            case TourismExtraType.CulturalTour:
                return "Cultural Tour";

            case TourismExtraType.NatureActivity:
                return "Nature Activity";

            case TourismExtraType.CityTransport:
                return "City Transport";

            case TourismExtraType.RomanticDinner:
                return "Romantic Dinner";

            case TourismExtraType.FamilyActivity:
                return "Family Activity";

            case TourismExtraType.BusinessTransport:
                return "Business Transport";

            case TourismExtraType.LocalSouvenir:
                return "Local Souvenir";

            case TourismExtraType.None:
                return "None";

            default:
                return extra.ToString();
        }
    }
}