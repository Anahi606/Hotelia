using UnityEngine;

public class RoomCleaningManager : MonoBehaviour
{
    [Header("Layouts")]
    [SerializeField] private GameObject layout1Bed;
    [SerializeField] private GameObject layout2Beds;
    [SerializeField] private GameObject layout3Beds;

    [Header("Trash")]
    [SerializeField] private TrashSpawner trashSpawner;

    private void Start()
    {
        ActivateCorrectLayout();

        if (RoomCleaningSession.selectedNeedsCleaning && trashSpawner != null)
        {
            trashSpawner.SpawnTrash();
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
            if (layout1Bed != null) layout1Bed.SetActive(true);
        }
        else if (bedCount == 2)
        {
            if (layout2Beds != null) layout2Beds.SetActive(true);
        }
        else
        {
            if (layout3Beds != null) layout3Beds.SetActive(true);
        }

        Debug.Log("Se activó layout para habitación " + RoomCleaningSession.selectedRoomId +
                  " con " + bedCount + " cama(s).");
    }
}