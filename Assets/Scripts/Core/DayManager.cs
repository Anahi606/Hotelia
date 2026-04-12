using UnityEngine;
using TMPro;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    public int CurrentDay { get; private set; } = 1;

    [Header("References")]
    [SerializeField] private RoomData[] allRooms;
    [SerializeField] private TextMeshProUGUI dayText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void EndDay()
    {
        CurrentDay++;

        foreach (RoomData room in allRooms)
        {
            if (room == null) continue;

            if (room.state == RoomState.Ocupada && !room.IsReservationActive(CurrentDay))
            {
                room.state = RoomState.Sucia;
                room.needsCleaning = true;

                Debug.Log("La habitación " + room.roomId + " terminó su reserva y ahora está sucia.");
            }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (dayText != null)
            dayText.text = "Día " + CurrentDay;
    }
}