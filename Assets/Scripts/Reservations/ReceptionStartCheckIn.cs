using UnityEngine;
using UnityEngine.InputSystem;

public class ReceptionStartCheckIn : MonoBehaviour
{
    public CheckInFlowController flowController;

    private bool playerInside;

    private void Update()
    {
        if (!playerInside) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (flowController == null)
            {
                Debug.LogWarning("FlowController no está asignado en ReceptionStartCheckIn.");
                return;
            }

            if (!flowController.IsCheckInActive)
            {
                flowController.StartCheckIn();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            Debug.Log("Jugador dentro del trigger de recepción.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            Debug.Log("Jugador salió del trigger de recepción.");
        }
    }
}