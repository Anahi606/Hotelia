using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MissionJournalPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Mission List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private MissionJournalRowUI rowPrefab;

    [Header("Empty State")]
    [SerializeField] private TMP_Text emptyListText;

    [Header("Mission Detail Panel")]
    [SerializeField] private GameObject missionTaskDescriptionPanel;
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailDescriptionText;

    private readonly List<MissionJournalRowUI> spawnedRows = new List<MissionJournalRowUI>();

    private bool hasPauseRequest;

    public bool IsOpen
    {
        get
        {
            return panel != null && panel.activeSelf;
        }
    }

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (missionTaskDescriptionPanel != null)
            missionTaskDescriptionPanel.SetActive(false);

        HideEmptyList();
        ClearDetail();
    }

    private void OnDestroy()
    {
        ReleasePauseIfNeeded();
    }

    private void OnDisable()
    {
        ReleasePauseIfNeeded();
    }

    public void ToggleJournal()
    {
        if (IsOpen)
            CloseJournal();
        else
            OpenJournal();
    }

    public void OpenJournal()
    {
        Debug.Log("OpenJournal called");

        if (panel == null)
        {
            Debug.LogError("Panel is not assigned in MissionJournalPanelUI.");
            return;
        }

        panel.SetActive(true);

        RequestPauseIfNeeded();

        if (missionTaskDescriptionPanel != null)
            missionTaskDescriptionPanel.SetActive(false);

        RefreshJournal();
    }

    public void CloseJournal()
    {
        if (panel == null)
            return;

        panel.SetActive(false);

        if (missionTaskDescriptionPanel != null)
            missionTaskDescriptionPanel.SetActive(false);

        ReleasePauseIfNeeded();
    }

    public void RefreshJournal()
    {
        Debug.Log("RefreshJournal called");

        ClearRows();

        List<HotelMissionData> missions = HotelMissionTracker.BuildMissions();

        if (missions.Count == 0)
        {
            ShowEmptyList();
            ClearDetail();

            if (missionTaskDescriptionPanel != null)
                missionTaskDescriptionPanel.SetActive(false);

            return;
        }

        HideEmptyList();
        ClearDetail();

        if (missionTaskDescriptionPanel != null)
            missionTaskDescriptionPanel.SetActive(false);

        foreach (HotelMissionData mission in missions)
        {
            MissionJournalRowUI row = Instantiate(rowPrefab, listContent);
            row.gameObject.SetActive(true);
            row.Setup(mission, this);
            spawnedRows.Add(row);
        }
    }

    public void SelectMission(HotelMissionData mission)
    {
        Debug.Log("SelectMission called");

        if (mission == null)
        {
            Debug.LogWarning("Mission is null.");
            ClearDetail();

            if (missionTaskDescriptionPanel != null)
                missionTaskDescriptionPanel.SetActive(false);

            return;
        }

        if (panel != null)
            panel.SetActive(true);

        RequestPauseIfNeeded();

        if (missionTaskDescriptionPanel != null)
        {
            missionTaskDescriptionPanel.SetActive(true);
            Debug.Log("MissionTaskDescription activated");
        }
        else
        {
            Debug.LogError("MissionTaskDescriptionPanel is not assigned.");
        }

        if (detailTitleText != null)
            detailTitleText.text = mission.title;

        if (detailDescriptionText != null)
            detailDescriptionText.text = mission.description;
    }

    private void RequestPauseIfNeeded()
    {
        if (hasPauseRequest)
            return;

        HotelGamePause.RequestPause();
        hasPauseRequest = true;
    }

    private void ReleasePauseIfNeeded()
    {
        if (!hasPauseRequest)
            return;

        HotelGamePause.ReleasePause();
        hasPauseRequest = false;
    }

    private void ClearRows()
    {
        foreach (MissionJournalRowUI row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedRows.Clear();

        if (listContent == null)
            return;

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }
    }

    private void ShowEmptyList()
    {
        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(true);
            emptyListText.text = "No pending tasks.";
        }
    }

    private void HideEmptyList()
    {
        if (emptyListText != null)
            emptyListText.gameObject.SetActive(false);
    }

    private void ClearDetail()
    {
        if (detailTitleText != null)
            detailTitleText.text = "";

        if (detailDescriptionText != null)
            detailDescriptionText.text = "";
    }
}