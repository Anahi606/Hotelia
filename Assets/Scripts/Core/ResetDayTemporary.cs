using UnityEngine;

public class ResetDayTemporary : MonoBehaviour
{
    private void Start()
    {
        PlayerPrefs.SetInt("CurrentDay", 1);
        PlayerPrefs.Save();

        if (DayManager.Instance != null)
        {
            DayManager.Instance.ResetDays();
        }

        Debug.Log("Día reseteado a 1. Ya puedes borrar ResetDayTemporary.");
    }
}