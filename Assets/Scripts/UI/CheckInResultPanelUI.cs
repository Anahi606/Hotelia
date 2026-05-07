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
            titleText.text = "Resultado de la atención";

        if (roomResultText != null)
            roomResultText.text = roomCorrect
                ? "Habitación correcta"
                : "Habitación incorrecta (-20 satisfacción)";

        if (segmentResultText != null)
            segmentResultText.text = segmentCorrect
                ? "STP correcto"
                : "STP incorrecto (-15 satisfacción)";

        if (offerResultText != null)
            offerResultText.text = offerCorrect
                ? "Oferta correcta"
                : "Oferta incorrecta (-15 satisfacción)";

        if (satisfactionText != null)
            satisfactionText.text = "Satisfacción del cliente: " + satisfaction + "%";

        if (revenueText != null)
            revenueText.text = "Ingreso: $" + revenue;

        if (feedbackText != null)
        {
            if (roomCorrect && segmentCorrect && offerCorrect)
            {
                feedbackText.text = "Excelente. Atendiste correctamente al huésped.";
            }
            else if (!roomCorrect)
            {
                feedbackText.text = "La habitación no cumplía con las necesidades del huésped.";
            }
            else if (!segmentCorrect)
            {
                feedbackText.text = "La habitación estuvo bien, pero falló la segmentación STP.";
            }
            else if (!offerCorrect)
            {
                feedbackText.text = "La habitación y el segmento estuvieron bien, pero la oferta no fue la adecuada.";
            }
        }

        gameObject.SetActive(true);
    }
}