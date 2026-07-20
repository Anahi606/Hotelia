using UnityEngine;

public static class HotelRuntimeProfile
{
    public static void ReloadFromActiveSQLite()
    {
        HotelSaveData save = HotelSaveSystem.LoadGame();

        ApplySave(save);
    }

    public static void ApplySave(HotelSaveData save)
    {
        ClearRuntimeData();

        if (save == null || !save.hasStartedGame)
        {
            Debug.Log(
                "[HotelRuntimeProfile] El perfil activo no tiene partida."
            );

            return;
        }

        if (HotelGameData.Instance != null)
        {
            HotelGameData.Instance.selectedCharacter =
                save.selectedCharacter;

            HotelGameData.Instance.rooms.Clear();

            if (save.rooms != null)
            {
                HotelGameData.Instance.rooms.AddRange(
                    save.rooms
                );
            }
        }

        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.ClearTodayResults();
            DailyResultsManager.Instance.allResults.Clear();

            if (save.allResults != null)
            {
                DailyResultsManager.Instance.allResults.AddRange(
                    save.allResults
                );
            }
        }

        if (DayManager.Instance != null)
        {
            DayManager.Instance.LoadDayFromSave(
                save.currentDay
            );
        }

        Debug.Log(
            "[HotelRuntimeProfile] Partida aplicada en memoria." +
            "\nPerfil SQLite: " +
            HoteliaSQLiteManager.ActiveProfileId +
            "\nDía: " +
            save.currentDay +
            "\nResultados: " +
            (save.allResults != null
                ? save.allResults.Count
                : 0)
        );
    }

    public static void ClearRuntimeData()
    {
        if (HotelGameData.Instance != null)
        {
            HotelGameData.Instance.selectedCharacter =
                PlayerCharacterType.None;

            HotelGameData.Instance.rooms.Clear();
        }

        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.ClearTodayResults();
            DailyResultsManager.Instance.allResults.Clear();
        }

        if (DayManager.Instance != null)
        {
            DayManager.Instance.LoadDayFromSave(1);
        }

        Debug.Log(
            "[HotelRuntimeProfile] Datos anteriores eliminados de memoria."
        );
    }
}