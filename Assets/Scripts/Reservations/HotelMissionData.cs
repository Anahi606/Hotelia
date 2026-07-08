using System;

[Serializable]
public class HotelMissionData
{
    public HotelMissionType type;

    public string icon;
    public string title;
    public string shortText;
    public string description;

    public int count;
}

public enum HotelMissionType
{
    CleanRooms,
    FoodOrders,
    FreeRooms
}