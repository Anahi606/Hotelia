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

    public void SpawnTrash()
    {
        ClearTrash();

        if (trashPrefab == null || trashSprites == null || trashSprites.Length == 0 || spawnAreas == null || spawnAreas.Length == 0)
        {
            Debug.LogWarning("TrashSpawner no está bien configurado.");
            return;
        }

        int amount = Random.Range(minTrash, maxTrash + 1);
        List<Vector3> usedPositions = new List<Vector3>();

        int safety = 0;
        int maxAttempts = 200;

        while (spawnedTrash.Count < amount && safety < maxAttempts)
        {
            safety++;

            TrashSpawnArea randomArea = spawnAreas[Random.Range(0, spawnAreas.Length)];
            Vector3 spawnPos = randomArea.GetRandomPosition();

            if (!IsFarEnoughFromOthers(spawnPos, usedPositions))
                continue;

            GameObject trashObj = Instantiate(trashPrefab, spawnPos, Quaternion.identity);

            SpriteRenderer sr = trashObj.GetComponent<SpriteRenderer>();
            TrashItem trashItem = trashObj.GetComponent<TrashItem>();

            if (sr != null)
            {
                sr.sprite = trashSprites[Random.Range(0, trashSprites.Length)];
            }

            if (trashItem != null)
            {
                trashItem.Setup(this);
                spawnedTrash.Add(trashItem);
            }

            usedPositions.Add(spawnPos);
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

    public void RemoveTrash(TrashItem trash)
    {
        if (spawnedTrash.Contains(trash))
        {
            spawnedTrash.Remove(trash);
        }
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
    }

    public void SetTrashRange(int min, int max)
    {
        minTrash = min;
        maxTrash = max;
    }
}