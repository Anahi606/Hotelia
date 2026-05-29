using UnityEngine;
using UnityEngine.SceneManagement;

public static class HotelSaveSystem
{
    private const string SaveKey = "Hotelia_SaveData";
    private const string SavedLevelKey = "SavedLevel";
    private const string OldDayKey = "CurrentDay";
    private const string DailyResultsKey = "Hotelia_DailyResults";

    public static bool HasSave()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return false;

        string json = PlayerPrefs.GetString(SaveKey);

        if (string.IsNullOrEmpty(json))
            return false;

        HotelSaveData data = JsonUtility.FromJson<HotelSaveData>(json);

        return data != null && data.hasStartedGame;
    }

    public static void SaveNewCharacter(PlayerCharacterType characterType, string firstGameSceneName)
    {
        HotelSaveData data = new HotelSaveData();

        data.hasStartedGame = true;
        data.selectedCharacter = characterType;
        data.currentDay = 1;
        data.savedSceneName = firstGameSceneName;

        if (HotelGameData.Instance != null)
        {
            HotelGameData.Instance.selectedCharacter = characterType;
            data.rooms = HotelGameData.Instance.rooms;
        }

        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.ClearTodayResults();
            DailyResultsManager.Instance.allResults.Clear();

            data.allResults = DailyResultsManager.Instance.allResults;
        }

        string json = JsonUtility.ToJson(data, true);

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.SetString(SavedLevelKey, firstGameSceneName);
        PlayerPrefs.Save();

        Debug.Log("Nuevo personaje guardado:\n" + json);
    }

    public static void SaveEndOfDay()
    {
        HotelSaveData data = new HotelSaveData();

        data.hasStartedGame = true;

        if (SceneManager.GetActiveScene() != null)
            data.savedSceneName = SceneManager.GetActiveScene().name;
        else
            data.savedSceneName = "02 - Hotel";

        if (DayManager.Instance != null)
            data.currentDay = DayManager.Instance.CurrentDay;
        else
            data.currentDay = 1;

        if (HotelGameData.Instance != null)
        {
            data.selectedCharacter = HotelGameData.Instance.selectedCharacter;
            data.rooms = HotelGameData.Instance.rooms;
        }

        if (DailyResultsManager.Instance != null)
        {
            data.allResults = DailyResultsManager.Instance.allResults;
        }

        string json = JsonUtility.ToJson(data, true);

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.SetString(SavedLevelKey, data.savedSceneName);
        PlayerPrefs.Save();

        Debug.Log("Partida guardada al final del día:\n" + json);
    }

    public static HotelSaveData LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return null;

        string json = PlayerPrefs.GetString(SaveKey);

        if (string.IsNullOrEmpty(json))
            return null;

        HotelSaveData data = JsonUtility.FromJson<HotelSaveData>(json);

        return data;
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(SavedLevelKey);
        PlayerPrefs.DeleteKey(OldDayKey);
        PlayerPrefs.DeleteKey(DailyResultsKey);

        PlayerPrefs.Save();

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

        Debug.Log("Partida eliminada completamente.");
    }
}