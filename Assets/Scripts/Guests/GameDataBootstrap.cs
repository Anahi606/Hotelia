using UnityEngine;

public class GameDataBootstrap : MonoBehaviour
{
    [SerializeField] private HotelGameData hotelGameDataPrefab;

    private void Awake()
    {
        if (HotelGameData.Instance == null)
        {
            Instantiate(hotelGameDataPrefab);
        }
    }
}