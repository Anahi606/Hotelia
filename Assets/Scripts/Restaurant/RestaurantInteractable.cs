using UnityEngine;
using UnityEngine.InputSystem;

public class RestaurantInteractable : MonoBehaviour
{
    [Header("Restaurant")]
    public RestaurantOrderManager restaurantOrderManager;

    /*[Header("Optional UI")]
    public GameObject interactionHint;*/

    private bool playerInside;

    private void Start()
    {
        /*if (interactionHint != null)
            interactionHint.SetActive(false);*/
    }

    private void Update()
    {
        if (!playerInside) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (restaurantOrderManager != null)
            {
                restaurantOrderManager.OpenRestaurant();

                /*if (interactionHint != null)
                    interactionHint.SetActive(false);*/
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        /*if (interactionHint != null)
            interactionHint.SetActive(true);*/
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;

        /*if (interactionHint != null)
            interactionHint.SetActive(false);*/
    }
}