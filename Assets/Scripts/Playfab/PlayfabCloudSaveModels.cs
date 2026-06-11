using System.Collections.Generic;

[System.Serializable]
public class CloudGameStateData
{
    public bool hasStartedGame;
    public string savedSceneName;
    public int currentDay;
    public int selectedCharacter;
}

[System.Serializable]
public class CloudRoomListData
{
    public List<CloudRoomData> rooms = new List<CloudRoomData>();
}

[System.Serializable]
public class CloudRoomData
{
    public string roomId;

    public bool isAccessible;

    public int bedType;
    public int bedCount;

    public int state;
    public bool needsCleaning;
    public int reservedUntilDay;

    public int currentGuestSegment;
    public int currentOffer;
    public int currentMealPlan;
    public int currentGuestCount;
    public bool hasGuestData;

    public string hotelDoorSpawnId;
    public string guestSpriteName;
}

[System.Serializable]
public class CloudDailyResultListData
{
    public List<CloudDailyResultData> results = new List<CloudDailyResultData>();
}

[System.Serializable]
public class CloudDailyResultData
{
    public int day;
    public string minigameName;
    public int finalScore;
    public int revenue;
    public int errors;
}

[System.Serializable]
public class CloudNpcStateListData
{
    public List<CloudNpcStateData> npcs = new List<CloudNpcStateData>();
}

[System.Serializable]
public class CloudNpcStateData
{
    public string npcId;
    public string assignedRoomId;
    public string sceneName;

    public int area;

    public float positionX;
    public float positionY;
    public float positionZ;

    public bool hasValidPosition;

    public float lastSeenTime;
    public float nextDecisionTime;
}