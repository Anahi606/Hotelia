using UnityEngine;
using UnityEngine.SceneManagement;

public class GuestNPC : MonoBehaviour
{
    [Header("Identity")]
    public string npcId;

    [Header("Room")]
    public string assignedRoomId;

    [Header("Area")]
    public GuestArea currentArea;

    private RoomRuntimeData assignedRoom;
    private bool isInitialized;
    private bool allowSaveOnDestroy = true;

    public void Initialize(string roomId, GuestArea area)
    {
        assignedRoomId = roomId;
        currentArea = area;
        npcId = "Guest_" + roomId;

        LoadSavedState();
        LoadAssignedRoom();

        isInitialized = true;
    }

    private void Start()
    {
        if (isInitialized)
            return;

        if (string.IsNullOrEmpty(npcId) && !string.IsNullOrEmpty(assignedRoomId))
        {
            npcId = "Guest_" + assignedRoomId;
        }

        LoadSavedState();
        LoadAssignedRoom();

        isInitialized = true;
    }

    private void LoadAssignedRoom()
    {
        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData.");
            return;
        }

        if (string.IsNullOrEmpty(assignedRoomId))
        {
            Debug.LogWarning("Este NPC no tiene assignedRoomId.");
            return;
        }

        assignedRoom = HotelGameData.Instance.GetRoomById(assignedRoomId);

        if (assignedRoom == null)
        {
            Debug.LogWarning("No existe habitación con ID: " + assignedRoomId);
        }
    }

    private void LoadSavedState()
    {
        if (string.IsNullOrEmpty(npcId))
            return;

        if (!GuestNPCMemory.TryGetState(npcId, out GuestNPCState state))
            return;

        string currentScene = SceneManager.GetActiveScene().name;

        if (state.sceneName != currentScene)
            return;

        if (!state.hasValidPosition)
            return;

        transform.position = state.position;
        currentArea = state.area;

        Debug.Log("NPC restaurado en posición: " + state.position);
    }

    public RoomRuntimeData GetAssignedRoom()
    {
        return assignedRoom;
    }

    public void DisableSaveOnDestroy()
    {
        allowSaveOnDestroy = false;
    }

    public void SaveStateNow()
    {
        SaveState();
    }

    private void OnDisable()
    {
        if (!allowSaveOnDestroy)
            return;

        SaveState();
    }

    private void OnDestroy()
    {
        if (!allowSaveOnDestroy)
            return;

        SaveState();
    }

    private void SaveState()
    {
        if (string.IsNullOrEmpty(npcId))
            return;

        float nextDecisionTime = Time.time;

        if (GuestNPCMemory.TryGetState(npcId, out GuestNPCState oldState))
        {
            nextDecisionTime = oldState.nextDecisionTime;
        }

        GuestNPCState state = new GuestNPCState
        {
            npcId = npcId,
            assignedRoomId = assignedRoomId,
            sceneName = SceneManager.GetActiveScene().name,
            area = currentArea,
            position = transform.position,
            hasValidPosition = true,
            lastSeenTime = Time.time,
            nextDecisionTime = nextDecisionTime
        };

        GuestNPCMemory.SaveState(npcId, state);
    }
}