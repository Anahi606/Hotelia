using System.Collections.Generic;
using UnityEngine;

public static class GuestNPCMemory
{
    private static Dictionary<string, GuestNPCState> npcStates =
        new Dictionary<string, GuestNPCState>();

    public static void SaveState(string npcId, GuestNPCState state)
    {
        if (string.IsNullOrEmpty(npcId))
            return;

        npcStates[npcId] = state;
    }

    public static bool TryGetState(string npcId, out GuestNPCState state)
    {
        return npcStates.TryGetValue(npcId, out state);
    }

    public static void RemoveState(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
            return;

        if (npcStates.ContainsKey(npcId))
            npcStates.Remove(npcId);
    }

    public static void ClearAll()
    {
        npcStates.Clear();
    }
}

[System.Serializable]
public class GuestNPCState
{
    public string npcId;
    public string assignedRoomId;
    public string sceneName;
    public GuestArea area;
    public Vector3 position;
    public bool hasValidPosition;
    public string destinationSpawnId;
    public float lastSeenTime;
    public float nextDecisionTime;
}