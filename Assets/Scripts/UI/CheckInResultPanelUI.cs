using TMPro;
using UnityEngine;

public class CheckInResultPanelUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text roomResultText;
    public TMP_Text segmentResultText;
    public TMP_Text offerResultText;
    public TMP_Text satisfactionText;
    public TMP_Text revenueText;
    public TMP_Text feedbackText;

    public void Show(bool segmentCorrect, bool offerCorrect, bool roomCorrect, int satisfaction, int revenue)
    {
        if (titleText != null)
            titleText.text = "Service Result";

        if (roomResultText != null)
            roomResultText.text = roomCorrect
                ? "Correct room"
                : "Incorrect room (-20 satisfaction)";

        if (segmentResultText != null)
            segmentResultText.text = segmentCorrect
                ? "Correct STP"
                : "Incorrect STP (-15 satisfaction)";

        if (offerResultText != null)
            offerResultText.text = offerCorrect
                ? "Correct offer"
                : "Incorrect offer (-15 satisfaction)";

        if (satisfactionText != null)
            satisfactionText.text = "Customer satisfaction: " + satisfaction + "%";

        if (revenueText != null)
            revenueText.text = "Revenue: $" + revenue;

        if (feedbackText != null)
        {
            if (roomCorrect && segmentCorrect && offerCorrect)
            {
                feedbackText.text = "Excellent. You served the guest correctly.";
            }
            else if (!roomCorrect)
            {
                feedbackText.text = "The room did not meet the guest's needs.";
            }
            else if (!segmentCorrect)
            {
                feedbackText.text = "The room was correct, but the STP segmentation was wrong.";
            }
            else if (!offerCorrect)
            {
                feedbackText.text = "The room and segment were correct, but the offer was not suitable.";
            }
        }

        gameObject.SetActive(true);
    }
}