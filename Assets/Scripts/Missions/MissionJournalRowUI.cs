using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionJournalRowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;

    private HotelMissionData mission;
    private MissionJournalPanelUI journalPanel;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
            button = GetComponentInChildren<Button>();
    }

    public void Setup(HotelMissionData missionData, MissionJournalPanelUI owner)
    {
        mission = missionData;
        journalPanel = owner;

        if (titleText != null)
            titleText.text = mission.title;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogError("MissionJournalRowUI has no Button assigned: " + gameObject.name);
        }
    }

    private void OnClick()
    {
        Debug.Log("Mission row clicked: " + gameObject.name);

        if (journalPanel != null)
            journalPanel.SelectMission(mission);
        else
            Debug.LogError("JournalPanel is null in row: " + gameObject.name);
    }
}