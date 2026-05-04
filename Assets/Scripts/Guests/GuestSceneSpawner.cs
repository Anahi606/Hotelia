using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GuestSceneSpawner : MonoBehaviour
{
    [Header("Current Scene Area")]
    public GuestArea currentArea;

    [Header("NPC Parent")]
    public Transform npcParent;

    [Header("Guest Prefab")]
    public GameObject defaultGuestPrefab;

    [Header("Scale")]
    [SerializeField] private Transform playerReference;
    [SerializeField] private bool matchPlayerScale = true;
    [SerializeField] private Vector3 manualNpcScale = Vector3.one;

    private Dictionary<string, GuestSpawnPoint> spawnPoints =
        new Dictionary<string, GuestSpawnPoint>();

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        if (playerReference == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerReference = player.transform;
        }

        RegisterSpawnPoints();
        SpawnGuests();

        GuestOffscreenSimulator simulator =
            FindFirstObjectByType<GuestOffscreenSimulator>();

        if (simulator != null)
        {
            simulator.RegisterSpawner(this);
        }
    }

    private void RegisterSpawnPoints()
    {
        spawnPoints.Clear();

        GuestSpawnPoint[] points =
            FindObjectsByType<GuestSpawnPoint>(FindObjectsSortMode.None);

        foreach (GuestSpawnPoint point in points)
        {
            if (point.area != currentArea)
                continue;

            if (!spawnPoints.ContainsKey(point.spawnId))
            {
                spawnPoints.Add(point.spawnId, point);
            }
        }
    }

    private void SpawnGuests()
    {
        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData en esta escena.");
            return;
        }

        if (currentArea == GuestArea.Room)
        {
            SpawnGuestForSelectedRoom();
            return;
        }

        SpawnGuestsForGeneralArea();
    }

    private void SpawnGuestForSelectedRoom()
    {
        string selectedRoomId = RoomCleaningSession.selectedRoomId;

        if (string.IsNullOrEmpty(selectedRoomId))
        {
            Debug.LogWarning("No hay selectedRoomId. No se spawnea guest.");
            return;
        }

        RoomRuntimeData room = HotelGameData.Instance.GetRoomById(selectedRoomId);

        if (room == null)
        {
            Debug.LogWarning("No existe habitación runtime con ID: " + selectedRoomId);
            return;
        }

        bool hasActiveGuest =
            room.state == RoomState.Ocupada &&
            room.hasGuestData &&
            room.currentGuestCount > 0;

        if (!hasActiveGuest)
        {
            Debug.Log("La habitación " + selectedRoomId + " NO tiene huésped. No se spawnea customer.");
            return;
        }

        SpawnGuestFromRoom(room);
    }

    private void SpawnGuestsForGeneralArea()
    {
        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (room == null)
                continue;

            if (room.state != RoomState.Ocupada || !room.hasGuestData)
                continue;

            SpawnGuestFromRoom(room);
        }
    }

    private void SpawnGuestFromRoom(RoomRuntimeData room)
    {
        string npcId = "Guest_" + room.roomId;
        string currentSceneName = SceneManager.GetActiveScene().name;

        bool hasSavedState =
            GuestNPCMemory.TryGetState(npcId, out GuestNPCState savedState);

        if (hasSavedState && savedState.sceneName != currentSceneName)
        {
            Debug.Log("El NPC " + npcId + " está guardado en otra escena: " + savedState.sceneName);
            return;
        }

        if (defaultGuestPrefab == null)
        {
            Debug.LogWarning("No has asignado Default Guest Prefab.");
            return;
        }
        Vector3 spawnPosition = Vector3.zero;
        bool hasSpawnPosition = false;

        if (hasSavedState && savedState.hasValidPosition)
        {
            spawnPosition = savedState.position;
            hasSpawnPosition = true;
        }

        if (!hasSpawnPosition &&
            hasSavedState &&
            !string.IsNullOrEmpty(savedState.destinationSpawnId))
        {
            if (spawnPoints.ContainsKey(savedState.destinationSpawnId))
            {
                spawnPosition = spawnPoints[savedState.destinationSpawnId].transform.position;
                hasSpawnPosition = true;
            }
            else
            {
                Debug.LogWarning(
                    "No existe GuestSpawnPoint con destinationSpawnId: " +
                    savedState.destinationSpawnId
                );
            }
        }

        if (!hasSpawnPosition && currentArea == GuestArea.Hotel)
        {
            if (RoomDoorRegistry.Instance != null &&
                RoomDoorRegistry.Instance.TryGetDoorSpawnPosition(room.roomId, out Vector3 doorPosition))
            {
                spawnPosition = doorPosition;
                hasSpawnPosition = true;
            }
        }

        if (!hasSpawnPosition)
        {
            string spawnId = GetSpawnIdForRoom(room);

            if (!spawnPoints.ContainsKey(spawnId))
            {
                Debug.LogWarning("No existe spawn point con id: " + spawnId);
                return;
            }

            spawnPosition = spawnPoints[spawnId].transform.position;
            hasSpawnPosition = true;
        }

        GameObject guest = Instantiate(
            defaultGuestPrefab,
            spawnPosition,
            Quaternion.identity,
            npcParent
        );

        ApplyNpcScale(guest);

        RandomGridRoamer roamer = guest.GetComponent<RandomGridRoamer>();

        if (roamer != null)
        {
            roamer.Initialize(currentArea);
        }

        GuestNPC guestNPC = guest.GetComponent<GuestNPC>();

        if (guestNPC != null)
        {
            guestNPC.Initialize(room.roomId, currentArea);
        }
    }

    private void ApplyNpcScale(GameObject guest)
    {
        if (guest == null)
            return;

        if (matchPlayerScale && playerReference != null)
        {
            guest.transform.localScale = playerReference.localScale;
        }
        else
        {
            guest.transform.localScale = manualNpcScale;
        }
    }

    private string GetSpawnIdForRoom(RoomRuntimeData room)
    {
        if (currentArea == GuestArea.Room)
        {
            return "RoomInside";
        }

        if (currentArea == GuestArea.Hotel)
        {
            return "GuestSpawn_Hotel";
        }

        if (currentArea == GuestArea.Restaurant)
        {
            return "RestaurantEntrance";
        }

        if (currentArea == GuestArea.Outside)
        {
            return "OutsideEntrance";
        }

        return "";
    }

    public void RefreshGuests()
    {
        ClearCurrentGuests();
        RegisterSpawnPoints();
        SpawnGuests();
    }

    private void ClearCurrentGuests()
    {
        if (npcParent == null)
            return;

        for (int i = npcParent.childCount - 1; i >= 0; i--)
        {
            Destroy(npcParent.GetChild(i).gameObject);
        }
    }
}