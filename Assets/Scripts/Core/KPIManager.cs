using UnityEngine;

public class KPIManager : MonoBehaviour
{
    public static KPIManager Instance { get; private set; }

    [Header("KPIs")]
    public int totalClients;
    public int correctSegments;
    public int correctOffers;
    public int totalRevenue;
    public int totalSatisfaction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterCheckInResult(bool segmentOk, bool offerOk, int satisfaction, int revenue)
    {
        totalClients++;

        if (segmentOk)
            correctSegments++;

        if (offerOk)
            correctOffers++;

        totalRevenue += revenue;
        totalSatisfaction += satisfaction;

        Debug.Log("KPIs actualizados.");
        Debug.Log("Clientes totales: " + totalClients);
        Debug.Log("Segmentaciones correctas: " + correctSegments);
        Debug.Log("Ofertas correctas: " + correctOffers);
        Debug.Log("Revenue total: " + totalRevenue);
        Debug.Log("Satisfacción total: " + totalSatisfaction);
    }

    public float GetSegmentAccuracy()
    {
        if (totalClients == 0) return 0f;
        return (float)correctSegments / totalClients * 100f;
    }

    public float GetOfferAccuracy()
    {
        if (totalClients == 0) return 0f;
        return (float)correctOffers / totalClients * 100f;
    }

    public float GetAverageSatisfaction()
    {
        if (totalClients == 0) return 0f;
        return (float)totalSatisfaction / totalClients;
    }

    public float GetAverageRevenue()
    {
        if (totalClients == 0) return 0f;
        return (float)totalRevenue / totalClients;
    }
}