using System.Collections.Generic;
using UnityEngine;

public static class CheckInRequestGenerator
{
    public static CheckInRequest GenerateRequest(RoomData[] allRooms)
    {
        List<RoomData> freeRooms = new List<RoomData>();

        foreach (RoomData room in allRooms)
        {
            if (room.state == RoomState.Libre)
                freeRooms.Add(room);
        }

        if (freeRooms.Count == 0)
            return null;

        RoomData randomRoom = freeRooms[UnityEngine.Random.Range(0, freeRooms.Count)];

        CheckInRequest request = new CheckInRequest();
        request.needsAccessibleRoom = randomRoom.isAccessible;
        request.bedType = randomRoom.bedType;
        request.guestCount = Mathf.Clamp(randomRoom.bedCount, 1, 2);
        request.mealPlan = UnityEngine.Random.value > 0.5f
            ? MealPlan.Completo
            : MealPlan.SoloAlojamiento;

        return request;
    }
}