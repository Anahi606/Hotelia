using TMPro;
using UnityEngine;

public class CheckInResultPanelUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text segmentResultText;
    public TMP_Text offerResultText;
    public TMP_Text satisfactionText;
    public TMP_Text revenueText;

    public void Show(bool segmentCorrect, bool offerCorrect, int satisfaction, int revenue)
    {
        if (titleText != null)
            titleText.text = "Resultado de la atención";

        if (segmentResultText != null)
            segmentResultText.text = segmentCorrect
                ? "Segmentación correcta"
                : "Segmentación incorrecta";

        if (offerResultText != null)
            offerResultText.text = offerCorrect
                ? "Oferta correcta"
                : "Oferta incorrecta";

        if (satisfactionText != null)
            satisfactionText.text = "Satisfacción: " + satisfaction;

        if (revenueText != null)
            revenueText.text = "Ingreso: $" + revenue;

        gameObject.SetActive(true);
    }
}