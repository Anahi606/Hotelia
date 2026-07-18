using UnityEngine;
using UnityEngine.InputSystem;

public class RestaurantInteractable : MonoBehaviour
{
    [Header("Restaurant")]
    [SerializeField] private RestaurantOrderManager restaurantOrderManager;

    [Header("Interaction Prompt")]
    [Tooltip("Objeto 2D con SpriteRenderer que muestra la tecla E.")]
    [SerializeField] private GameObject interactionPrompt;

    private bool playerInside;
    private bool restaurantOpen;

    private void Start()
    {
        HideInteractionPrompt();
    }

    private void Update()
    {
        if (!playerInside || restaurantOpen)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (restaurantOrderManager == null)
        {
            Debug.LogWarning(
                "RestaurantOrderManager no está asignado en RestaurantInteractable."
            );

            return;
        }

        OpenRestaurant();
    }

    private void OpenRestaurant()
    {
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

        if (!restaurantOpen)
            ShowInteractionPrompt();

        Debug.Log("Jugador dentro del trigger del restaurante.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        HideInteractionPrompt();

        Debug.Log("Jugador salió del trigger del restaurante.");
    }

    public void NotifyRestaurantClosed()
    {
        restaurantOpen = false;

        if (playerInside)
            ShowInteractionPrompt();
        else
            HideInteractionPrompt();

        Debug.Log("Restaurante cerrado.");
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
                "Interaction Prompt no está asignado en RestaurantInteractable."
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