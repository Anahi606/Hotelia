using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public static class PlayfabCloudSaveManager
{
    private const string GameStateKey = "Hotelia_GameState";
    private const string RoomsKey = "Hotelia_Rooms";
    private const string DailyResultsKey = "Hotelia_DailyResults";
    private const string NpcStatesKey = "Hotelia_NpcStates";
    private const string LastSyncKey = "Hotelia_LastSyncUtc";

    public static void UploadLocalSQLiteSaveToPlayFab(Action<bool> onFinished = null)
    {
        if (!PlayfabManager.IsLoggedInWithEmail)
        {
            Debug.Log("Progress was not uploaded to PlayFab because the player is not logged in with email.");
            onFinished?.Invoke(false);
            return;
        }

        if (PlayfabManager.IsTeacher)
        {
            Debug.Log("Progress was not uploaded because the current account is a teacher account.");
            onFinished?.Invoke(false);
            return;
        }

        HotelSaveData localSave = HotelSaveSystem.LoadGame();

        if (localSave == null || !localSave.hasStartedGame)
        {
            Debug.Log("There is no local save data to upload to PlayFab.");
            onFinished?.Invoke(false);
            return;
        }

        CloudGameStateData gameStateData = new CloudGameStateData
        {
            hasStartedGame = localSave.hasStartedGame,
            savedSceneName = localSave.savedSceneName,
            currentDay = localSave.currentDay,
            selectedCharacter = (int)localSave.selectedCharacter
        };

        CloudRoomListData roomListData = new CloudRoomListData
        {
            rooms = ConvertRooms(localSave.rooms)
        };

        CloudDailyResultListData resultListData = new CloudDailyResultListData
        {
            results = ConvertResults(localSave.allResults)
        };

        CloudNpcStateListData npcListData = new CloudNpcStateListData
        {
            npcs = ConvertNpcStates(HoteliaSQLiteManager.LoadNpcStates())
        };

        Dictionary<string, string> data = new Dictionary<string, string>
        {
            { GameStateKey, JsonUtility.ToJson(gameStateData, true) },
            { RoomsKey, JsonUtility.ToJson(roomListData, true) },
            { DailyResultsKey, JsonUtility.ToJson(resultListData, true) },
            { NpcStatesKey, JsonUtility.ToJson(npcListData, true) },
            { LastSyncKey, DateTime.UtcNow.ToString("o") }
        };

        var request = new UpdateUserDataRequest
        {
            Data = data,
            Permission = UserDataPermission.Private
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log("Local progress uploaded to PlayFab successfully.");
                onFinished?.Invoke(true);
            },
            error =>
            {
                Debug.LogError("Error uploading progress to PlayFab: " + error.GenerateErrorReport());
                onFinished?.Invoke(false);
            }
        );
    }

    private static List<CloudRoomData> ConvertRooms(List<RoomRuntimeData> rooms)
    {
        List<CloudRoomData> list = new List<CloudRoomData>();

        if (rooms == null)
            return list;

        foreach (RoomRuntimeData room in rooms)
        {
            if (room == null)
                continue;

            CloudRoomData data = new CloudRoomData
            {
                roomId = room.roomId,
                isAccessible = room.isAccessible,
                bedType = (int)room.bedType,
                bedCount = room.bedCount,
                state = (int)room.state,
                needsCleaning = room.needsCleaning,
                reservedUntilDay = room.reservedUntilDay,
                currentGuestSegment = (int)room.currentGuestSegment,
                currentOffer = (int)room.currentOffer,
                currentMealPlan = (int)room.currentMealPlan,
                currentGuestCount = room.currentGuestCount,
                hasGuestData = room.hasGuestData,
                hotelDoorSpawnId = room.hotelDoorSpawnId,
                guestSpriteName = room.guestSpriteName
            };

            list.Add(data);
        }

        return list;
    }

    private static List<CloudDailyResultData> ConvertResults(List<MiniGameResultData> results)
    {
        List<CloudDailyResultData> list = new List<CloudDailyResultData>();

        if (results == null)
            return list;

        foreach (MiniGameResultData result in results)
        {
            if (result == null)
                continue;

            CloudDailyResultData data = new CloudDailyResultData
            {
                day = result.day,
                minigameName = result.minigameName,
                finalScore = result.finalScore,
                revenue = result.revenue,
                errors = result.errors
            };

            list.Add(data);
        }

        return list;
    }

    private static List<CloudNpcStateData> ConvertNpcStates(List<NpcSaveEntity> npcs)
    {
        List<CloudNpcStateData> list = new List<CloudNpcStateData>();

        if (npcs == null)
            return list;

        foreach (NpcSaveEntity npc in npcs)
        {
            if (npc == null)
                continue;

            CloudNpcStateData data = new CloudNpcStateData
            {
                npcId = npc.NpcId,
                assignedRoomId = npc.AssignedRoomId,
                sceneName = npc.SceneName,
                area = npc.Area,
                positionX = npc.PositionX,
                positionY = npc.PositionY,
                positionZ = npc.PositionZ,
                hasValidPosition = npc.HasValidPosition,
                lastSeenTime = npc.LastSeenTime,
                nextDecisionTime = npc.NextDecisionTime
            };

            list.Add(data);
        }

        return list;
    }
}