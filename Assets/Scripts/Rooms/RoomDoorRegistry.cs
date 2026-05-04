using System.Collections.Generic;
using UnityEngine;

public class RoomDoorRegistry : MonoBehaviour
{
    public static RoomDoorRegistry Instance { get; private set; }

    private Dictionary<string, RoomDoorInteractable> doorsByRoomId =
        new Dictionary<string, RoomDoorInteractable>();

    private void Awake()
    {
        Instance = this;
        RegisterDoors();
    }

    private void RegisterDoors()
    {
        doorsByRoomId.Clear();

        RoomDoorInteractable[] doors =
            FindObjectsByType<RoomDoorInteractable>(FindObjectsSortMode.None);

        foreach (RoomDoorInteractable door in doors)
        {
            string roomId = door.GetRoomId();

            if (string.IsNullOrEmpty(roomId))
            {
                Debug.LogWarning("Puerta sin RoomData o sin roomId: " + door.name);
                continue;
            }

            if (doorsByRoomId.ContainsKey(roomId))
            {
                Debug.LogWarning(
                    "RoomDoorRegistry encontró roomId duplicado: " + roomId +
                    " / Puerta existente: " + doorsByRoomId[roomId].name +
                    " / Puerta repetida: " + door.name
                );

                continue;
            }

            doorsByRoomId.Add(roomId, door);
        }
    }

    public bool TryGetDoorSpawnPosition(string roomId, out Vector3 position)
    {
        if (doorsByRoomId.TryGetValue(roomId, out RoomDoorInteractable door))
        {
            position = door.GetNPCExitSpawnPosition();
            return true;
        }

        Debug.LogWarning("No se encontró puerta para roomId: " + roomId);

        position = Vector3.zero;
        return false;
    }
}