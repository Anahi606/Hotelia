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
        else
            Debug.LogWarning("EndDayPanel no está asignado en EndDayBoardInteractable.");

        if (interactText != null)
            interactText.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside) return;
        if (panelOpen) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("E presionada dentro del trigger de fin de día.");
            OpenPanel();
        }
    }

    private void OpenPanel()
    {
        if (endDayPanel == null)
        {
            Debug.LogWarning("No se puede abrir EndDayPanel porque no está asignado.");
            return;
        }

        endDayPanel.SetActive(true);

        if (interactText != null)
            interactText.SetActive(false);

        panelOpen = true;

        Debug.Log("EndDayPanel abierto.");
    }

    public void ConfirmEndDay()
    {
        ClosePanel();

        if (DailySummaryUI.Instance != null)
        {
            DailySummaryUI.Instance.OpenSummary();
        }
        else
        {
            Debug.LogWarning("No se encontró DailySummaryUI en la escena.");
        }
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
        Debug.Log("Entró al trigger: " + other.name);

        if (other.CompareTag("Player"))
        {
            playerInside = true;

            if (interactText != null && !panelOpen)
                interactText.SetActive(true);

            Debug.Log("Player dentro del trigger de fin de día.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Salió del trigger: " + other.name);

        if (other.CompareTag("Player"))
        {
            playerInside = false;

            if (interactText != null)
                interactText.SetActive(false);

            Debug.Log("Player salió del trigger de fin de día.");
        }
    }
}