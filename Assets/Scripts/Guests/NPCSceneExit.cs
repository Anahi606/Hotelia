using UnityEngine;

public class NPCSceneExit : MonoBehaviour
{
    [Header("Source")]
    public GuestArea sourceArea;

    [Header("Destination")]
    public string destinationSceneName;
    public GuestArea destinationArea;

    [Header("Spawn In Destination")]
    public bool useExactDestinationPosition;
    public Vector3 destinationPosition;
    public string destinationSpawnId;

    [Header("Room Filter")]
    public bool onlyAssignedRoom;
    public string roomId;

    [Header("Walk Target")]
    public Transform walkTarget;

    public Vector3 GetWalkPosition()
    {
        if (walkTarget != null)
            return walkTarget.position;

        return transform.position;
    }

    public bool CanBeUsedBy(GuestNPC npc)
    {
        if (npc == null)
            return false;

        if (npc.currentArea != sourceArea)
            return false;

        if (onlyAssignedRoom)
        {
            if (string.IsNullOrEmpty(roomId))
                return false;

            if (npc.assignedRoomId.Trim() != roomId.Trim())
                return false;
        }

        return true;
    }

    public void UseExit(GuestNPC npc)
    {
        if (npc == null)
            return;

        if (!CanBeUsedBy(npc))
            return;

        GuestNPCState state = new GuestNPCState
        {
            npcId = npc.npcId,
            assignedRoomId = npc.assignedRoomId,
            sceneName = destinationSceneName,
            area = destinationArea,

            position = useExactDestinationPosition ? destinationPosition : Vector3.zero,
            hasValidPosition = useExactDestinationPosition,

            destinationSpawnId = destinationSpawnId,

            lastSeenTime = Time.time,
            nextDecisionTime = Time.time + Random.Range(8f, 20f)
        };

        GuestNPCMemory.SaveState(npc.npcId, state);

        npc.DisableSaveOnDestroy();
        Destroy(npc.gameObject);

        Debug.Log(npc.npcId + " cambió de escena lógica hacia: " + destinationSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GuestNPC npc = other.GetComponentInParent<GuestNPC>();

        if (npc == null)
            return;

        NPCVisibleTravelBrain brain = npc.GetComponent<NPCVisibleTravelBrain>();

        if (brain == null || !brain.IsTargetExit(this))
            return;

        UseExit(npc);
    }
}