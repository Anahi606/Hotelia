using TMPro;
using UnityEngine;

public class RoomInfoPanelUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text roomTitleText;
    public TMP_Text bedTypeText;
    public TMP_Text bedCountText;
    public TMP_Text accessibleText;
    public TMP_Text stateText;

    private RoomData currentRoom;
    private CheckInFlowController flowController;

    public void Show(RoomData room, CheckInFlowController controller)
    {
        currentRoom = room;
        flowController = controller;

        if (roomTitleText != null)
            roomTitleText.text = "Room " + room.roomId;

        if (bedTypeText != null)
            bedTypeText.text = "Type: " + room.bedType;

        if (bedCountText != null)
            bedCountText.text = "Beds: " + room.bedCount;

        if (accessibleText != null)
            accessibleText.text = room.isAccessible ? "Accessible: Yes" : "Accessible: No";

        if (stateText != null)
            stateText.text = "Status: " + GetRoomStateName(room.state);

        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    public void AssignRoom()
    {
        if (currentRoom == null || flowController == null) return;

        flowController.SelectRoom(currentRoom);
        flowController.ConfirmRoomSelection();
        gameObject.SetActive(false);
    }

    private string GetRoomStateName(RoomState state)
    {
        switch (state)
        {
            case RoomState.Available:
                return "Available";

            case RoomState.Occupied:
                return "Occupied";

            case RoomState.Dirty:
                return "Needs cleaning";

            default:
                return state.ToString();
        }
    }
}