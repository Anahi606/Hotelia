using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ReceptionStartCheckIn : MonoBehaviour
{
    [Header("Check-In")]
    [SerializeField] private CheckInFlowController flowController;

    [Header("Interaction Prompt")]
    [Tooltip("Objeto 2D con SpriteRenderer que muestra la tecla E.")]
    [SerializeField] private GameObject interactionPrompt;

    [Tooltip("Texto TextMeshPro que acompaña al indicador de la tecla E.")]
    [SerializeField] private TMP_Text interactionPromptText;

    private bool playerInside;

    private void Start()
    {
        HideInteractionPrompt();
    }

    private void Update()
    {
        if (IsInteractionBlocked())
        {
            HideInteractionPrompt();
            return;
        }

        if (!playerInside)
        {
            HideInteractionPrompt();
            return;
        }

        if (flowController == null)
        {
            HideInteractionPrompt();
            return;
        }

        if (flowController.IsCheckInActive)
        {
            HideInteractionPrompt();
            return;
        }

        ShowInteractionPrompt();

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        StartCheckIn();
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

    private void StartCheckIn()
    {
        if (flowController == null)
        {
            Debug.LogWarning(
                "FlowController no está asignado en ReceptionStartCheckIn."
            );

            return;
        }

        if (IsInteractionBlocked())
        {
            Debug.Log(
                "La interacción de recepción fue bloqueada " +
                "porque el jugador está escribiendo o hablando con un NPC."
            );

            return;
        }

        if (flowController.IsCheckInActive)
            return;

        HideInteractionPrompt();

        flowController.StartCheckIn();

        Debug.Log(
            "Check-in iniciado desde la recepción."
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (CanShowInteractionPrompt())
        {
            ShowInteractionPrompt();
        }
        else
        {
            HideInteractionPrompt();
        }

        Debug.Log(
            "Jugador dentro del trigger de recepción."
        );
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        HideInteractionPrompt();

        Debug.Log(
            "Jugador salió del trigger de recepción."
        );
    }

    private bool CanShowInteractionPrompt()
    {
        if (!playerInside)
            return false;

        if (IsInteractionBlocked())
            return false;

        if (flowController == null)
            return false;

        if (flowController.IsCheckInActive)
            return false;

        return true;
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

    public void RefreshInteractionPrompt()
    {
        if (CanShowInteractionPrompt())
        {
            ShowInteractionPrompt();
        }
        else
        {
            HideInteractionPrompt();
        }
    }
}