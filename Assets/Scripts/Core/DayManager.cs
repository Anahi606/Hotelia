using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    public int CurrentDay { get; private set; } = 1;

    private const string DayKey = "CurrentDay";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentDay = PlayerPrefs.GetInt(DayKey, 1);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void EndDay()
    {
        CurrentDay++;

        PlayerPrefs.SetInt(DayKey, CurrentDay);
        PlayerPrefs.Save();

        UpdateRoomsForNewDay();

        DayTextUI.UpdateAllDayTexts();

        Debug.Log("Nuevo día: " + CurrentDay);
    }

    private void UpdateRoomsForNewDay()
    {
        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData en escena.");
            return;
        }

        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (room == null) continue;

            if (room.state == RoomState.Ocupada && !IsReservationActive(room))
            {
                room.state = RoomState.Sucia;
                room.needsCleaning = true;

                room.hasGuestData = false;
                room.currentGuestCount = 0;
                room.currentOffer = OfferType.Ninguna;
                room.currentMealPlan = MealPlan.SoloAlojamiento;
                room.guestSpriteName = "";
                room.hotelDoorSpawnId = "";

                Debug.Log("La habitación " + room.roomId + " terminó su reserva y ahora está sucia.");
            }
        }
    }

    private bool IsReservationActive(RoomRuntimeData room)
    {
        return CurrentDay <= room.reservedUntilDay;
    }

    public void ResetDays()
    {
        CurrentDay = 1;

        PlayerPrefs.SetInt(DayKey, CurrentDay);
        PlayerPrefs.Save();

        DayTextUI.UpdateAllDayTexts();
    }
}