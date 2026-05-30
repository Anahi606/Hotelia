using System.Collections.Generic;
using UnityEngine;

public class DailyResultsManager : MonoBehaviour
{
    public static DailyResultsManager Instance { get; private set; }

    [Header("Current Day Results - TEMPORAL")]
    public List<MiniGameResultData> todayResults = new List<MiniGameResultData>();

    [Header("Saved History - ONLY FINISHED DAYS")]
    public List<MiniGameResultData> allResults = new List<MiniGameResultData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSavedResults();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterResult(MiniGameResultData result)
    {
        if (result == null) return;

        todayResults.Add(result);

        Debug.Log("Resultado temporal registrado: " + result.minigameName + " Día " + result.day);
    }

    public List<MiniGameResultData> GetTodayResults()
    {
        return todayResults;
    }

    public List<MiniGameResultData> GetSavedHistory()
    {
        return allResults;
    }

    public void CommitTodayResults()
    {
        if (todayResults == null || todayResults.Count == 0)
        {
            Debug.Log("No hay resultados del día para guardar.");
            return;
        }

        foreach (MiniGameResultData result in todayResults)
        {
            if (result != null)
                allResults.Add(result);
        }

        Debug.Log("Resultados del día enviados al historial en memoria: " + todayResults.Count);
    }

    public void ClearTodayResults()
    {
        todayResults.Clear();
    }

    private void LoadSavedResults()
    {
        allResults.Clear();

        List<MiniGameResultData> savedResults = HoteliaSQLiteManager.LoadDailyResults();

        if (savedResults != null)
            allResults = savedResults;

        Debug.Log("Resultados cargados desde SQLite: " + allResults.Count);
    }

    public void ClearAllResultsInMemory()
    {
        todayResults.Clear();
        allResults.Clear();

        Debug.Log("Resultados diarios limpiados en memoria.");
    }

    public static void DeleteSavedResults()
    {
        if (Instance != null)
        {
            Instance.ClearAllResultsInMemory();
        }

        Debug.Log("Historial de resultados eliminado de memoria.");
    }

    [ContextMenu("Reset Saved Results")]
    public void ResetSavedResults()
    {
        todayResults.Clear();
        allResults.Clear();

        Debug.Log("Historial de resultados eliminado de memoria.");
    }
}