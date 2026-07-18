using UnityEngine;
using UnityEngine.InputSystem;

public class ReceptionStartCheckIn : MonoBehaviour
{
    [Header("Check-In")]
    [SerializeField] private CheckInFlowController flowController;

    [Header("Interaction Prompt")]
    [Tooltip("Objeto 2D con SpriteRenderer que muestra la tecla E.")]
    [SerializeField] private GameObject interactionPrompt;

    private bool playerInside;

    private void Start()
    {
        HideInteractionPrompt();
    }

    private void Update()
    {
        if (!playerInside)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (flowController == null)
        {
            Debug.LogWarning(
                "FlowController no está asignado en ReceptionStartCheckIn."
            );

            return;
        }

        if (flowController.IsCheckInActive)
            return;

        HideInteractionPrompt();
        flowController.StartCheckIn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (flowController == null || !flowController.IsCheckInActive)
            ShowInteractionPrompt();

        Debug.Log("Jugador dentro del trigger de recepción.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        HideInteractionPrompt();

        Debug.Log("Jugador salió del trigger de recepción.");
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
                "Interaction Prompt no está asignado en ReceptionStartCheckIn."
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

    public void RefreshInteractionPrompt()
    {
        if (!playerInside)
            return;

        if (flowController != null && flowController.IsCheckInActive)
            return;

        ShowInteractionPrompt();
    }
}