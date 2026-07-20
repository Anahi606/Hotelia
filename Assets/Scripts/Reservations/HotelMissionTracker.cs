using System.Collections.Generic;
using UnityEngine;

public static class HotelMissionTracker
{
    public static List<HotelMissionData> BuildMissions()
    {
        List<HotelMissionData> missions =
            new List<HotelMissionData>();

        List<string> dirtyRooms = GetDirtyRoomIds();
        List<string> pendingFoodRooms =
            GetPendingFoodOrderRoomIds();
        List<string> freeRooms = GetFreeRoomIds();

        if (dirtyRooms.Count > 0)
            missions.Add(
                CreateCleanRoomsMission(dirtyRooms)
            );

        if (pendingFoodRooms.Count > 0)
            missions.Add(
                CreateFoodOrdersMission(pendingFoodRooms)
            );

        if (freeRooms.Count > 0)
            missions.Add(
                CreateFreeRoomsMission(freeRooms)
            );

        return missions;
    }

    private static HotelMissionData CreateCleanRoomsMission(List<string> roomIds)
    {
        int count = roomIds.Count;

        HotelMissionData mission = new HotelMissionData();
        mission.type = HotelMissionType.CleanRooms;
        mission.icon = count > 0 ? "!" : "✓";
        mission.title = "Room Cleaning";
        mission.shortText = count + " room(s) pending";
        mission.count = count;

        if (count > 0)
        {
            mission.description =
                "You still need to clean these room(s): " +
                FormatRoomIds(roomIds) +
                ".\n\nGo to the bedroom scene, enter each dirty room, collect the trash and make the beds.";
        }
        else
        {
            mission.description =
                "All rooms are clean. There are no cleaning tasks pending right now.";
        }

        return mission;
    }

    private static HotelMissionData CreateFoodOrdersMission(List<string> roomIds)
    {
        int count = roomIds.Count;

        HotelMissionData mission = new HotelMissionData();
        mission.type = HotelMissionType.FoodOrders;
        mission.icon = count > 0 ? "!" : "✓";
        mission.title = "Restaurant Orders";
        mission.shortText = count + " order(s) pending";
        mission.count = count;

        if (count > 0)
        {
            mission.description =
                "You still need to prepare food orders for these room(s): " +
                FormatRoomIds(roomIds) +
                ".\n\nGo to the restaurant and prioritize the tickets. Remember to check allergies, urgency and room service.";
        }
        else
        {
            mission.description =
                "There are no food orders pending right now.";
        }

        return mission;
    }

    private static HotelMissionData CreateFreeRoomsMission(List<string> roomIds)
    {
        int count = roomIds.Count;

        HotelMissionData mission = new HotelMissionData();
        mission.type = HotelMissionType.FreeRooms;
        mission.icon = "i";
        mission.title = "Available Rooms";
        mission.shortText = count + " room(s) free";
        mission.count = count;

        if (count > 0)
        {
            mission.description =
                "You can continue making reservations.\n\nAvailable room(s): " +
                FormatRoomIds(roomIds) +
                ".\n\nGo to the check-in computer and assign guests to a suitable room.";
        }
        else
        {
            mission.description =
                "There are no available rooms right now. Clean dirty rooms or wait until occupied rooms become free.";
        }

        return mission;
    }


    private static List<string> GetDirtyRoomIds()
    {
        List<string> ids = new List<string>();

        if (HotelGameData.Instance == null)
            return ids;

        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (room == null) continue;

            if (room.state == RoomState.Dirty || room.needsCleaning)
                ids.Add(room.roomId);
        }

        return ids;
    }

    private static List<string> GetPendingFoodOrderRoomIds()
    {
        List<string> ids = new List<string>();

        if (HotelGameData.Instance == null)
            return ids;

        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (IsFoodOrderPending(room))
                ids.Add(room.roomId);
        }

        return ids;
    }

    private static List<string> GetFreeRoomIds()
    {
        List<string> ids = new List<string>();

        if (HotelGameData.Instance == null)
            return ids;

        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (room == null) continue;

            if (room.state == RoomState.Available)
                ids.Add(room.roomId);
        }

        return ids;
    }

    public static bool IsFoodOrderPending(RoomRuntimeData room)
    {
        if (room == null)
            return false;

        if (room.state != RoomState.Occupied)
            return false;

        if (!room.hasGuestData)
            return false;

        if (room.currentMealPlan != MealPlan.Full)
            return false;

        int currentDay =
            DayManager.Instance != null
                ? DayManager.Instance.CurrentDay
                : 1;

        return room.lastRestaurantOrderCompletedDay != currentDay;
    }

    private static int GetCurrentDay()
    {
        return DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
    }

    private static string FormatRoomIds(List<string> roomIds)
    {
        if (roomIds == null || roomIds.Count == 0)
            return "None";

        return string.Join(", ", roomIds);
    }
}