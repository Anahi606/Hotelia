using SQLite;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class HoteliaSQLiteManager
{
    private const string GuestProfileId = "guest";

    private const string LastLocalPlayFabIdKey = "Hotelia_LastLocalPlayFabId";

    private static SQLiteConnection connection;
    private static string activeProfileId = GuestProfileId;

    public static string ActiveProfileId
    {
        get
        {
            return activeProfileId;
        }
    }

    public static string CurrentDatabasePath
    {
        get
        {
            return DatabasePath;
        }
    }

    private static string DatabasePath
    {
        get
        {
            string safeProfileId = SanitizeFileName(activeProfileId);

            return Path.Combine(
                Application.persistentDataPath,
                "hotelia_save_" + safeProfileId + ".db"
            );
        }
    }

    public static SQLiteConnection Connection
    {
        get
        {
            if (connection == null)
            {
                Initialize();
            }

            return connection;
        }
    }

    public static void Initialize()
    {
        if (connection != null)
            return;

        connection = new SQLiteConnection(DatabasePath);

        connection.CreateTable<GameStateEntity>();
        connection.CreateTable<RoomSaveEntity>();
        connection.CreateTable<DailyResultEntity>();
        connection.CreateTable<NpcSaveEntity>();

        Debug.Log(
            "[HoteliaSQLiteManager] SQLite initialized." +
            "\nProfile: " + activeProfileId +
            "\nPath: " + DatabasePath
        );
    }

    public static void UseGuestProfile()
    {
        SwitchProfile(GuestProfileId);
    }

    public static bool UsePlayFabProfile(
    string playFabId,
    bool rememberAsLastProfile = true
)
    {
        if (string.IsNullOrWhiteSpace(playFabId))
        {
            Debug.LogError(
                "[HoteliaSQLiteManager] The PlayFab profile could not " +
                "be activated because the PlayFabId is empty."
            );

            return false;
        }

        playFabId = playFabId.Trim();

        SwitchProfile(
            GetPlayFabProfileId(playFabId)
        );

        if (rememberAsLastProfile)
        {
            PlayerPrefs.SetString(
                LastLocalPlayFabIdKey,
                playFabId
            );

            PlayerPrefs.Save();

            Debug.Log(
                "[HoteliaSQLiteManager] Last local student remembered: " +
                playFabId
            );
        }

        return true;
    }

    public static bool TryUseLastPlayFabProfile()
    {
        string lastPlayFabId = PlayerPrefs.GetString(
            LastLocalPlayFabIdKey,
            ""
        );

        if (string.IsNullOrWhiteSpace(lastPlayFabId))
        {
            Debug.Log(
                "[HoteliaSQLiteManager] There is no remembered " +
                "student profile on this device."
            );

            return false;
        }

        bool activated = UsePlayFabProfile(
            lastPlayFabId,
            false
        );

        if (activated)
        {
            Debug.Log(
                "[HoteliaSQLiteManager] Remembered offline profile activated: " +
                ActiveProfileId
            );
        }

        return activated;
    }

    public static bool IsUsingPlayFabProfile(string playFabId)
    {
        if (string.IsNullOrWhiteSpace(playFabId))
            return false;

        string expectedProfileId =
            GetPlayFabProfileId(playFabId.Trim());

        return string.Equals(
            activeProfileId,
            expectedProfileId,
            System.StringComparison.Ordinal
        );
    }

    public static void ForgetLastPlayFabProfile()
    {
        PlayerPrefs.DeleteKey(
            LastLocalPlayFabIdKey
        );

        PlayerPrefs.Save();

        UseGuestProfile();

        Debug.Log(
            "[HoteliaSQLiteManager] The remembered student profile " +
            "was removed. Guest profile activated."
        );
    }

    private static string GetPlayFabProfileId(string playFabId)
    {
        return SanitizeFileName(
            "user_" + playFabId
        );
    }

    private static void SwitchProfile(string newProfileId)
    {
        if (string.IsNullOrWhiteSpace(newProfileId))
        {
            newProfileId = GuestProfileId;
        }

        newProfileId = SanitizeFileName(newProfileId);

        if (connection != null && activeProfileId == newProfileId)
        {
            return;
        }

        CloseConnection();

        activeProfileId = newProfileId;

        Initialize();

        Debug.Log(
            "[HoteliaSQLiteManager] SQLite profile changed." +
            "\nProfile: " + activeProfileId +
            "\nPath: " + DatabasePath
        );
    }

    private static void CloseConnection()
    {
        if (connection == null)
            return;

        try
        {
            connection.Close();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[HoteliaSQLiteManager] The previous SQLite connection " +
                "could not be closed correctly: " +
                exception.Message
            );
        }
        finally
        {
            connection = null;
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GuestProfileId;
        }

        char[] invalidCharacters = Path.GetInvalidFileNameChars();

        foreach (char invalidCharacter in invalidCharacters)
        {
            value = value.Replace(invalidCharacter, '_');
        }

        return value.Trim();
    }

    public static bool HasGameState()
    {
        Initialize();

        GameStateEntity state = connection.Find<GameStateEntity>(1);

        return state != null && state.HasStartedGame;
    }

    public static void SaveGameState(GameStateEntity state)
    {
        Initialize();

        if (state == null)
            return;

        state.Id = 1;

        connection.InsertOrReplace(state);
    }

    public static GameStateEntity LoadGameState()
    {
        Initialize();

        return connection.Find<GameStateEntity>(1);
    }

    public static void SaveRooms(List<RoomRuntimeData> rooms)
    {
        Initialize();

        if (rooms == null)
            return;

        foreach (RoomRuntimeData room in rooms)
        {
            if (room == null)
                continue;

            RoomSaveEntity entity = new RoomSaveEntity
            {
                RoomId = room.roomId,

                IsAccessible = room.isAccessible,

                BedType = (int)room.bedType,
                BedCount = room.bedCount,

                State = (int)room.state,
                NeedsCleaning = room.needsCleaning,
                ReservedUntilDay = room.reservedUntilDay,

                CurrentGuestSegment = (int)room.currentGuestSegment,
                CurrentOffer = (int)room.currentOffer,
                CurrentMealPlan = (int)room.currentMealPlan,
                CurrentGuestCount = room.currentGuestCount,
                HasGuestData = room.hasGuestData,

                LastRestaurantOrderCompletedDay = room.lastRestaurantOrderCompletedDay,

                HotelDoorSpawnId = room.hotelDoorSpawnId,
                GuestSpriteName = room.guestSpriteName
            };

            connection.InsertOrReplace(entity);
        }
    }

    public static List<RoomRuntimeData> LoadRooms()
    {
        Initialize();

        List<RoomSaveEntity> entities =
            connection.Table<RoomSaveEntity>().ToList();

        List<RoomRuntimeData> rooms =
            new List<RoomRuntimeData>();

        foreach (RoomSaveEntity entity in entities)
        {
            RoomRuntimeData room = new RoomRuntimeData
            {
                roomId = entity.RoomId,

                isAccessible = entity.IsAccessible,

                bedType = (BedType)entity.BedType,
                bedCount = entity.BedCount,

                state = (RoomState)entity.State,
                needsCleaning = entity.NeedsCleaning,
                reservedUntilDay = entity.ReservedUntilDay,

                currentGuestSegment =
                    (GuestSegment)entity.CurrentGuestSegment,

                currentOffer =
                    (OfferType)entity.CurrentOffer,

                currentMealPlan =
                    (MealPlan)entity.CurrentMealPlan,

                currentGuestCount = entity.CurrentGuestCount,

                hasGuestData = entity.HasGuestData,

                lastRestaurantOrderCompletedDay = entity.LastRestaurantOrderCompletedDay,

                hotelDoorSpawnId = entity.HotelDoorSpawnId,

                guestSpriteName = entity.GuestSpriteName
            };

            rooms.Add(room);
        }

        return rooms;
    }

    public static void SaveDailyResults(
        List<MiniGameResultData> results
    )
    {
        Initialize();

        if (results == null)
            return;

        foreach (MiniGameResultData result in results)
        {
            if (result == null)
                continue;

            DailyResultEntity entity = new DailyResultEntity
            {
                Day = result.day,
                MinigameName = result.minigameName,

                FinalScore = result.finalScore,
                Satisfaction = result.satisfaction,
                Revenue = result.revenue,
                Errors = result.errors,
                TimeScore = result.timeScore,

                StpSummary = result.stpSummary,
                Feedback = result.feedback,

                ClientBudget = result.clientBudget,
                PackageCost = result.packageCost,

                SelectedSegment =
                    (int)result.selectedSegment,

                SelectedOffer =
                    (int)result.selectedOffer,

                SelectedTourismExtra =
                    (int)result.selectedTourismExtra
            };

            connection.Insert(entity);
        }
    }

    public static List<MiniGameResultData> LoadDailyResults()
    {
        Initialize();

        List<DailyResultEntity> entities =
            connection.Table<DailyResultEntity>().ToList();

        List<MiniGameResultData> results =
            new List<MiniGameResultData>();

        foreach (DailyResultEntity entity in entities)
        {
            MiniGameResultData result = new MiniGameResultData
            {
                day = entity.Day,
                minigameName = entity.MinigameName,

                finalScore = entity.FinalScore,
                satisfaction = entity.Satisfaction,
                revenue = entity.Revenue,
                errors = entity.Errors,
                timeScore = entity.TimeScore,

                stpSummary = entity.StpSummary,
                feedback = entity.Feedback,

                clientBudget = entity.ClientBudget,
                packageCost = entity.PackageCost,

                selectedSegment =
                    (GuestSegment)entity.SelectedSegment,

                selectedOffer =
                    (OfferType)entity.SelectedOffer,

                selectedTourismExtra =
                    (TourismExtraType)entity.SelectedTourismExtra
            };

            results.Add(result);
        }

        return results;
    }

    public static void SaveVisibleNpcStates()
    {
        Initialize();

        GuestNPC[] visibleGuests =
            Object.FindObjectsByType<GuestNPC>(
                FindObjectsSortMode.None
            );

        foreach (GuestNPC guest in visibleGuests)
        {
            if (guest == null)
                continue;

            if (string.IsNullOrEmpty(guest.npcId))
                continue;

            NpcSaveEntity entity = new NpcSaveEntity
            {
                NpcId = guest.npcId,
                AssignedRoomId = guest.assignedRoomId,

                SceneName =
                    UnityEngine.SceneManagement
                        .SceneManager
                        .GetActiveScene()
                        .name,

                Area = (int)guest.currentArea,

                PositionX = guest.transform.position.x,
                PositionY = guest.transform.position.y,
                PositionZ = guest.transform.position.z,

                HasValidPosition = true,
                LastSeenTime = Time.time,
                NextDecisionTime = Time.time
            };

            connection.InsertOrReplace(entity);
        }
    }

    public static void SaveNpcStates(
        List<NpcSaveEntity> npcs
    )
    {
        Initialize();

        if (npcs == null)
            return;

        foreach (NpcSaveEntity npc in npcs)
        {
            if (npc == null)
                continue;

            connection.InsertOrReplace(npc);
        }
    }

    public static List<NpcSaveEntity> LoadNpcStates()
    {
        Initialize();

        return connection
            .Table<NpcSaveEntity>()
            .ToList();
    }

    public static void DeleteNpcState(string npcId)
    {
        Initialize();

        if (string.IsNullOrEmpty(npcId))
            return;

        connection.Delete<NpcSaveEntity>(npcId);
    }

    public static void DeleteAllSaveData()
    {
        Initialize();

        connection.DeleteAll<GameStateEntity>();
        connection.DeleteAll<RoomSaveEntity>();
        connection.DeleteAll<DailyResultEntity>();
        connection.DeleteAll<NpcSaveEntity>();

        try
        {
            connection.Execute(
                "DELETE FROM sqlite_sequence WHERE name = ?",
                "DailyResults"
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[HoteliaSQLiteManager] DailyResults sequence " +
                "could not be reset: " +
                exception.Message
            );
        }

        Debug.Log(
            "[HoteliaSQLiteManager] SQLite save deleted." +
            "\nProfile: " + activeProfileId +
            "\nPath: " + DatabasePath
        );
    }
}