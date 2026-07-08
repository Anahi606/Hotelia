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

    [Header("Price Texts")]
    public TMP_Text roomPriceText;
    public TMP_Text totalPackageText;

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

        UpdatePriceInfo(room, controller);

        gameObject.SetActive(true);
    }

    private void UpdatePriceInfo(RoomData room, CheckInFlowController controller)
    {
        if (room == null || controller == null)
        {
            if (roomPriceText != null)
                roomPriceText.text = "";

            if (totalPackageText != null)
                totalPackageText.text = "";

            return;
        }

        CheckInRequest request = controller.GetCurrentRequest();

        if (request == null)
        {
            if (roomPriceText != null)
                roomPriceText.text = "";

            if (totalPackageText != null)
                totalPackageText.text = "";

            return;
        }

        int roomPrice = PackagePricing.GetRoomPrice(room, request.stayDays);

        if (roomPriceText != null)
        {
            roomPriceText.text =
                "Room price: $" + roomPrice +
                " (" + request.stayDays + " day" + (request.stayDays > 1 ? "s" : "") + ")";
        }

        if (totalPackageText != null)
        {
            if (!controller.HasConfirmedPackage())
            {
                totalPackageText.text = "Select offer and tourism extra first.";
                return;
            }

            int total = controller.GetTotalCostWithRoom(room);
            int remaining = request.clientBudget - total;

            string status = remaining >= 0
                ? "Within budget"
                : "Over budget";

            totalPackageText.text =
                "Total with this room: $" + total +
                " / Budget: $" + request.clientBudget +
                "\nRemaining: $" + remaining +
                "\nStatus: " + status;
        }
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