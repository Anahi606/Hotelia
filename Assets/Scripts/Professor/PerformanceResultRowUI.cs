using TMPro;
using UnityEngine;

public class PerformanceResultRowUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text minigameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text revenueText;
    [SerializeField] private TMP_Text errorsText;

    public void Setup(CloudDailyResultData result)
    {
        if (result == null)
            return;

        if (dayText != null)
            dayText.text = "Day " + result.day;

        if (minigameText != null)
            minigameText.text = result.minigameName;

        if (scoreText != null)
            scoreText.text = "Score: " + result.finalScore + "%";

        if (revenueText != null)
            revenueText.text = "Revenue: $" + result.revenue;

        if (errorsText != null)
            errorsText.text = "Errors: " + result.errors;
    }
}