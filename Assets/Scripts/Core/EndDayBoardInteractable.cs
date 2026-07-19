using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class EndDayBoardInteractable : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject endDayPanel;

    [Header("Interaction Prompt")]
    [Tooltip("Objeto 2D con SpriteRenderer que muestra la tecla E.")]
    [SerializeField] private GameObject interactionPrompt;

    [Tooltip("Texto TextMeshPro que acompaña al indicador de la tecla E.")]
    [SerializeField] private TMP_Text interactionPromptText;

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
        if (IsInteractionBlocked())
        {
            HideInteractionPrompt();
            return;
        }

        if (!playerInside || panelOpen)
        {
            HideInteractionPrompt();
            return;
        }

        ShowInteractionPrompt();

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log(
                "E presionada dentro del trigger de fin de día."
            );

            OpenPanel();
        }
    }

    private bool IsInteractionBlocked()
    {
        if (Ollama_Handler.Instance != null &&
            Ollama_Handler.Instance.IsOpen)
        {
            return true;
        }

        if (EventSystem.current != null)
        {
            GameObject selectedObject =
                EventSystem.current.currentSelectedGameObject;

            if (selectedObject != null)
            {
                TMP_InputField selectedInput =
                    selectedObject.GetComponent<TMP_InputField>();

                if (selectedInput == null)
                {
                    selectedInput =
                        selectedObject.GetComponentInParent<TMP_InputField>();
                }

                if (selectedInput != null &&
                    selectedInput.isFocused)
                {
                    return true;
                }
            }
        }

        return false;
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

        if (IsInteractionBlocked())
        {
            Debug.Log(
                "La interacción de fin de día fue bloqueada " +
                "porque el jugador está escribiendo o hablando con un NPC."
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
        {
            endDayPanel.SetActive(false);
        }

        panelOpen = false;

        if (playerInside && !IsInteractionBlocked())
        {
            ShowInteractionPrompt();
        }
        else
        {
            HideInteractionPrompt();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (!panelOpen && !IsInteractionBlocked())
        {
            ShowInteractionPrompt();
        }
        else
        {
            HideInteractionPrompt();
        }

        Debug.Log(
            "Player dentro del trigger de fin de día."
        );
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        HideInteractionPrompt();

        Debug.Log(
            "Player salió del trigger de fin de día."
        );
    }

    private void ShowInteractionPrompt()
    {
        if (interactionPrompt != null &&
            !interactionPrompt.activeSelf)
        {
            interactionPrompt.SetActive(true);
        }

        if (interactionPromptText != null &&
            !interactionPromptText.gameObject.activeSelf)
        {
            interactionPromptText.gameObject.SetActive(true);
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null &&
            interactionPrompt.activeSelf)
        {
            interactionPrompt.SetActive(false);
        }

        if (interactionPromptText != null &&
            interactionPromptText.gameObject.activeSelf)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        HideInteractionPrompt();
    }
}