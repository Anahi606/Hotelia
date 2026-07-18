using UnityEngine;
using UnityEngine.InputSystem;

public class EndDayBoardInteractable : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject endDayPanel;

    [Header("Interaction Prompt")]
    [Tooltip("Objeto 2D con SpriteRenderer que muestra la tecla E.")]
    [SerializeField] private GameObject interactionPrompt;

    private bool playerInside;
    private bool panelOpen;

    private void Start()
    {
        if (endDayPanel != null)
        {
            endDayPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "EndDayPanel no está asignado en EndDayBoardInteractable."
            );
        }

        HideInteractionPrompt();
    }

    private void Update()
    {
        if (!playerInside || panelOpen)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("E presionada dentro del trigger de fin de día.");
            OpenPanel();
        }
    }

    private void OpenPanel()
    {
        if (endDayPanel == null)
        {
            Debug.LogWarning(
                "No se puede abrir EndDayPanel porque no está asignado."
            );

            return;
        }

        HideInteractionPrompt();

        endDayPanel.SetActive(true);
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
            Debug.LogWarning(
                "No se encontró DailySummaryUI en la escena."
            );
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

        panelOpen = false;

        if (playerInside)
            ShowInteractionPrompt();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (!panelOpen)
            ShowInteractionPrompt();

        Debug.Log("Player dentro del trigger de fin de día.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        HideInteractionPrompt();

        Debug.Log("Player salió del trigger de fin de día.");
    }

    private void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Interaction Prompt no está asignado en EndDayBoardInteractable."
            );
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OnDisable()
    {
        HideInteractionPrompt();
    }
}