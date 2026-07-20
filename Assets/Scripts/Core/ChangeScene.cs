using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    [Header("Scene")]
    public int sceneBuildIndex;

    [Header("Player Spawn In Destination")]
    public string destinationPlayerSpawnId;

    [Header("Dynamic Room Door Spawn")]
    public bool useSelectedRoomDoorSpawn;
    public string roomDoorSpawnPrefix = "RoomDoor_";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        GuestNPCSceneSaver.SaveAllVisibleGuests();

        string spawnId = destinationPlayerSpawnId;

        if (useSelectedRoomDoorSpawn)
        {
            if (string.IsNullOrEmpty(RoomCleaningSession.selectedRoomId))
            {
                Debug.LogWarning("No hay habitación seleccionada para calcular el spawn del player.");
            }
            else
            {
                spawnId = roomDoorSpawnPrefix + RoomCleaningSession.selectedRoomId;
            }
        }

        if (!string.IsNullOrEmpty(spawnId))
        {
            PlayerSpawnMemory.SetNextSpawn(spawnId);
        }
        else
        {
            PlayerSpawnMemory.Clear();
        }

        SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Single);
    }
}