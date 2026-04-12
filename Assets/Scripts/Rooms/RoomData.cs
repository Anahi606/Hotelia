using UnityEngine;

public class RoomData : MonoBehaviour
{
    [Header("Static Data")]
    public string roomId;
    public bool isAccessible;
    public BedType bedType;
    public int bedCount;

    [Header("Dynamic Data")]
    public RoomState state;
    public bool needsCleaning;
    public int reservedUntilDay = -1;

    public bool IsReservationActive(int currentDay)
    {
        return currentDay <= reservedUntilDay;
    }
}