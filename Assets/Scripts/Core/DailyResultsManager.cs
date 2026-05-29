using System.Collections.Generic;
using UnityEngine;

public class DailyResultsManager : MonoBehaviour
{
    public static DailyResultsManager Instance { get; private set; }

    [Header("Current Day Results - TEMPORAL")]
    public List<MiniGameResultData> todayResults = new List<MiniGameResultData>();

    [Header("Saved History - ONLY FINISHED DAYS")]
    public List<MiniGameResultData> allResults = new List<MiniGameResultData>();

    private const string ResultsKey = "Hotelia_DailyResults";

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

        SaveHistory();

        Debug.Log("Día guardado. Resultados guardados: " + todayResults.Count);
    }

    public void ClearTodayResults()
    {
        todayResults.Clear();
    }

    private void SaveHistory()
    {
        MiniGameResultDataList wrapper = new MiniGameResultDataList();
        wrapper.results = allResults;

        string json = JsonUtility.ToJson(wrapper, true);

        PlayerPrefs.SetString(ResultsKey, json);
        PlayerPrefs.Save();
    }

    private void LoadSavedResults()
    {
        allResults.Clear();

        if (!PlayerPrefs.HasKey(ResultsKey))
            return;

        string json = PlayerPrefs.GetString(ResultsKey);
        MiniGameResultDataList wrapper = JsonUtility.FromJson<MiniGameResultDataList>(json);

        if (wrapper != null && wrapper.results != null)
            allResults = wrapper.results;
    }

    public void ClearAllResultsInMemory()
    {
        todayResults.Clear();
        allResults.Clear();

        Debug.Log("Resultados diarios limpiados en memoria.");
    }

    public static void DeleteSavedResults()
    {
        PlayerPrefs.DeleteKey(ResultsKey);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.ClearAllResultsInMemory();
        }

        Debug.Log("Historial de resultados eliminado completamente.");
    }

    [ContextMenu("Reset Saved Results")]
    public void ResetSavedResults()
    {
        todayResults.Clear();
        allResults.Clear();

        PlayerPrefs.DeleteKey(ResultsKey);
        PlayerPrefs.Save();

        Debug.Log("Historial de resultados eliminado.");
    }
}

[System.Serializable]
public class MiniGameResultDataList
{
    public List<MiniGameResultData> results = new List<MiniGameResultData>();
}