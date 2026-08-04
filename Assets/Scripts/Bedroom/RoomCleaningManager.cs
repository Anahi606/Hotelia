using System;
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

        ConfigureTrashSeedForSelectedRoom();

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
            Debug.LogWarning(
                "[RoomCleaningManager] " +
                "No asignaste el RoomCleaningKPIManager."
            );
        }
    }

    private void ConfigureTrashSeedForSelectedRoom()
    {
        if (trashSpawner == null)
        {
            Debug.LogError(
                "[RoomCleaningManager] " +
                "TrashSpawner no está asignado."
            );

            return;
        }

        string selectedRoomId =
            Convert.ToString(
                RoomCleaningSession.selectedRoomId
            );

        if (string.IsNullOrWhiteSpace(selectedRoomId))
        {
            Debug.LogError(
                "[RoomCleaningManager] " +
                "RoomCleaningSession.selectedRoomId está vacío. " +
                "No se puede derivar una seed para la habitación."
            );

            return;
        }

        selectedRoomId =
            selectedRoomId.Trim();

        trashSpawner.SetRoomId(
            selectedRoomId
        );

        Debug.Log(
            "[RoomCleaningManager] " +
            "Procedural trash configured.\n" +
            "Room ID: " + selectedRoomId + "\n" +
            "Bed count: " +
            RoomCleaningSession.selectedBedCount
        );
    }

    private void ActivateCorrectLayout()
    {
        if (layout1Bed != null)
            layout1Bed.SetActive(false);

        if (layout2Beds != null)
            layout2Beds.SetActive(false);

        if (layout3Beds != null)
            layout3Beds.SetActive(false);

        int bedCount =
            RoomCleaningSession.selectedBedCount;

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
        {
            activeLayout.SetActive(true);
        }

        Debug.Log(
            "[RoomCleaningManager] " +
            "Se activó el layout para la habitación " +
            RoomCleaningSession.selectedRoomId +
            " con " +
            bedCount +
            " cama(s)."
        );
    }

    private BedSpot[] GetActiveLayoutBeds()
    {
        if (activeLayout == null)
        {
            Debug.LogWarning(
                "[RoomCleaningManager] " +
                "No hay un layout activo para buscar camas."
            );

            return Array.Empty<BedSpot>();
        }

        return activeLayout
            .GetComponentsInChildren<BedSpot>(true);
    }
}