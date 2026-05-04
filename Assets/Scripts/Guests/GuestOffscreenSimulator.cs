using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GuestOffscreenSimulator : MonoBehaviour
{
    [Header("Scene Names")]
    public string bedroomSceneName = "03 - Bedroom";
    public string hotelSceneName = "02 - Hotel";
    public string outsideSceneName = "04 - Outside";
    public string restaurantSceneName = "05 - Restaurant";

    [Header("Destination Spawn Ids")]
    public string outsideHotelEntranceSpawnId = "Outside_HotelEntrance";
    public string outsideRestaurantEntranceSpawnId = "Outside_RestaurantEntrance";
    public string restaurantEntranceSpawnId = "RestaurantEntrance";
    public string hotelEntranceSpawnId = "HotelEntrance";
    public string roomInsideSpawnId = "RoomInside";

    [Header("Timing")]
    public float minSecondsBeforeDecision = 15f;
    public float minNextDecisionDelay = 20f;
    public float maxNextDecisionDelay = 60f;
    public float simulationInterval = 5f;

    [Header("Simulation Limits")]
    public int maxGuestsMovedPerCycle = 1;

    [Header("Chances")]
    [Range(0f, 1f)] public float chanceBedroomToHotel = 0.25f;
    [Range(0f, 1f)] public float chanceHotelToOutside = 0.15f;
    [Range(0f, 1f)] public float chanceHotelToBedroom = 0.20f;
    [Range(0f, 1f)] public float chanceOutsideToRestaurant = 0.20f;
    [Range(0f, 1f)] public float chanceOutsideToHotel = 0.25f;
    [Range(0f, 1f)] public float chanceRestaurantToOutside = 0.30f;

    [Header("Fallback Spawn Positions")]
    public Vector3 hotelFallbackPosition;
    public Vector3 outsideEntrancePosition;
    public Vector3 restaurantEntrancePosition;

    private GuestSceneSpawner currentSpawner;

    private void Start()
    {
        StartCoroutine(SimulationLoop());
    }

    public void RegisterSpawner(GuestSceneSpawner spawner)
    {
        currentSpawner = spawner;
    }

    private IEnumerator SimulationLoop()
    {
        yield return null;

        while (true)
        {
            bool changedToCurrentScene = SimulateGuestsOffscreen();

            if (changedToCurrentScene && currentSpawner != null)
            {
                currentSpawner.RefreshGuests();
            }

            yield return new WaitForSeconds(simulationInterval);
        }
    }

    public bool SimulateGuestsOffscreen()
    {
        if (HotelGameData.Instance == null)
            return false;

        bool changedToCurrentScene = false;
        string currentScene = SceneManager.GetActiveScene().name;

        int movedThisCycle = 0;

        foreach (RoomRuntimeData room in HotelGameData.Instance.rooms)
        {
            if (room == null)
                continue;

            if (room.state != RoomState.Ocupada || !room.hasGuestData)
                continue;

            string npcId = "Guest_" + room.roomId;

            if (!GuestNPCMemory.TryGetState(npcId, out GuestNPCState state))
            {
                state = CreateInitialBedroomState(room);
            }

            // Si el NPC está en la escena visible, no se simula offscreen.
            // Ahí lo maneja NPCVisibleTravelBrain.
            if (state.sceneName == currentScene)
                continue;

            if (Time.time < state.nextDecisionTime)
                continue;

            float timeAway = Time.time - state.lastSeenTime;

            if (timeAway < minSecondsBeforeDecision)
                continue;

            bool changed = SimulateStateTransition(state);

            if (changed)
            {
                changedToCurrentScene = true;
                movedThisCycle++;

                if (movedThisCycle >= maxGuestsMovedPerCycle)
                    break;
            }
        }

        return changedToCurrentScene;
    }

    private GuestNPCState CreateInitialBedroomState(RoomRuntimeData room)
    {
        string npcId = "Guest_" + room.roomId;

        float firstDecisionDelay = Random.Range(minNextDecisionDelay, maxNextDecisionDelay);

        GuestNPCState newState = new GuestNPCState
        {
            npcId = npcId,
            assignedRoomId = room.roomId,
            sceneName = bedroomSceneName,
            area = GuestArea.Room,
            position = Vector3.zero,
            hasValidPosition = false,
            destinationSpawnId = "",
            lastSeenTime = Time.time,
            nextDecisionTime = Time.time + firstDecisionDelay
        };

        GuestNPCMemory.SaveState(npcId, newState);

        return newState;
    }

    private bool SimulateStateTransition(GuestNPCState state)
    {
        if (state.sceneName == bedroomSceneName)
            return TryBedroomToHotel(state);

        if (state.sceneName == hotelSceneName)
            return TryHotelDecision(state);

        if (state.sceneName == outsideSceneName)
            return TryOutsideDecision(state);

        if (state.sceneName == restaurantSceneName)
            return TryRestaurantDecision(state);

        ScheduleNextDecision(state);
        return false;
    }

    private bool TryBedroomToHotel(GuestNPCState state)
    {
        if (Random.value > chanceBedroomToHotel)
        {
            ScheduleNextDecision(state);
            return false;
        }

        Vector3 hotelDoorPosition = GetHotelDoorPosition(state.assignedRoomId);

        return MoveGuestToScene(
            state,
            hotelSceneName,
            GuestArea.Hotel,
            hotelDoorPosition,
            true,
            "",
            "salió de su habitación al hotel"
        );
    }

    private bool TryHotelDecision(GuestNPCState state)
    {
        float roll = Random.value;

        if (roll < chanceHotelToOutside)
        {
            return MoveGuestToScene(
                state,
                outsideSceneName,
                GuestArea.Outside,
                Vector3.zero,
                false,
                outsideHotelEntranceSpawnId,
                "salió del hotel hacia Outside"
            );
        }

        if (roll < chanceHotelToOutside + chanceHotelToBedroom)
        {
            return MoveGuestToScene(
                state,
                bedroomSceneName,
                GuestArea.Room,
                Vector3.zero,
                false,
                roomInsideSpawnId,
                "regresó a su habitación"
            );
        }

        ScheduleNextDecision(state);
        return false;
    }

    private bool TryOutsideDecision(GuestNPCState state)
    {
        float roll = Random.value;

        if (roll < chanceOutsideToRestaurant)
        {
            return MoveGuestToScene(
                state,
                restaurantSceneName,
                GuestArea.Restaurant,
                Vector3.zero,
                false,
                restaurantEntranceSpawnId,
                "fue de Outside al restaurante"
            );
        }

        if (roll < chanceOutsideToRestaurant + chanceOutsideToHotel)
        {
            return MoveGuestToScene(
                state,
                hotelSceneName,
                GuestArea.Hotel,
                Vector3.zero,
                false,
                hotelEntranceSpawnId,
                "regresó de Outside al hotel"
            );
        }

        ScheduleNextDecision(state);
        return false;
    }

    private bool TryRestaurantDecision(GuestNPCState state)
    {
        if (Random.value > chanceRestaurantToOutside)
        {
            ScheduleNextDecision(state);
            return false;
        }

        return MoveGuestToScene(
            state,
            outsideSceneName,
            GuestArea.Outside,
            Vector3.zero,
            false,
            outsideRestaurantEntranceSpawnId,
            "salió del restaurante hacia Outside"
        );
    }

    private Vector3 GetHotelDoorPosition(string roomId)
    {
        Vector3 hotelDoorPosition = hotelFallbackPosition;

        bool foundDoor =
            RoomDoorRegistry.Instance != null &&
            RoomDoorRegistry.Instance.TryGetDoorSpawnPosition(roomId, out hotelDoorPosition);

        if (!foundDoor)
        {
            Debug.LogWarning("No se encontró NPCExitSpawn para roomId: " + roomId + ". Se usará fallback.");
        }

        return hotelDoorPosition;
    }

    private bool MoveGuestToScene(
        GuestNPCState oldState,
        string destinationSceneName,
        GuestArea destinationArea,
        Vector3 destinationPosition,
        bool hasValidPosition,
        string destinationSpawnId,
        string actionLog)
    {
        GuestNPCState newState = new GuestNPCState
        {
            npcId = oldState.npcId,
            assignedRoomId = oldState.assignedRoomId,
            sceneName = destinationSceneName,
            area = destinationArea,
            position = destinationPosition,
            hasValidPosition = hasValidPosition,
            destinationSpawnId = destinationSpawnId,
            lastSeenTime = Time.time,
            nextDecisionTime = GetNextDecisionTime()
        };

        GuestNPCMemory.SaveState(oldState.npcId, newState);

        Debug.Log(oldState.npcId + " " + actionLog + ". Nueva escena: " + destinationSceneName);

        return destinationSceneName == SceneManager.GetActiveScene().name;
    }

    private void ScheduleNextDecision(GuestNPCState state)
    {
        state.lastSeenTime = Time.time;
        state.nextDecisionTime = GetNextDecisionTime();

        GuestNPCMemory.SaveState(state.npcId, state);
    }

    private float GetNextDecisionTime()
    {
        return Time.time + Random.Range(minNextDecisionDelay, maxNextDecisionDelay);
    }
}