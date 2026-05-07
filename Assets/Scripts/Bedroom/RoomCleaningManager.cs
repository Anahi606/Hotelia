using UnityEngine;

public class RoomCleaningManager : MonoBehaviour
{
    [Header("Layouts")]
    [SerializeField] private GameObject layout1Bed;
    [SerializeField] private GameObject layout2Beds;
    [SerializeField] private GameObject layout3Beds;

    [Header("Trash")]
    [SerializeField] private TrashSpawner trashSpawner;

    [Header("KPI")]
    [SerializeField] private RoomCleaningKPIManager kpiManager;

    private GameObject activeLayout;

    private void Start()
    {
        ActivateCorrectLayout();

        BedSpot[] activeBeds = GetActiveLayoutBeds();

        if (kpiManager != null)
        {
            kpiManager.SetupCleaningRoom(
                RoomCleaningSession.selectedNeedsCleaning,
                trashSpawner,
                activeBeds
            );
        }
        else
        {
            Debug.LogWarning("No asignaste el RoomCleaningKPIManager en RoomCleaningManager.");
        }
    }

    private void ActivateCorrectLayout()
    {
        if (layout1Bed != null) layout1Bed.SetActive(false);
        if (layout2Beds != null) layout2Beds.SetActive(false);
        if (layout3Beds != null) layout3Beds.SetActive(false);

        int bedCount = RoomCleaningSession.selectedBedCount;

        if (bedCount <= 1)
        {
            activeLayout = layout1Bed;
        }
        else if (bedCount == 2)
        {
            activeLayout = layout2Beds;
        }
        else
        {
            activeLayout = layout3Beds;
        }

        if (activeLayout != null)
            activeLayout.SetActive(true);

        Debug.Log("Se activó layout para habitación " + RoomCleaningSession.selectedRoomId +
                  " con " + bedCount + " cama(s).");
    }

    private BedSpot[] GetActiveLayoutBeds()
    {
        if (activeLayout == null)
        {
            Debug.LogWarning("No hay layout activo para buscar camas.");
            return new BedSpot[0];
        }

        return activeLayout.GetComponentsInChildren<BedSpot>(true);
    }
}