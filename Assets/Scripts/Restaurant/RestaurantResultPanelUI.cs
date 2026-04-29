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
            resultTitleText.text = "Resumen del restaurante";

        if (satisfactionText != null)
            satisfactionText.text = "Satisfacción: " + satisfaction + "%";

        if (timeText != null)
            timeText.text = "Tiempo de atención: " + timeScore + "%";

        if (errorsText != null)
            errorsText.text = "Errores: " + errors;

        if (revenueText != null)
            revenueText.text = "Ingresos: $" + revenue;

        if (feedbackText != null)
        {
            if (errors == 0)
                feedbackText.text = "Excelente. Priorizaste correctamente los pedidos.";
            else if (errors == 1)
                feedbackText.text = "Bien, pero revisa mejor las urgencias y alergias.";
            else
                feedbackText.text = "Debes priorizar alergias, urgencias y room service.";
        }

        gameObject.SetActive(true);
    }
}