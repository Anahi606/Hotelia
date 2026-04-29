using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RestaurantTicketUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Texts")]
    public TMP_Text roomText;
    public TMP_Text segmentText;
    public TMP_Text dishText;
    public TMP_Text tagsText;

    [Header("Visual")]
    public Image backgroundImage;

    [Header("Double Click")]
    public float doubleClickTime = 0.35f;

    private RestaurantOrder order;
    private RestaurantOrderManager manager;

    private float lastClickTime;

    public RestaurantOrder Order => order;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundImage.raycastTarget = true;
    }

    public void Setup(RestaurantOrder newOrder, RestaurantOrderManager newManager)
    {
        order = newOrder;
        manager = newManager;

        if (roomText != null)
            roomText.text = "Habitación " + order.roomId;

        if (segmentText != null)
            segmentText.text = "Segmento: " + order.segment;

        if (dishText != null)
            dishText.text = "Pedido: " + order.dishName;

        if (tagsText != null)
            tagsText.text = GetTagsText(order);

        if (backgroundImage != null)
        {
            if (order.hasAllergy)
                backgroundImage.color = new Color(1f, 0.78f, 0.78f);
            else if (order.isUrgent)
                backgroundImage.color = new Color(1f, 0.92f, 0.75f);
            else
                backgroundImage.color = Color.white;

            backgroundImage.raycastTarget = true;
        }

        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickTime)
        {
            SelectTicketByDoubleClick();
            lastClickTime = 0f;
        }
        else
        {
            lastClickTime = Time.time;
        }
    }

    private void SelectTicketByDoubleClick()
    {
        if (manager != null && order != null)
        {
            manager.SelectOrder(order);
        }
    }

    private string GetTagsText(RestaurantOrder order)
    {
        string tags = "";

        if (order.hasAllergy)
            tags += "[Alergia] ";

        if (order.isUrgent)
            tags += "[Urgente] ";

        if (order.isRoomService)
            tags += "[Room service] ";

        if (string.IsNullOrEmpty(tags))
            tags = "[Normal]";

        return tags;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}