using System;
using System.Text;
using UnityEngine;

public sealed class ProceduralSeedManager : MonoBehaviour
{
    public static ProceduralSeedManager Instance { get; private set; }

    [Header("Global Procedural Generation")]
    [SerializeField]
    private int masterSeed = 12345;

    [SerializeField]
    private bool persistBetweenScenes = true;

    public int MasterSeed => masterSeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int GetDerivedSeed(
        string generatorId,
        string instanceId
    )
    {
        ValidateIdentifier(
            generatorId,
            nameof(generatorId)
        );

        ValidateIdentifier(
            instanceId,
            nameof(instanceId)
        );

        return StableSeedUtility.Derive(
            masterSeed,
            generatorId.Trim(),
            instanceId.Trim()
        );
    }

    public int GetDerivedSeedFromMaster(
        int sourceMasterSeed,
        string generatorId,
        string instanceId
    )
    {
        ValidateIdentifier(
            generatorId,
            nameof(generatorId)
        );

        ValidateIdentifier(
            instanceId,
            nameof(instanceId)
        );

        return StableSeedUtility.Derive(
            sourceMasterSeed,
            generatorId.Trim(),
            instanceId.Trim()
        );
    }

    public void SetMasterSeed(int newMasterSeed)
    {
        masterSeed = newMasterSeed;
    }

    private static void ValidateIdentifier(
        string value,
        string parameterName
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "The procedural generation identifier cannot be empty.",
                parameterName
            );
        }
    }
}

public static class StableSeedUtility
{
    private const uint FnvOffsetBasis = 2166136261u;
    private const uint FnvPrime = 16777619u;

    public static int Derive(
        int masterSeed,
        params string[] identifiers
    )
    {
        unchecked
        {
            uint hash = FnvOffsetBasis;

            AddInteger(
                ref hash,
                masterSeed
            );

            if (identifiers != null)
            {
                foreach (string identifier in identifiers)
                {
                    AddString(
                        ref hash,
                        identifier ?? string.Empty
                    );

                    AddByte(
                        ref hash,
                        0xFF
                    );
                }
            }

            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            return (int)hash;
        }
    }

    private static void AddInteger(
        ref uint hash,
        int value
    )
    {
        unchecked
        {
            uint unsignedValue = (uint)value;

            for (int byteIndex = 0;
                 byteIndex < 4;
                 byteIndex++)
            {
                AddByte(
                    ref hash,
                    (byte)(unsignedValue & 0xFF)
                );

                unsignedValue >>= 8;
            }
        }
    }

    private static void AddString(
        ref uint hash,
        string value
    )
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(value);

        foreach (byte currentByte in bytes)
        {
            AddByte(
                ref hash,
                currentByte
            );
        }
    }

    private static void AddByte(
        ref uint hash,
        byte value
    )
    {
        unchecked
        {
            hash ^= value;
            hash *= FnvPrime;
        }
    }
}