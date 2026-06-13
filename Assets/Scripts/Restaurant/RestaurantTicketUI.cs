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
            roomText.text = "Room " + order.roomId;

        if (segmentText != null)
            segmentText.text = "Segment: " + GetSegmentText(order.segment);

        if (dishText != null)
            dishText.text = "Order: " + order.dishName;

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
            tags += "[Allergy] ";

        if (order.isUrgent)
            tags += "[Urgent] ";

        if (order.isRoomService)
            tags += "[Room Service] ";

        if (string.IsNullOrEmpty(tags))
            tags = "[Normal]";

        return tags;
    }

    private string GetSegmentText(object segment)
    {
        if (segment == null)
            return "";

        string text = segment.ToString();

        switch (text)
        {
            case "Business":
                return "Business";

            case "Family":
                return "Family";

            case "Couple":
                return "Couple";

            case "Tourist":
                return "Tourist";

            case "VIP":
                return "VIP";

            default:
                return text;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}