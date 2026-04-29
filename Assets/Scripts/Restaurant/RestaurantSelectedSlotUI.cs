using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RestaurantSelectedSlotUI : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text slotText;

    [Header("Visual")]
    public Image backgroundImage;

    [Header("Double Click")]
    public float doubleClickTime = 0.35f;

    private RestaurantOrder currentOrder;
    private RestaurantOrderManager manager;

    private int slotNumber;
    private float lastClickTime;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundImage.raycastTarget = true;
    }

    public void SetManager(RestaurantOrderManager newManager)
    {
        manager = newManager;
    }

    public void SetEmpty(int newSlotNumber)
    {
        slotNumber = newSlotNumber;
        currentOrder = null;

        if (slotText != null)
            slotText.text = slotNumber + ". ---";
    }

    public void SetOrder(int newSlotNumber, RestaurantOrder order)
    {
        slotNumber = newSlotNumber;
        currentOrder = order;

        if (slotText != null)
            slotText.text = slotNumber + ". Habitación " + order.roomId + " - " + order.dishName;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentOrder == null) return;

        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickTime)
        {
            ReturnOrderByDoubleClick();
            lastClickTime = 0f;
        }
        else
        {
            lastClickTime = Time.time;
        }
    }

    private void ReturnOrderByDoubleClick()
    {
        if (manager != null && currentOrder != null)
        {
            manager.ReturnOrderToTickets(currentOrder);
        }
    }
}