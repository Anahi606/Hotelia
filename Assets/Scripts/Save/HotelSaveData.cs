using System.Collections.Generic;

[System.Serializable]
public class HotelSaveData
{
    public bool hasStartedGame;
    public string savedSceneName;
    public int currentDay;

    public PlayerCharacterType selectedCharacter;

    public List<RoomRuntimeData> rooms = new List<RoomRuntimeData>();
    public List<MiniGameResultData> allResults = new List<MiniGameResultData>();
}