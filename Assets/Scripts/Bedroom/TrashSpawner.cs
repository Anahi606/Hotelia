using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    [Header("Trash Setup")]
    [SerializeField] private GameObject trashPrefab;
    [SerializeField] private Sprite[] trashSprites;

    [Header("Spawn Areas")]
    [SerializeField] private TrashSpawnArea[] spawnAreas;

    [Header("Amount")]
    [SerializeField] private int minTrash = 2;
    [SerializeField] private int maxTrash = 5;

    [Header("Spacing")]
    [SerializeField] private float minDistanceBetweenTrash = 0.7f;

    private readonly List<TrashItem> spawnedTrash = new List<TrashItem>();
    public int TotalSpawnedTrash { get; private set; }


    public void SpawnTrash()
    {
        ClearTrash();

        if (trashPrefab == null ||
            trashSprites == null ||
            trashSprites.Length == 0 ||
            spawnAreas == null ||
            spawnAreas.Length == 0)
        {
            Debug.LogWarning(
                "TrashSpawner no está bien configurado."
            );

            return;
        }

        int validMinimum = Mathf.Max(0, minTrash);
        int validMaximum = Mathf.Max(
            validMinimum,
            maxTrash
        );

        int amount = Random.Range(
            validMinimum,
            validMaximum + 1
        );

        List<Vector3> usedPositions =
            new List<Vector3>();

        const int maxAttempts = 200;
        int attempts = 0;

        while (
            spawnedTrash.Count < amount &&
            attempts < maxAttempts)
        {
            attempts++;

            TrashSpawnArea randomArea =
                spawnAreas[Random.Range(0, spawnAreas.Length)];

            if (randomArea == null)
                continue;

            Vector3 spawnPosition =
                randomArea.GetRandomPosition();

            if (!IsFarEnoughFromOthers(
                    spawnPosition,
                    usedPositions))
            {
                continue;
            }

            GameObject trashObject = Instantiate(
                trashPrefab,
                spawnPosition,
                Quaternion.identity
            );

            TrashItem trashItem =
                trashObject.GetComponent<TrashItem>();

            if (trashItem == null)
            {
                Debug.LogError(
                    "El prefab de basura no contiene TrashItem."
                );

                Destroy(trashObject);
                continue;
            }

            SpriteRenderer spriteRenderer =
                trashObject.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite =
                    trashSprites[
                        Random.Range(0, trashSprites.Length)
                    ];
            }

            trashItem.Setup(this);
            spawnedTrash.Add(trashItem);
            usedPositions.Add(spawnPosition);
        }

        TotalSpawnedTrash = spawnedTrash.Count;

        if (TotalSpawnedTrash < amount)
        {
            Debug.LogWarning(
                "No fue posible generar toda la basura solicitada. " +
                "Generada: " + TotalSpawnedTrash +
                " / Solicitada: " + amount
            );
        }
    }

    private bool IsFarEnoughFromOthers(Vector3 candidate, List<Vector3> usedPositions)
    {
        foreach (Vector3 pos in usedPositions)
        {
            if (Vector3.Distance(candidate, pos) < minDistanceBetweenTrash)
                return false;
        }

        return true;
    }

    public bool RemoveTrash(TrashItem trash)
    {
        if (trash == null)
            return false;

        bool wasRegistered = spawnedTrash.Remove(trash);

        if (!wasRegistered)
        {
            Debug.LogError(
                "El objeto no estaba registrado como basura: " +
                trash.gameObject.name,
                trash
            );

            return false;
        }

        Destroy(trash.gameObject);
        return true;
    }

    public int GetRemainingTrash()
    {
        return spawnedTrash.Count;
    }

    public void ClearTrash()
    {
        foreach (TrashItem trash in spawnedTrash)
        {
            if (trash != null)
                Destroy(trash.gameObject);
        }

        spawnedTrash.Clear();
        TotalSpawnedTrash = 0;
    }

    public void SetTrashRange(int min, int max)
    {
        minTrash = min;
        maxTrash = max;
    }
}