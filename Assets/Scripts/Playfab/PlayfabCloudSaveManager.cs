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

    public static void SyncBestSaveBetweenLocalAndPlayFab(
    Action<HotelSaveData> onFinished
)
    {
        if (!PlayfabManager.IsLoggedInWithEmail)
        {
            HotelSaveData localOnlySave =
                HotelSaveSystem.LoadGame();

            FinishSync(
                onFinished,
                localOnlySave
            );

            return;
        }

        if (PlayfabManager.IsTeacher)
        {
            Debug.Log(
                "Teacher accounts do not sync gameplay progress."
            );

            FinishSync(
                onFinished,
                null
            );

            return;
        }

        HotelSaveData localSave =
            HotelSaveSystem.LoadGame();

        var request = new GetUserDataRequest
        {
            Keys = new List<string>
        {
            GameStateKey,
            RoomsKey,
            DailyResultsKey,
            NpcStatesKey,
            LastSyncKey
        }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                HotelSaveData cloudSave =
                    BuildHotelSaveDataFromCloud(
                        result.Data
                    );

                bool hasLocal =
                    localSave != null &&
                    localSave.hasStartedGame;

                bool hasCloud =
                    cloudSave != null &&
                    cloudSave.hasStartedGame;

                if (!hasLocal && !hasCloud)
                {
                    Debug.Log(
                        "No local save and no cloud save found."
                    );

                    FinishSync(
                        onFinished,
                        null
                    );

                    return;
                }

                if (hasLocal && !hasCloud)
                {
                    Debug.Log(
                        "Only local save exists. " +
                        "Uploading local save to PlayFab."
                    );

                    UploadLocalSQLiteSaveToPlayFab(
                        success =>
                        {
                            FinishSync(
                                onFinished,
                                localSave
                            );
                        }
                    );

                    return;
                }

                if (!hasLocal && hasCloud)
                {
                    Debug.Log(
                        "Only cloud save exists. " +
                        "Downloading cloud save to SQLite."
                    );

                    SaveCloudSaveToSQLite(
                        result.Data
                    );

                    FinishSync(
                        onFinished,
                        HotelSaveSystem.LoadGame()
                    );

                    return;
                }

                int localDay = localSave.currentDay;
                int cloudDay = cloudSave.currentDay;

                if (localDay > cloudDay)
                {
                    Debug.Log(
                        "Local save is ahead. " +
                        "Uploading local save to PlayFab."
                    );

                    UploadLocalSQLiteSaveToPlayFab(
                        success =>
                        {
                            FinishSync(
                                onFinished,
                                localSave
                            );
                        }
                    );

                    return;
                }

                if (cloudDay > localDay)
                {
                    Debug.Log(
                        "Cloud save is ahead. " +
                        "Downloading cloud save to SQLite."
                    );

                    SaveCloudSaveToSQLite(
                        result.Data
                    );

                    FinishSync(
                        onFinished,
                        HotelSaveSystem.LoadGame()
                    );

                    return;
                }

                int localResultsCount =
                    localSave.allResults != null
                        ? localSave.allResults.Count
                        : 0;

                int cloudResultsCount =
                    cloudSave.allResults != null
                        ? cloudSave.allResults.Count
                        : 0;

                if (cloudResultsCount > localResultsCount)
                {
                    Debug.Log(
                        "Same day, but cloud has more results. " +
                        "Downloading cloud save."
                    );

                    SaveCloudSaveToSQLite(
                        result.Data
                    );

                    FinishSync(
                        onFinished,
                        HotelSaveSystem.LoadGame()
                    );

                    return;
                }

                Debug.Log(
                    "Local save is equal or better. " +
                    "Keeping local save and uploading it to PlayFab."
                );

                UploadLocalSQLiteSaveToPlayFab(
                    success =>
                    {
                        FinishSync(
                            onFinished,
                            localSave
                        );
                    }
                );
            },
            error =>
            {
                Debug.LogError(
                    "Could not read PlayFab cloud save: " +
                    error.GenerateErrorReport()
                );

                HotelSaveData fallbackLocalSave =
                    HotelSaveSystem.LoadGame();

                FinishSync(
                    onFinished,
                    fallbackLocalSave
                );
            }
        );
    }
    private static void FinishSync(
    Action<HotelSaveData> onFinished,
    HotelSaveData save)
    {
        HotelSaveSystem.ApplyLoadedDataToRuntime(save);

        onFinished?.Invoke(save);
    }

    public static void DeleteCloudGameplaySave(Action<bool> onFinished = null)
    {
        if (!PlayfabManager.IsLoggedInWithEmail ||
            !PlayfabManager.IsStudent ||
            string.IsNullOrWhiteSpace(
                PlayfabManager.CurrentPlayFabId
            ))
        {
            Debug.LogError(
                "[PlayfabCloudSaveManager] Cloud deletion denied. " +
                "There is no valid logged student."
            );

            onFinished?.Invoke(false);
            return;
        }

        var request = new UpdateUserDataRequest
        {
            KeysToRemove = new List<string>
        {
            GameStateKey,
            RoomsKey,
            DailyResultsKey,
            NpcStatesKey,
            LastSyncKey
        }
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log(
                    "[PlayfabCloudSaveManager] Save deleted only " +
                    "for the currently authenticated student: " +
                    PlayfabManager.CurrentPlayFabId
                );

                onFinished?.Invoke(true);
            },
            error =>
            {
                Debug.LogError(
                    "[PlayfabCloudSaveManager] The current student's " +
                    "save could not be deleted: " +
                    error.GenerateErrorReport()
                );

                onFinished?.Invoke(false);
            }
        );
    }

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
            Debug.Log(
                "Progress was not uploaded because " +
                "the current account is a teacher account."
            );

            onFinished?.Invoke(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(
            PlayfabManager.CurrentPlayFabId
        ))
        {
            Debug.LogError(
                "Progress cannot be uploaded because " +
                "the PlayFabId is missing."
            );

            onFinished?.Invoke(false);
            return;
        }

        bool correctLocalProfile =
        HoteliaSQLiteManager.IsUsingPlayFabProfile(
            PlayfabManager.CurrentPlayFabId
        );

        if (!correctLocalProfile)
        {
            Debug.LogError(
                "El progreso no se puede subir porque el perfil " +
                "SQLite activo no pertenece a la cuenta conectada." +
                "\nCuenta PlayFab: " +
                PlayfabManager.CurrentPlayFabId +
                "\nPerfil SQLite activo: " +
                HoteliaSQLiteManager.ActiveProfileId
            );

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

    private static HotelSaveData BuildHotelSaveDataFromCloud(Dictionary<string, UserDataRecord> data)
    {
        if (data == null || !data.ContainsKey(GameStateKey))
            return null;

        CloudGameStateData gameState = JsonUtility.FromJson<CloudGameStateData>(data[GameStateKey].Value);

        if (gameState == null || !gameState.hasStartedGame)
            return null;

        HotelSaveData save = new HotelSaveData
        {
            hasStartedGame = gameState.hasStartedGame,
            savedSceneName = gameState.savedSceneName,
            currentDay = gameState.currentDay,
            selectedCharacter = (PlayerCharacterType)gameState.selectedCharacter
        };

        if (data.ContainsKey(RoomsKey))
        {
            CloudRoomListData roomsData = JsonUtility.FromJson<CloudRoomListData>(data[RoomsKey].Value);

            if (roomsData != null && roomsData.rooms != null)
                save.rooms = ConvertCloudRoomsToRuntimeRooms(roomsData.rooms);
        }

        if (data.ContainsKey(DailyResultsKey))
        {
            CloudDailyResultListData resultsData = JsonUtility.FromJson<CloudDailyResultListData>(data[DailyResultsKey].Value);

            if (resultsData != null && resultsData.results != null)
                save.allResults = ConvertCloudResultsToMiniGameResults(resultsData.results);
        }

        return save;
    }

    private static void SaveCloudSaveToSQLite(Dictionary<string, UserDataRecord> data)
    {
        if (data == null || !data.ContainsKey(GameStateKey))
        {
            Debug.LogWarning("Cannot save cloud data to SQLite because GameState is missing.");
            return;
        }

        CloudGameStateData gameState = JsonUtility.FromJson<CloudGameStateData>(data[GameStateKey].Value);

        if (gameState == null || !gameState.hasStartedGame)
        {
            Debug.LogWarning("Cloud GameState is empty or invalid.");
            return;
        }

        HoteliaSQLiteManager.DeleteAllSaveData();

        GameStateEntity gameStateEntity = new GameStateEntity
        {
            Id = 1,
            HasStartedGame = gameState.hasStartedGame,
            SavedSceneName = gameState.savedSceneName,
            CurrentDay = gameState.currentDay,
            SelectedCharacter = gameState.selectedCharacter
        };

        HoteliaSQLiteManager.SaveGameState(gameStateEntity);

        if (data.ContainsKey(RoomsKey))
        {
            CloudRoomListData roomsData = JsonUtility.FromJson<CloudRoomListData>(data[RoomsKey].Value);

            if (roomsData != null && roomsData.rooms != null)
            {
                List<RoomRuntimeData> runtimeRooms = ConvertCloudRoomsToRuntimeRooms(roomsData.rooms);
                HoteliaSQLiteManager.SaveRooms(runtimeRooms);
            }
        }

        if (data.ContainsKey(DailyResultsKey))
        {
            CloudDailyResultListData resultsData = JsonUtility.FromJson<CloudDailyResultListData>(data[DailyResultsKey].Value);

            if (resultsData != null && resultsData.results != null)
            {
                List<MiniGameResultData> results = ConvertCloudResultsToMiniGameResults(resultsData.results);
                HoteliaSQLiteManager.SaveDailyResults(results);
            }
        }

        if (data.ContainsKey(NpcStatesKey))
        {
            CloudNpcStateListData npcData = JsonUtility.FromJson<CloudNpcStateListData>(data[NpcStatesKey].Value);

            if (npcData != null && npcData.npcs != null)
            {
                List<NpcSaveEntity> npcs = ConvertCloudNpcStatesToEntities(npcData.npcs);
                HoteliaSQLiteManager.SaveNpcStates(npcs);
            }
        }

        Debug.Log("Cloud save downloaded and written to SQLite.");
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

    private static List<RoomRuntimeData> ConvertCloudRoomsToRuntimeRooms(List<CloudRoomData> cloudRooms)
    {
        List<RoomRuntimeData> rooms = new List<RoomRuntimeData>();

        if (cloudRooms == null)
            return rooms;

        foreach (CloudRoomData cloudRoom in cloudRooms)
        {
            if (cloudRoom == null)
                continue;

            RoomRuntimeData room = new RoomRuntimeData
            {
                roomId = cloudRoom.roomId,
                isAccessible = cloudRoom.isAccessible,
                bedType = (BedType)cloudRoom.bedType,
                bedCount = cloudRoom.bedCount,
                state = (RoomState)cloudRoom.state,
                needsCleaning = cloudRoom.needsCleaning,
                reservedUntilDay = cloudRoom.reservedUntilDay,
                currentGuestSegment = (GuestSegment)cloudRoom.currentGuestSegment,
                currentOffer = (OfferType)cloudRoom.currentOffer,
                currentMealPlan = (MealPlan)cloudRoom.currentMealPlan,
                currentGuestCount = cloudRoom.currentGuestCount,
                hasGuestData = cloudRoom.hasGuestData,
                hotelDoorSpawnId = cloudRoom.hotelDoorSpawnId,
                guestSpriteName = cloudRoom.guestSpriteName
            };

            rooms.Add(room);
        }

        return rooms;
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
                errors = result.errors,

                hasDetailedCheckInScores = result.hasDetailedCheckInScores,

                roomCorrect = result.roomCorrect,
                segmentCorrect = result.segmentCorrect,
                offerCorrect = result.offerCorrect,
                tourismExtraCorrect = result.tourismExtraCorrect,
                budgetCorrect = result.budgetCorrect,

                roomScore = result.roomScore,
                stpScore = result.stpScore,
                offerScore = result.offerScore,
                tourismExtraScore = result.tourismExtraScore,
                budgetScore = result.budgetScore
            };

            list.Add(data);
        }

        return list;
    }

    private static List<MiniGameResultData> ConvertCloudResultsToMiniGameResults(List<CloudDailyResultData> cloudResults)
    {
        List<MiniGameResultData> results = new List<MiniGameResultData>();

        if (cloudResults == null)
            return results;

        foreach (CloudDailyResultData cloudResult in cloudResults)
        {
            if (cloudResult == null)
                continue;

            MiniGameResultData result = new MiniGameResultData
            {
                day = cloudResult.day,
                minigameName = cloudResult.minigameName,
                finalScore = cloudResult.finalScore,
                revenue = cloudResult.revenue,
                errors = cloudResult.errors,

                hasDetailedCheckInScores = cloudResult.hasDetailedCheckInScores,

                roomCorrect = cloudResult.roomCorrect,
                segmentCorrect = cloudResult.segmentCorrect,
                offerCorrect = cloudResult.offerCorrect,
                tourismExtraCorrect = cloudResult.tourismExtraCorrect,
                budgetCorrect = cloudResult.budgetCorrect,

                roomScore = cloudResult.roomScore,
                stpScore = cloudResult.stpScore,
                offerScore = cloudResult.offerScore,
                tourismExtraScore = cloudResult.tourismExtraScore,
                budgetScore = cloudResult.budgetScore
            };

            results.Add(result);
        }

        return results;
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

    private static List<NpcSaveEntity> ConvertCloudNpcStatesToEntities(List<CloudNpcStateData> cloudNpcs)
    {
        List<NpcSaveEntity> npcs = new List<NpcSaveEntity>();

        if (cloudNpcs == null)
            return npcs;

        foreach (CloudNpcStateData cloudNpc in cloudNpcs)
        {
            if (cloudNpc == null)
                continue;

            NpcSaveEntity npc = new NpcSaveEntity
            {
                NpcId = cloudNpc.npcId,
                AssignedRoomId = cloudNpc.assignedRoomId,
                SceneName = cloudNpc.sceneName,
                Area = cloudNpc.area,
                PositionX = cloudNpc.positionX,
                PositionY = cloudNpc.positionY,
                PositionZ = cloudNpc.positionZ,
                HasValidPosition = cloudNpc.hasValidPosition,
                LastSeenTime = cloudNpc.lastSeenTime,
                NextDecisionTime = cloudNpc.nextDecisionTime
            };

            npcs.Add(npc);
        }

        return npcs;
    }
}