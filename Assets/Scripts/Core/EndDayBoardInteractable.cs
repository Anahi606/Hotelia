using UnityEngine;
using UnityEngine.InputSystem;

public class EndDayBoardInteractable : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject endDayPanel;
    [SerializeField] private GameObject interactText;

    private bool playerInside;
    private bool panelOpen;

    private void Start()
    {
        if (endDayPanel != null)
            endDayPanel.SetActive(false);

        if (interactText != null)
            interactText.SetActive(false);
    }

    private void Update()
    {
        if (playerInside && !panelOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenPanel();
        }
    }

    private void OpenPanel()
    {
        if (endDayPanel != null)
            endDayPanel.SetActive(true);

        if (interactText != null)
            interactText.SetActive(false);

        panelOpen = true;
    }

    public void ConfirmEndDay()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.EndDay();
        }
        else
        {
            Debug.LogWarning("No se encontró una instancia de DayManager en la escena.");
        }

        ClosePanel();
    }

    public void CancelEndDay()
    {
        ClosePanel();
    }

    private void ClosePanel()
    {
        if (endDayPanel != null)
            endDayPanel.SetActive(false);

        if (interactText != null && playerInside)
            interactText.SetActive(true);

        panelOpen = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;

            if (interactText != null && !panelOpen)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            if (interactText != null)
                interactText.SetActive(false);
        }
    }
}