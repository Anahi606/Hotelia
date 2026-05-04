using System.Collections;
using UnityEngine;

[RequireComponent(typeof(GuestNPC))]
[RequireComponent(typeof(RandomGridRoamer))]
public class NPCVisibleTravelBrain : MonoBehaviour
{
    [Header("Decision Time")]
    public float minDecisionDelay = 8f;
    public float maxDecisionDelay = 20f;

    [Header("Chances")]
    [Range(0f, 1f)] public float chanceBedroomToHotel = 0.35f;
    [Range(0f, 1f)] public float chanceHotelToOutside = 0.25f;
    [Range(0f, 1f)] public float chanceHotelToRoom = 0.20f;
    [Range(0f, 1f)] public float chanceOutsideToHotel = 0.30f;
    [Range(0f, 1f)] public float chanceOutsideToRestaurant = 0.30f;
    [Range(0f, 1f)] public float chanceRestaurantToOutside = 0.40f;

    private GuestNPC npc;
    private RandomGridRoamer roamer;

    private bool isGoingToExit;
    private NPCSceneExit targetExit;

    private void Awake()
    {
        npc = GetComponent<GuestNPC>();
        roamer = GetComponent<RandomGridRoamer>();
    }

    private void Start()
    {
        RandomizePersonality();
        StartCoroutine(DecisionLoop());
    }

    public bool IsTargetExit(NPCSceneExit exit)
    {
        return isGoingToExit && targetExit == exit;
    }

    private IEnumerator DecisionLoop()
    {
        yield return new WaitForSeconds(Random.Range(minDecisionDelay, maxDecisionDelay));

        while (true)
        {
            if (!isGoingToExit)
            {
                TryMakeTravelDecision();
            }

            yield return new WaitForSeconds(Random.Range(minDecisionDelay, maxDecisionDelay));
        }
    }

    private void TryMakeTravelDecision()
    {
        if (!TryChooseDestination(out GuestArea desiredDestination))
            return;

        NPCSceneExit exit = FindExitTo(desiredDestination);

        if (exit == null)
        {
            Debug.LogWarning(npc.npcId + " no encontró trigger hacia: " + desiredDestination);
            return;
        }

        isGoingToExit = true;
        targetExit = exit;

        roamer.MoveToDestination(exit.GetWalkPosition());

        Debug.Log(npc.npcId + " decidió caminar hacia: " + desiredDestination);
    }

    private void RandomizePersonality()
    {
        float delayMultiplier = Random.Range(0.75f, 1.5f);

        minDecisionDelay *= delayMultiplier;
        maxDecisionDelay *= delayMultiplier;

        chanceBedroomToHotel *= Random.Range(0.6f, 1.2f);
        chanceHotelToOutside *= Random.Range(0.5f, 1.3f);
        chanceHotelToRoom *= Random.Range(0.5f, 1.3f);
        chanceOutsideToHotel *= Random.Range(0.6f, 1.2f);
        chanceOutsideToRestaurant *= Random.Range(0.5f, 1.3f);
        chanceRestaurantToOutside *= Random.Range(0.6f, 1.2f);

        chanceBedroomToHotel = Mathf.Clamp01(chanceBedroomToHotel);
        chanceHotelToOutside = Mathf.Clamp01(chanceHotelToOutside);
        chanceHotelToRoom = Mathf.Clamp01(chanceHotelToRoom);
        chanceOutsideToHotel = Mathf.Clamp01(chanceOutsideToHotel);
        chanceOutsideToRestaurant = Mathf.Clamp01(chanceOutsideToRestaurant);
        chanceRestaurantToOutside = Mathf.Clamp01(chanceRestaurantToOutside);
    }
    private bool TryChooseDestination(out GuestArea destination)
    {
        destination = npc.currentArea;

        float roll = Random.value;

        if (npc.currentArea == GuestArea.Room)
        {
            if (roll <= chanceBedroomToHotel)
            {
                destination = GuestArea.Hotel;
                return true;
            }

            return false;
        }

        if (npc.currentArea == GuestArea.Hotel)
        {
            if (roll <= chanceHotelToOutside)
            {
                destination = GuestArea.Outside;
                return true;
            }

            if (roll <= chanceHotelToOutside + chanceHotelToRoom)
            {
                destination = GuestArea.Room;
                return true;
            }

            return false;
        }

        if (npc.currentArea == GuestArea.Outside)
        {
            if (roll <= chanceOutsideToHotel)
            {
                destination = GuestArea.Hotel;
                return true;
            }

            if (roll <= chanceOutsideToHotel + chanceOutsideToRestaurant)
            {
                destination = GuestArea.Restaurant;
                return true;
            }

            return false;
        }

        if (npc.currentArea == GuestArea.Restaurant)
        {
            if (roll <= chanceRestaurantToOutside)
            {
                destination = GuestArea.Outside;
                return true;
            }

            return false;
        }

        return false;
    }

    private NPCSceneExit FindExitTo(GuestArea destination)
    {
        NPCSceneExit[] exits = FindObjectsByType<NPCSceneExit>(FindObjectsSortMode.None);

        foreach (NPCSceneExit exit in exits)
        {
            if (exit == null)
                continue;

            if (exit.destinationArea != destination)
                continue;

            if (!exit.CanBeUsedBy(npc))
                continue;

            if (destination == GuestArea.Room)
            {
                if (!exit.onlyAssignedRoom)
                    continue;

                if (exit.roomId.Trim() != npc.assignedRoomId.Trim())
                    continue;
            }

            return exit;
        }

        return null;
    }
}