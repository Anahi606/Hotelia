using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    public int CurrentDay { get; private set; } = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCurrentDayFromSave();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void LoadCurrentDayFromSave()
    {
        HotelSaveData saveData = HotelSaveSystem.LoadGame();

        if (saveData != null && saveData.hasStartedGame)
        {
            CurrentDay = saveData.currentDay;
        }
        else
        {
            CurrentDay = 1;
        }
    }

    public void EndDay()
    {
        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.CommitTodayResults();
        }

        CurrentDay++;

        UpdateRoomsForNewDay();

        HotelSaveSystem.SaveEndOfDay();

        if (PlayfabManager.IsLoggedInWithEmail && PlayfabManager.IsStudent)
        {
            PlayfabCloudSaveManager.UploadLocalSQLiteSaveToPlayFab(success =>
            {
                if (success)
                    Debug.Log("Cloud save updated after ending day. Current day: " + CurrentDay);
                else
                    Debug.LogWarning("Cloud save was NOT updated after ending day.");
            });
        }
        else
        {
            Debug.Log("Progress saved only locally. User is not logged in as a student.");
        }

        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.ClearTodayResults();
        }

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

            if (room.state == RoomState.Occupied)
            {
                if (!IsReservationActive(room))
                {
                    EndReservation(room);
                }
                else
                {
                    MarkOccupiedRoomDirty(room);
                }
            }

            else if (room.state == RoomState.Dirty)
            {
                room.needsCleaning = true;
            }

            else if (room.state == RoomState.Available)
            {
                room.needsCleaning = false;
            }
        }
    }

    private void MarkOccupiedRoomDirty(RoomRuntimeData room)
    {
        if (room == null) return;

        room.state = RoomState.Occupied;
        room.needsCleaning = true;

        Debug.Log("La habitación " + room.roomId +
                  " sigue ocupada y ahora necesita limpieza diaria.");
    }

    private void EndReservation(RoomRuntimeData room)
    {
        string npcId = "Guest_" + room.roomId;

        //Borra la memoria del NPC para que no reaparezca luego.
        GuestNPCMemory.RemoveState(npcId);

        //Si el NPC está visible en la escena actual, lo destruye.
        DestroyVisibleGuest(room.roomId);

        //La habitación pasa a sucia.
        room.state = RoomState.Dirty;
        room.needsCleaning = true;

        //Limpia datos del huésped.
        room.hasGuestData = false;
        room.currentGuestCount = 0;
        //room.currentOffer = OfferType.None;
        room.currentMealPlan = MealPlan.AccommodationOnly;
        room.guestSpriteName = "";
        room.hotelDoorSpawnId = "";

        Debug.Log("La habitación " + room.roomId + " terminó su reserva, el NPC se eliminó y ahora está sucia.");
    }

    private void DestroyVisibleGuest(string roomId)
    {
        GuestNPC[] visibleGuests = Object.FindObjectsByType<GuestNPC>(FindObjectsSortMode.None);

        foreach (GuestNPC guest in visibleGuests)
        {
            if (guest == null) continue;

            if (guest.assignedRoomId == roomId)
            {
                guest.DisableSaveOnDestroy();
                Destroy(guest.gameObject);

                Debug.Log("NPC visible de la habitación " + roomId + " destruido.");
            }
        }
    }

    private bool IsReservationActive(RoomRuntimeData room)
    {
        return CurrentDay <= room.reservedUntilDay;
    }

    public void ResetDays()
    {
        SetCurrentDayFromSave(1);

        Debug.Log(
            "Días reiniciados para una nueva partida."
        );
    }

    public void SetCurrentDayFromSave(int savedDay)
    {
        CurrentDay = Mathf.Max(1, savedDay);

        DayTextUI.UpdateAllDayTexts();

        Debug.Log(
            "[DayManager] Día cargado en memoria: " +
            CurrentDay
        );
    }
    public void LoadDayFromSave(int savedDay)
    {
        SetCurrentDayFromSave(savedDay);
    }
}