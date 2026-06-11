using TMPro;
using UnityEngine;

public class RestaurantResultPanelUI : MonoBehaviour
{
    public TMP_Text resultTitleText;
    public TMP_Text satisfactionText;
    public TMP_Text timeText;
    public TMP_Text errorsText;
    public TMP_Text revenueText;
    public TMP_Text feedbackText;

    public void Show(int satisfaction, int timeScore, int errors, int revenue)
    {
        if (resultTitleText != null)
            resultTitleText.text = "Restaurant Summary";

        if (satisfactionText != null)
            satisfactionText.text = "Satisfaction: " + satisfaction + "%";

        if (timeText != null)
            timeText.text = "Service time: " + timeScore + "%";

        if (errorsText != null)
            errorsText.text = "Errors: " + errors;

        if (revenueText != null)
            revenueText.text = "Revenue: $" + revenue;

        if (feedbackText != null)
        {
            if (errors == 0)
                feedbackText.text = "Excellent. You prioritized the orders correctly.";
            else if (errors == 1)
                feedbackText.text = "Good job, but review urgency and allergies more carefully.";
            else
                feedbackText.text = "You need to prioritize allergies, urgent orders, and room service.";
        }

        gameObject.SetActive(true);
    }
}