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
        request.guestCount = UnityEngine.Random.Range(1, randomRoom.bedCount + 1);

        request.mealPlan = UnityEngine.Random.value > 0.5f
            ? MealPlan.Full
            : MealPlan.AccommodationOnly;

        request.stayDays = UnityEngine.Random.Range(1, 4);

        if (randomRoom.bedType == BedType.Double && request.guestCount == 2)
        {
            request.correctSegment = GuestSegment.Couple;
            request.travelReason = "a romantic getaway";
            request.budgetLevel = 3;
            request.bestOffer = OfferType.Romantic;
        }
        else if (randomRoom.bedType == BedType.Separate && request.guestCount >= 2)
        {
            request.correctSegment = GuestSegment.Family;
            request.travelReason = "a family vacation";
            request.budgetLevel = 2;
            request.bestOffer = OfferType.Family;
        }
        else
        {
            request.correctSegment = GuestSegment.Executive;
            request.travelReason = "work and meetings";
            request.budgetLevel = 2;
            request.bestOffer = OfferType.Executive;
        }

        return request;
    }
}