using System.Collections.Generic;
using UnityEngine;

public class HotelGameData : MonoBehaviour
{
    public static HotelGameData Instance { get; private set; }

    [Header("Runtime Rooms")]
    public List<RoomRuntimeData> rooms = new List<RoomRuntimeData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRoomsIfEmpty();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeRoomsIfEmpty()
    {
        if (rooms.Count > 0) return;

        for (int i = 1; i <= 6; i++)
        {
            RoomRuntimeData room = new RoomRuntimeData();

            room.roomId = i.ToString("00");
            room.state = RoomState.Libre;
            room.needsCleaning = false;
            room.reservedUntilDay = -1;
            room.currentOffer = OfferType.Ninguna;
            room.currentMealPlan = MealPlan.SoloAlojamiento;
            room.currentGuestCount = 0;
            room.hasGuestData = false;

            rooms.Add(room);
        }
    }

    public RoomRuntimeData GetRoomById(string roomId)
    {
        foreach (RoomRuntimeData room in rooms)
        {
            if (room.roomId == roomId)
                return room;
        }

        return null;
    }

    public void SaveRoomFromRoomData(RoomData roomData)
    {
        if (roomData == null) return;

        RoomRuntimeData runtimeRoom = GetRoomById(roomData.roomId);

        if (runtimeRoom == null)
        {
            runtimeRoom = new RoomRuntimeData();
            runtimeRoom.roomId = roomData.roomId;
            rooms.Add(runtimeRoom);
        }

        runtimeRoom.state = roomData.state;
        runtimeRoom.needsCleaning = roomData.needsCleaning;
        runtimeRoom.reservedUntilDay = roomData.reservedUntilDay;

        runtimeRoom.currentGuestSegment = roomData.currentGuestSegment;
        runtimeRoom.currentOffer = roomData.currentOffer;
        runtimeRoom.currentMealPlan = roomData.currentMealPlan;
        runtimeRoom.currentGuestCount = roomData.currentGuestCount;
        runtimeRoom.hasGuestData = roomData.hasGuestData;
    }

    public void LoadRoomIntoRoomData(RoomData roomData)
    {
        if (roomData == null) return;

        RoomRuntimeData runtimeRoom = GetRoomById(roomData.roomId);

        if (runtimeRoom == null) return;

        roomData.state = runtimeRoom.state;
        roomData.needsCleaning = runtimeRoom.needsCleaning;
        roomData.reservedUntilDay = runtimeRoom.reservedUntilDay;

        roomData.currentGuestSegment = runtimeRoom.currentGuestSegment;
        roomData.currentOffer = runtimeRoom.currentOffer;
        roomData.currentMealPlan = runtimeRoom.currentMealPlan;
        roomData.currentGuestCount = runtimeRoom.currentGuestCount;
        roomData.hasGuestData = runtimeRoom.hasGuestData;
    }
}