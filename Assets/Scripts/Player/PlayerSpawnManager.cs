using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    private void Start()
    {
        if (string.IsNullOrEmpty(PlayerSpawnMemory.nextSpawnId))
        {
            Debug.Log("No hay nextSpawnId. Player se queda donde está en la escena.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("No se encontró Player para mover al spawn.");
            return;
        }

        PlayerSpawnPoint[] spawnPoints =
            FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);

        Debug.Log("Buscando PlayerSpawnPoint con id: " + PlayerSpawnMemory.nextSpawnId +
                  " | Spawns encontrados: " + spawnPoints.Length);

        foreach (PlayerSpawnPoint spawnPoint in spawnPoints)
        {
            Debug.Log("Spawn disponible: " + spawnPoint.spawnId + " | Pos: " + spawnPoint.transform.position);

            if (spawnPoint.spawnId == PlayerSpawnMemory.nextSpawnId)
            {
                player.transform.position = spawnPoint.transform.position;

                Debug.Log("Player spawneado en: " + spawnPoint.spawnId);

                PlayerSpawnMemory.Clear();
                return;
            }
        }

        Debug.LogWarning("No existe PlayerSpawnPoint con id: " + PlayerSpawnMemory.nextSpawnId);
    }
}