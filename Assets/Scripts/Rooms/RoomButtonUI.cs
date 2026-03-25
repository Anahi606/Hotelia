using UnityEngine;
using UnityEngine.UI;

public class RoomButtonUI : MonoBehaviour
{
    public RoomData roomData;
    public CheckInFlowController flowController;

    private Image image;
    private Button button;

    void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (roomData == null)
        {
            Debug.LogWarning($"{gameObject.name}: roomData no asignado.");
            return;
        }

        bool isFree = roomData.state == RoomState.Libre;
        Debug.Log($"{gameObject.name} -> Room {roomData.roomId} estado: {roomData.state}, visible: {isFree}");
        gameObject.SetActive(isFree);

        if (button != null)
            button.interactable = isFree;

        if (image != null && isFree)
            image.color = Color.green;
    }

    public void OnClickRoom()
    {
        if (roomData == null || flowController == null) return;
        if (roomData.state != RoomState.Libre) return;
        Debug.Log("Click en habitación: " + roomData.roomId);
        flowController.ShowRoomInfo(roomData);
    }
}