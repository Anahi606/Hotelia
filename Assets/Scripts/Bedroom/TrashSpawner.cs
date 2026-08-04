using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct DeterministicTrashSpawnData
{
    public int areaIndex;
    public int spriteIndex;
    public Vector2Int gridPosition;

    public int worldXMillimeters;
    public int worldYMillimeters;
    public int worldZMillimeters;

    public Vector3 WorldPosition
    {
        get
        {
            return new Vector3(
                worldXMillimeters / 1000f,
                worldYMillimeters / 1000f,
                worldZMillimeters / 1000f
            );
        }
    }
}

public class TrashSpawner : MonoBehaviour
{
    [Header("Deterministic Generation")]
    [SerializeField]
    private bool useMasterSeed = true;

    [SerializeField]
    private ProceduralSeedManager seedManager;

    [SerializeField]
    private string generatorId = "Trash";

    [SerializeField]
    private string roomId = "Bedroom_01";

    [SerializeField]
    private int standaloneSeed = 12345;

    [SerializeField]
    private bool generateOnStart;

    [Header("Trash Setup")]
    [SerializeField]
    private GameObject trashPrefab;

    [SerializeField]
    private Sprite[] trashSprites;

    [Header("Spawn Areas")]
    [SerializeField]
    private Transform spawnAreasRoot;

    [SerializeField]
    private TrashSpawnArea[] spawnAreas;

    [SerializeField]
    private bool autoCollectSpawnAreas = true;

    [Header("Amount")]
    [SerializeField, Min(0)]
    private int minTrash = 2;

    [SerializeField, Min(0)]
    private int maxTrash = 5;

    [Header("Position")]
    [SerializeField, Min(0.001f)]
    private float positionGridSize = 0.05f;

    [SerializeField, Min(0f)]
    private float minDistanceBetweenTrash = 0.7f;

    [SerializeField, Min(1)]
    private int maxPositionAttemptsPerObject = 200;

    [Header("Generated Objects")]
    [SerializeField]
    private Transform spawnedTrashParent;

    private readonly List<TrashItem> spawnedTrash =
        new List<TrashItem>();

    public int TotalSpawnedTrash { get; private set; }

    public int CurrentSeed => ResolveGenerationSeed();

    public string LastLayoutHash { get; private set; }

    private void Start()
    {
        if (generateOnStart)
        {
            SpawnTrash();
        }
    }

    [ContextMenu("Refresh Spawn Areas")]
    public void RefreshSpawnAreas()
    {
        Transform root =
            spawnAreasRoot != null
                ? spawnAreasRoot
                : transform;

        spawnAreas =
            root.GetComponentsInChildren<TrashSpawnArea>(true);

        Array.Sort(
            spawnAreas,
            CompareSpawnAreas
        );

        Debug.Log(
            "[TrashSpawner] Spawn areas found: " +
            spawnAreas.Length,
            this
        );
    }

    [ContextMenu("Spawn Using Current Seed")]
    public void SpawnTrash()
    {
        int generationSeed =
            ResolveGenerationSeed();

        SpawnTrash(generationSeed);
    }

    public void SpawnTrash(int generationSeed)
    {
        try
        {
            List<DeterministicTrashSpawnData> layout =
                BuildLayout(generationSeed);

            ClearTrash();

            for (int index = 0;
                 index < layout.Count;
                 index++)
            {
                CreateTrashObject(
                    layout[index],
                    index,
                    generationSeed
                );
            }

            TotalSpawnedTrash =
                spawnedTrash.Count;

            LastLayoutHash =
                CalculateLayoutHash(layout);

            Debug.Log(
                "[TrashSpawner] Deterministic generation completed.\n" +
                "Seed: " + generationSeed + "\n" +
                "Requested objects: " + layout.Count + "\n" +
                "Spawned objects: " + TotalSpawnedTrash + "\n" +
                "Layout hash: " + LastLayoutHash,
                this
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[TrashSpawner] Generation failed: " +
                exception.Message,
                this
            );
        }
    }

    public List<DeterministicTrashSpawnData> BuildLayout(
        int generationSeed
    )
    {
        EnsureSpawnAreas();
        ValidateConfiguration();

        int validMinimum =
            Mathf.Max(0, minTrash);

        int validMaximum =
            Mathf.Max(
                validMinimum,
                maxTrash
            );

        DeterministicRandom amountRandom =
            new DeterministicRandom(
                generationSeed,
                0
            );

        int amount =
            amountRandom.Range(
                validMinimum,
                validMaximum + 1
            );

        List<DeterministicTrashSpawnData> layout =
            new List<DeterministicTrashSpawnData>(
                amount
            );

        List<Vector3> usedPositions =
            new List<Vector3>(amount);

        for (int objectIndex = 0;
             objectIndex < amount;
             objectIndex++)
        {
            DeterministicRandom placementRandom =
                new DeterministicRandom(
                    generationSeed,
                    1000 + objectIndex
                );

            DeterministicRandom spriteRandom =
                new DeterministicRandom(
                    generationSeed,
                    2000 + objectIndex
                );

            int spriteIndex =
                spriteRandom.Range(
                    0,
                    trashSprites.Length
                );

            bool validPositionFound = false;

            int selectedAreaIndex = 0;

            Vector2Int selectedGridPosition =
                Vector2Int.zero;

            Vector3 selectedWorldPosition =
                Vector3.zero;

            for (int attempt = 0;
                 attempt < maxPositionAttemptsPerObject;
                 attempt++)
            {
                int candidateAreaIndex =
                    placementRandom.Range(
                        0,
                        spawnAreas.Length
                    );

                TrashSpawnArea candidateArea =
                    spawnAreas[candidateAreaIndex];

                Vector2Int candidateGridPosition =
                    candidateArea
                        .GetDeterministicGridPosition(
                            ref placementRandom,
                            positionGridSize
                        );

                Vector3 candidateWorldPosition =
                    candidateArea
                        .GridPositionToWorldPosition(
                            candidateGridPosition,
                            positionGridSize
                        );

                candidateWorldPosition =
                    QuantizeWorldPosition(
                        candidateWorldPosition
                    );

                selectedAreaIndex =
                    candidateAreaIndex;

                selectedGridPosition =
                    candidateGridPosition;

                selectedWorldPosition =
                    candidateWorldPosition;

                if (IsFarEnoughFromOthers(
                        candidateWorldPosition,
                        usedPositions))
                {
                    validPositionFound = true;
                    break;
                }
            }

            if (!validPositionFound)
            {
                Debug.LogWarning(
                    "[TrashSpawner] No valid deterministic position " +
                    "was found for object " +
                    objectIndex +
                    ". The object will be skipped.",
                    this
                );

                continue;
            }

            DeterministicTrashSpawnData spawnData =
                new DeterministicTrashSpawnData
                {
                    areaIndex =
                        selectedAreaIndex,

                    spriteIndex =
                        spriteIndex,

                    gridPosition =
                        selectedGridPosition,

                    worldXMillimeters =
                        Mathf.RoundToInt(
                            selectedWorldPosition.x *
                            1000f
                        ),

                    worldYMillimeters =
                        Mathf.RoundToInt(
                            selectedWorldPosition.y *
                            1000f
                        ),

                    worldZMillimeters =
                        Mathf.RoundToInt(
                            selectedWorldPosition.z *
                            1000f
                        )
                };

            layout.Add(spawnData);

            usedPositions.Add(
                selectedWorldPosition
            );
        }

        return layout;
    }

    private void CreateTrashObject(
        DeterministicTrashSpawnData spawnData,
        int objectIndex,
        int generationSeed
    )
    {
        GameObject trashObject =
            Instantiate(
                trashPrefab,
                spawnData.WorldPosition,
                Quaternion.identity,
                spawnedTrashParent
            );

        trashObject.name =
            trashPrefab.name +
            "_Seed_" +
            generationSeed +
            "_Area_" +
            spawnData.areaIndex +
            "_Index_" +
            objectIndex;

        TrashItem trashItem =
            trashObject.GetComponent<TrashItem>();

        if (trashItem == null)
        {
            Debug.LogError(
                "[TrashSpawner] The trash prefab does not " +
                "contain a TrashItem component.",
                trashObject
            );

            DestroyObject(trashObject);
            return;
        }

        SpriteRenderer spriteRenderer =
            trashObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite =
                trashSprites[
                    spawnData.spriteIndex
                ];
        }

        trashItem.Setup(this);

        spawnedTrash.Add(
            trashItem
        );
    }

    private bool IsFarEnoughFromOthers(
        Vector3 candidate,
        List<Vector3> usedPositions
    )
    {
        float minimumSquaredDistance =
            minDistanceBetweenTrash *
            minDistanceBetweenTrash;

        foreach (Vector3 position in usedPositions)
        {
            float squaredDistance =
                (candidate - position).sqrMagnitude;

            if (squaredDistance <
                minimumSquaredDistance)
            {
                return false;
            }
        }

        return true;
    }

    private static Vector3 QuantizeWorldPosition(
        Vector3 position
    )
    {
        return new Vector3(
            Mathf.Round(position.x * 1000f) / 1000f,
            Mathf.Round(position.y * 1000f) / 1000f,
            Mathf.Round(position.z * 1000f) / 1000f
        );
    }

    [ContextMenu("Verify Determinism")]
    public void VerifyDeterminism()
    {
        try
        {
            int verificationSeed =
                ResolveGenerationSeed();

            List<DeterministicTrashSpawnData> firstLayout =
                BuildLayout(
                    verificationSeed
                );

            List<DeterministicTrashSpawnData> secondLayout =
                BuildLayout(
                    verificationSeed
                );

            string firstHash =
                CalculateLayoutHash(
                    firstLayout
                );

            string secondHash =
                CalculateLayoutHash(
                    secondLayout
                );

            bool exactMatch =
                AreLayoutsIdentical(
                    firstLayout,
                    secondLayout
                );

            int differentSeed =
                ResolveDifferentGenerationSeed();

            List<DeterministicTrashSpawnData> differentLayout =
                BuildLayout(
                    differentSeed
                );

            string differentHash =
                CalculateLayoutHash(
                    differentLayout
                );

            if (exactMatch &&
                firstHash == secondHash)
            {
                string seedInformation =
                    useMasterSeed
                        ? "Master seed: " +
                          GetSeedManager().MasterSeed +
                          "\nGenerator ID: " +
                          generatorId +
                          "\nRoom ID: " +
                          roomId +
                          "\nDerived seed: " +
                          verificationSeed
                        : "Standalone seed: " +
                          verificationSeed;

                Debug.Log(
                    "[TrashSpawner] DETERMINISM TEST PASSED\n" +
                    seedInformation + "\n" +
                    "First hash: " + firstHash + "\n" +
                    "Second hash: " + secondHash + "\n" +
                    "Different seed hash: " +
                    differentHash + "\n" +
                    "The same master seed and room ID " +
                    "generated the exact same amount, " +
                    "areas, positions and sprites.",
                    this
                );

                return;
            }

            Debug.LogError(
                "[TrashSpawner] DETERMINISM TEST FAILED\n" +
                "First hash: " + firstHash + "\n" +
                "Second hash: " + secondHash,
                this
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[TrashSpawner] Determinism verification failed: " +
                exception.Message,
                this
            );
        }
    }

    private static bool AreLayoutsIdentical(
        List<DeterministicTrashSpawnData> first,
        List<DeterministicTrashSpawnData> second
    )
    {
        if (first == null ||
            second == null)
        {
            return first == second;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        for (int index = 0;
             index < first.Count;
             index++)
        {
            DeterministicTrashSpawnData firstItem =
                first[index];

            DeterministicTrashSpawnData secondItem =
                second[index];

            if (firstItem.areaIndex !=
                    secondItem.areaIndex ||
                firstItem.spriteIndex !=
                    secondItem.spriteIndex ||
                firstItem.gridPosition !=
                    secondItem.gridPosition ||
                firstItem.worldXMillimeters !=
                    secondItem.worldXMillimeters ||
                firstItem.worldYMillimeters !=
                    secondItem.worldYMillimeters ||
                firstItem.worldZMillimeters !=
                    secondItem.worldZMillimeters)
            {
                return false;
            }
        }

        return true;
    }

    public static string CalculateLayoutHash(
        List<DeterministicTrashSpawnData> layout
    )
    {
        unchecked
        {
            ulong hash =
                14695981039346656037UL;

            AddIntegerToHash(
                ref hash,
                layout.Count
            );

            foreach (
                DeterministicTrashSpawnData item
                in layout)
            {
                AddIntegerToHash(
                    ref hash,
                    item.areaIndex
                );

                AddIntegerToHash(
                    ref hash,
                    item.spriteIndex
                );

                AddIntegerToHash(
                    ref hash,
                    item.gridPosition.x
                );

                AddIntegerToHash(
                    ref hash,
                    item.gridPosition.y
                );

                AddIntegerToHash(
                    ref hash,
                    item.worldXMillimeters
                );

                AddIntegerToHash(
                    ref hash,
                    item.worldYMillimeters
                );

                AddIntegerToHash(
                    ref hash,
                    item.worldZMillimeters
                );
            }

            return "0x" +
                   hash.ToString("X16");
        }
    }

    private static void AddIntegerToHash(
        ref ulong hash,
        int value
    )
    {
        unchecked
        {
            uint unsignedValue =
                (uint)value;

            const ulong prime =
                1099511628211UL;

            for (int byteIndex = 0;
                 byteIndex < 4;
                 byteIndex++)
            {
                byte currentByte =
                    (byte)(
                        unsignedValue & 0xFF
                    );

                hash ^= currentByte;
                hash *= prime;

                unsignedValue >>= 8;
            }
        }
    }

    public bool RemoveTrash(
        TrashItem trash
    )
    {
        if (trash == null)
        {
            return false;
        }

        bool wasRegistered =
            spawnedTrash.Remove(trash);

        if (!wasRegistered)
        {
            Debug.LogError(
                "[TrashSpawner] The object was not registered: " +
                trash.gameObject.name,
                trash
            );

            return false;
        }

        DestroyObject(
            trash.gameObject
        );

        return true;
    }

    public int GetRemainingTrash()
    {
        return spawnedTrash.Count;
    }

    public void ClearTrash()
    {
        for (int index =
                 spawnedTrash.Count - 1;
             index >= 0;
             index--)
        {
            TrashItem trash =
                spawnedTrash[index];

            if (trash != null)
            {
                DestroyObject(
                    trash.gameObject
                );
            }
        }

        spawnedTrash.Clear();

        TotalSpawnedTrash = 0;
    }

    public void SetTrashRange(
        int min,
        int max
    )
    {
        minTrash =
            Mathf.Max(0, min);

        maxTrash =
            Mathf.Max(
                minTrash,
                max
            );
    }

    public void SetSeed(int newSeed)
    {
        standaloneSeed = newSeed;
    }

    public void SetSeedAndSpawn(int newSeed)
    {
        standaloneSeed = newSeed;

        SpawnTrash(
            standaloneSeed
        );
    }

    public string GetCurrentLayoutHash()
    {
        int generationSeed =
            ResolveGenerationSeed();

        List<DeterministicTrashSpawnData> layout =
            BuildLayout(
                generationSeed
            );

        return CalculateLayoutHash(
            layout
        );
    }

    private void EnsureSpawnAreas()
    {
        if (autoCollectSpawnAreas ||
            spawnAreas == null ||
            spawnAreas.Length == 0)
        {
            RefreshSpawnAreas();
        }
        else
        {
            Array.Sort(
                spawnAreas,
                CompareSpawnAreas
            );
        }
    }

    private static int CompareSpawnAreas(
        TrashSpawnArea first,
        TrashSpawnArea second
    )
    {
        if (ReferenceEquals(first, second))
        {
            return 0;
        }

        if (first == null)
        {
            return 1;
        }

        if (second == null)
        {
            return -1;
        }

        int firstSiblingIndex =
            first.transform.GetSiblingIndex();

        int secondSiblingIndex =
            second.transform.GetSiblingIndex();

        int siblingComparison =
            firstSiblingIndex.CompareTo(
                secondSiblingIndex
            );

        if (siblingComparison != 0)
        {
            return siblingComparison;
        }

        return string.CompareOrdinal(
            first.gameObject.name,
            second.gameObject.name
        );
    }

    private void ValidateConfiguration()
    {
        if (trashPrefab == null)
        {
            throw new InvalidOperationException(
                "Trash prefab is not assigned."
            );
        }

        if (trashPrefab.GetComponent<TrashItem>() == null)
        {
            throw new InvalidOperationException(
                "Trash prefab does not contain TrashItem."
            );
        }

        if (trashSprites == null ||
            trashSprites.Length == 0)
        {
            throw new InvalidOperationException(
                "No trash sprites are assigned."
            );
        }

        for (int index = 0;
             index < trashSprites.Length;
             index++)
        {
            if (trashSprites[index] == null)
            {
                throw new InvalidOperationException(
                    "Trash sprite at index " +
                    index +
                    " is null."
                );
            }
        }

        if (spawnAreas == null ||
            spawnAreas.Length == 0)
        {
            throw new InvalidOperationException(
                "No TrashSpawnArea components were found."
            );
        }

        for (int index = 0;
             index < spawnAreas.Length;
             index++)
        {
            if (spawnAreas[index] == null)
            {
                throw new InvalidOperationException(
                    "Spawn area at index " +
                    index +
                    " is null."
                );
            }
        }

        if (positionGridSize <= 0f)
        {
            throw new InvalidOperationException(
                "Position grid size must be greater than zero."
            );
        }

        if (maxPositionAttemptsPerObject <= 0)
        {
            throw new InvalidOperationException(
                "Maximum position attempts must be greater than zero."
            );
        }
    }

    private static void DestroyObject(
        GameObject target
    )
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private int ResolveGenerationSeed()
    {
        if (!useMasterSeed)
        {
            return standaloneSeed;
        }

        ProceduralSeedManager manager =
            GetSeedManager();

        ValidateProceduralIdentifiers();

        return manager.GetDerivedSeed(
            generatorId,
            roomId
        );
    }

    private int ResolveDifferentGenerationSeed()
    {
        if (!useMasterSeed)
        {
            return unchecked(
                standaloneSeed + 1
            );
        }

        ProceduralSeedManager manager =
            GetSeedManager();

        ValidateProceduralIdentifiers();

        int differentMasterSeed =
            unchecked(
                manager.MasterSeed + 1
            );

        return manager.GetDerivedSeedFromMaster(
            differentMasterSeed,
            generatorId,
            roomId
        );
    }

    private ProceduralSeedManager GetSeedManager()
    {
        if (seedManager != null)
        {
            return seedManager;
        }

        if (ProceduralSeedManager.Instance != null)
        {
            seedManager =
                ProceduralSeedManager.Instance;

            return seedManager;
        }

        throw new InvalidOperationException(
            "Use Master Seed is enabled, but no " +
            "ProceduralSeedManager was found."
        );
    }

    private void ValidateProceduralIdentifiers()
    {
        if (string.IsNullOrWhiteSpace(generatorId))
        {
            throw new InvalidOperationException(
                "Generator ID cannot be empty."
            );
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new InvalidOperationException(
                "Room ID cannot be empty."
            );
        }
    }

    public void SetRoomId(string newRoomId)
    {
        if (string.IsNullOrWhiteSpace(newRoomId))
        {
            throw new ArgumentException(
                "Room ID cannot be empty.",
                nameof(newRoomId)
            );
        }

        roomId = newRoomId.Trim();
    }

    public void SetRoomIdAndSpawn(
        string newRoomId
    )
    {
        SetRoomId(newRoomId);
        SpawnTrash();
    }

    private void OnValidate()
    {
        minTrash =
            Mathf.Max(0, minTrash);

        maxTrash =
            Mathf.Max(
                minTrash,
                maxTrash
            );

        positionGridSize =
            Mathf.Max(
                0.001f,
                positionGridSize
            );

        minDistanceBetweenTrash =
            Mathf.Max(
                0f,
                minDistanceBetweenTrash
            );

        maxPositionAttemptsPerObject =
            Mathf.Max(
                1,
                maxPositionAttemptsPerObject
            );
    }
}