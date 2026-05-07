using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class DayTextUI : MonoBehaviour
{
    private TextMeshProUGUI dayText;

    private static readonly List<DayTextUI> allDayTexts = new List<DayTextUI>();

    private void Awake()
    {
        dayText = GetComponent<TextMeshProUGUI>();

        if (!allDayTexts.Contains(this))
            allDayTexts.Add(this);
    }

    private void Start()
    {
        UpdateDayText();
    }

    private void OnDestroy()
    {
        if (allDayTexts.Contains(this))
            allDayTexts.Remove(this);
    }

    public void UpdateDayText()
    {
        if (dayText == null) return;
        if (DayManager.Instance == null) return;

        dayText.text = "Día " + DayManager.Instance.CurrentDay;
    }

    public static void UpdateAllDayTexts()
    {
        foreach (DayTextUI textUI in allDayTexts)
        {
            if (textUI != null)
                textUI.UpdateDayText();
        }
    }
}