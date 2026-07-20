using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RestaurantInteractable : MonoBehaviour
{
    [Header("Restaurant")]
    [SerializeField] private RestaurantOrderManager restaurantOrderManager;

    [Header("Interaction Prompt")]
    [Tooltip("Objeto 2D con SpriteRenderer que muestra la tecla E.")]
    [SerializeField] private GameObject interactionPrompt;

    [Tooltip("Texto TextMeshPro que acompaña al indicador de la tecla E.")]
    [SerializeField] private TMP_Text interactionPromptText;

    private bool playerInside;
    private bool restaurantOpen;

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

        if (!playerInside || restaurantOpen)
        {
            HideInteractionPrompt();
            return;
        }

        if (restaurantOrderManager == null)
        {
            HideInteractionPrompt();
            return;
        }

        ShowInteractionPrompt();

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        OpenRestaurant();
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

    private void OpenRestaurant()
    {
        if (restaurantOrderManager == null)
        {
            Debug.LogWarning(
                "RestaurantOrderManager no está asignado " +
                "en RestaurantInteractable."
            );

            return;
        }
        if (IsInteractionBlocked())
        {
            Debug.Log(
                "La interacción del restaurante fue bloqueada " +
                "porque el jugador está escribiendo o hablando con un NPC."
            );

            return;
        }

        if (restaurantOpen)
            return;

        restaurantOpen = true;

        HideInteractionPrompt();

        restaurantOrderManager.OpenRestaurant();

        Debug.Log("Restaurante abierto.");
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
            "Jugador dentro del trigger del restaurante."
        );
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        HideInteractionPrompt();

        Debug.Log(
            "Jugador salió del trigger del restaurante."
        );
    }

    public void NotifyRestaurantClosed()
    {
        restaurantOpen = false;

        if (CanShowInteractionPrompt())
        {
            ShowInteractionPrompt();
        }
        else
        {
            HideInteractionPrompt();
        }

        Debug.Log("Restaurante cerrado.");
    }

    private bool CanShowInteractionPrompt()
    {
        if (!playerInside)
            return false;

        if (restaurantOpen)
            return false;

        if (restaurantOrderManager == null)
            return false;

        if (IsInteractionBlocked())
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