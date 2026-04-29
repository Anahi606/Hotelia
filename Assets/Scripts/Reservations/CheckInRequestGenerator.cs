using System.Collections.Generic;
using UnityEngine;

public static class CheckInRequestGenerator
{
    public static CheckInRequest GenerateRequest(RoomData[] allRooms)
    {
        List<RoomData> freeRooms = new List<RoomData>();

        foreach (RoomData room in allRooms)
        {
            if (room != null && room.state == RoomState.Libre)
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
            ? MealPlan.Completo
            : MealPlan.SoloAlojamiento;
        request.stayDays = UnityEngine.Random.Range(1, 4);

        if (randomRoom.bedType == BedType.Matrimonial && request.guestCount == 2)
        {
            request.correctSegment = GuestSegment.Pareja;
            request.travelReason = "una escapada romántica";
            request.budgetLevel = 3;
            request.bestOffer = OfferType.Romantico;
        }
        else if (randomRoom.bedType == BedType.Separadas && request.guestCount >= 2)
        {
            request.correctSegment = GuestSegment.Familiar;
            request.travelReason = "vacaciones familiares";
            request.budgetLevel = 2;
            request.bestOffer = OfferType.Familiar;
        }
        else
        {
            request.correctSegment = GuestSegment.Ejecutivo;
            request.travelReason = "trabajo y reuniones";
            request.budgetLevel = 2;
            request.bestOffer = OfferType.Ejecutivo;
        }

        return request;
    }
}