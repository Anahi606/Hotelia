using SQLite;

[SQLite.Table("GameState")]
public class GameStateEntity
{
    [SQLite.PrimaryKey]
    public int Id { get; set; }

    public bool HasStartedGame { get; set; }
    public string SavedSceneName { get; set; }
    public int CurrentDay { get; set; }

    public int SelectedCharacter { get; set; }
}

[SQLite.Table("Rooms")]
public class RoomSaveEntity
{
    [SQLite.PrimaryKey]
    public string RoomId { get; set; }

    public bool IsAccessible { get; set; }

    public int BedType { get; set; }
    public int BedCount { get; set; }

    public int State { get; set; }
    public bool NeedsCleaning { get; set; }
    public int ReservedUntilDay { get; set; }

    public int CurrentGuestSegment { get; set; }
    public int CurrentOffer { get; set; }
    public int CurrentMealPlan { get; set; }
    public int CurrentGuestCount { get; set; }
    public bool HasGuestData { get; set; }
    public int LastRestaurantOrderCompletedDay { get; set; }
    public string HotelDoorSpawnId { get; set; }
    public string GuestSpriteName { get; set; }
}

[SQLite.Table("DailyResults")]
public class DailyResultEntity
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public int Id { get; set; }

    public int Day { get; set; }
    public string MinigameName { get; set; }

    public int FinalScore { get; set; }
    public int Satisfaction { get; set; }
    public int Revenue { get; set; }
    public int Errors { get; set; }
    public int TimeScore { get; set; }

    public string StpSummary { get; set; }
    public string Feedback { get; set; }

    public int ClientBudget { get; set; }
    public int PackageCost { get; set; }

    public int SelectedSegment { get; set; }
    public int SelectedOffer { get; set; }
    public int SelectedTourismExtra { get; set; }
}

[SQLite.Table("NpcStates")]
public class NpcSaveEntity
{
    [SQLite.PrimaryKey]
    public string NpcId { get; set; }

    public string AssignedRoomId { get; set; }
    public string SceneName { get; set; }

    public int Area { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }

    public bool HasValidPosition { get; set; }

    public float LastSeenTime { get; set; }
    public float NextDecisionTime { get; set; }
}