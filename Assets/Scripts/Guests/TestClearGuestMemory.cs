using UnityEngine;

public class TestClearGuestMemory : MonoBehaviour
{
    private void Awake()
    {
        GuestNPCMemory.ClearAll();
        Debug.Log("Memoria de NPCs limpiada para prueba.");
    }
}