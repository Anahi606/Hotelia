using SQLite;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class HoteliaSQLiteManager
{
    private static SQLiteConnection connection;

    private static string DatabasePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "hotelia_save.db");
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

        Debug.Log("SQLite inicializado en: " + DatabasePath);
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

                HotelDoorSpawnId = room.hotelDoorSpawnId,
                GuestSpriteName = room.guestSpriteName
            };

            connection.InsertOrReplace(entity);
        }
    }

    public static List<RoomRuntimeData> LoadRooms()
    {
        Initialize();

        List<RoomSaveEntity> entities = connection.Table<RoomSaveEntity>().ToList();
        List<RoomRuntimeData> rooms = new List<RoomRuntimeData>();

        foreach (RoomSaveEntity entity in entities)
        {
            RoomRuntimeData room = new RoomRuntimeData();

            room.roomId = entity.RoomId;

            room.isAccessible = entity.IsAccessible;

            room.bedType = (BedType)entity.BedType;
            room.bedCount = entity.BedCount;

            room.state = (RoomState)entity.State;
            room.needsCleaning = entity.NeedsCleaning;
            room.reservedUntilDay = entity.ReservedUntilDay;

            room.currentGuestSegment = (GuestSegment)entity.CurrentGuestSegment;
            room.currentOffer = (OfferType)entity.CurrentOffer;
            room.currentMealPlan = (MealPlan)entity.CurrentMealPlan;
            room.currentGuestCount = entity.CurrentGuestCount;
            room.hasGuestData = entity.HasGuestData;

            room.hotelDoorSpawnId = entity.HotelDoorSpawnId;
            room.guestSpriteName = entity.GuestSpriteName;

            rooms.Add(room);
        }

        return rooms;
    }

    public static void SaveDailyResults(List<MiniGameResultData> results)
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

                SelectedSegment = (int)result.selectedSegment,
                SelectedOffer = (int)result.selectedOffer,
                SelectedTourismExtra = (int)result.selectedTourismExtra
            };

            connection.Insert(entity);
        }
    }

    public static List<MiniGameResultData> LoadDailyResults()
    {
        Initialize();

        List<DailyResultEntity> entities = connection.Table<DailyResultEntity>().ToList();
        List<MiniGameResultData> results = new List<MiniGameResultData>();

        foreach (DailyResultEntity entity in entities)
        {
            MiniGameResultData result = new MiniGameResultData();

            result.day = entity.Day;
            result.minigameName = entity.MinigameName;

            result.finalScore = entity.FinalScore;
            result.satisfaction = entity.Satisfaction;
            result.revenue = entity.Revenue;
            result.errors = entity.Errors;
            result.timeScore = entity.TimeScore;

            result.stpSummary = entity.StpSummary;
            result.feedback = entity.Feedback;

            result.clientBudget = entity.ClientBudget;
            result.packageCost = entity.PackageCost;

            result.selectedSegment = (GuestSegment)entity.SelectedSegment;
            result.selectedOffer = (OfferType)entity.SelectedOffer;
            result.selectedTourismExtra = (TourismExtraType)entity.SelectedTourismExtra;

            results.Add(result);
        }

        return results;
    }

    public static void SaveVisibleNpcStates()
    {
        Initialize();

        GuestNPC[] visibleGuests = Object.FindObjectsByType<GuestNPC>(FindObjectsSortMode.None);

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
                SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
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

    public static void SaveNpcStates(List<NpcSaveEntity> npcs)
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

        return connection.Table<NpcSaveEntity>().ToList();
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

        connection.Execute("DELETE FROM sqlite_sequence WHERE name = ?", "DailyResults");

        Debug.Log("Base SQLite limpiada completamente.");
    }
}