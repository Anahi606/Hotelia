using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HotelSaveSystem
{
    public static bool HasSave()
    {
        return HoteliaSQLiteManager.HasGameState();
    }

    public static void SaveNewCharacter(PlayerCharacterType characterType, string firstGameSceneName)
    {
        GameStateEntity gameState = new GameStateEntity
        {
            Id = 1,
            HasStartedGame = true,
            SelectedCharacter = (int)characterType,
            CurrentDay = 1,
            SavedSceneName = firstGameSceneName
        };

        HoteliaSQLiteManager.SaveGameState(gameState);

        if (HotelGameData.Instance != null)
        {
            HotelGameData.Instance.selectedCharacter = characterType;
            HoteliaSQLiteManager.SaveRooms(HotelGameData.Instance.rooms);
        }

        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.ClearTodayResults();
            DailyResultsManager.Instance.allResults.Clear();
        }

        Debug.Log("Nuevo personaje guardado en SQLite: " + characterType);
    }

    public static void SaveEndOfDay()
    {
        string sceneName = "02 - Hotel";

        if (SceneManager.GetActiveScene() != null)
            sceneName = SceneManager.GetActiveScene().name;

        int currentDay = 1;
        PlayerCharacterType selectedCharacter = PlayerCharacterType.None;

        if (DayManager.Instance != null)
            currentDay = DayManager.Instance.CurrentDay;

        if (HotelGameData.Instance != null)
            selectedCharacter = HotelGameData.Instance.selectedCharacter;

        GameStateEntity gameState = new GameStateEntity
        {
            Id = 1,
            HasStartedGame = true,
            SavedSceneName = sceneName,
            CurrentDay = currentDay,
            SelectedCharacter = (int)selectedCharacter
        };

        HoteliaSQLiteManager.SaveGameState(gameState);

        if (HotelGameData.Instance != null)
        {
            HoteliaSQLiteManager.SaveRooms(HotelGameData.Instance.rooms);
        }

        if (DailyResultsManager.Instance != null)
        {
            HoteliaSQLiteManager.SaveDailyResults(DailyResultsManager.Instance.todayResults);
        }

        HoteliaSQLiteManager.SaveVisibleNpcStates();

        Debug.Log("Partida guardada al final del día en SQLite.");
    }

    public static HotelSaveData LoadGame()
    {
        GameStateEntity gameState = HoteliaSQLiteManager.LoadGameState();

        if (gameState == null || !gameState.HasStartedGame)
            return null;

        HotelSaveData data = new HotelSaveData();

        data.hasStartedGame = gameState.HasStartedGame;
        data.savedSceneName = gameState.SavedSceneName;
        data.currentDay = gameState.CurrentDay;
        data.selectedCharacter = (PlayerCharacterType)gameState.SelectedCharacter;

        List<RoomRuntimeData> rooms = HoteliaSQLiteManager.LoadRooms();

        if (rooms != null)
            data.rooms = rooms;

        List<MiniGameResultData> results = HoteliaSQLiteManager.LoadDailyResults();

        if (results != null)
            data.allResults = results;

        return data;
    }

    public static void DeleteSave()
    {
        HoteliaSQLiteManager.DeleteAllSaveData();

        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.ClearTodayResults();
            DailyResultsManager.Instance.allResults.Clear();
        }

        if (HotelGameData.Instance != null)
        {
            HotelGameData.Instance.selectedCharacter = PlayerCharacterType.None;
            HotelGameData.Instance.rooms.Clear();
        }

        Debug.Log("Partida eliminada completamente desde SQLite.");
    }
}