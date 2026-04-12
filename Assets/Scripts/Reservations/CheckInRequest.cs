[System.Serializable]
public class CheckInRequest
{
    public bool needsAccessibleRoom;
    public BedType bedType;
    public int guestCount;
    public MealPlan mealPlan;
    public int stayDays;

    public string[] GetDialogueLines()
    {
        string accesible = needsAccessibleRoom
            ? "Necesito una habitación accesible."
            : "No necesito accesibilidad.";

        string camas = bedType == BedType.Matrimonial
            ? "Quiero una habitación matrimonial."
            : "Quiero una habitación con camas separadas.";

        string comidas = mealPlan == MealPlan.Completo
            ? "Deseo el plan completo con comidas."
            : "Solo alojamiento, por favor.";

        string dias = stayDays == 1
            ? "Me quedaré 1 día."
            : "Me quedaré " + stayDays + " días.";

        return new string[]
        {
            "Hola, vengo a hacer check-in.",
            accesible,
            camas,
            "Somos " + guestCount + " huésped" + (guestCount > 1 ? "es." : "."),
            comidas,
            dias,
            "Gracias."
        };
    }
}