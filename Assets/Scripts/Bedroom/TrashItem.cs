using UnityEngine;
using UnityEngine.InputSystem;

public class TrashItem : MonoBehaviour
{
    private TrashSpawner trashSpawner;
    private bool playerNear;

    public void Setup(TrashSpawner spawner)
    {
        trashSpawner = spawner;
    }

    private void OnMouseDown()
    {
        CleanTrash();
    }

    private void Update()
    {
        if (playerNear &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            CleanTrash();
        }
    }

    private void CleanTrash()
    {
        if (trashSpawner != null)
        {
            trashSpawner.RemoveTrash(this);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerNear = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerNear = false;
    }
}