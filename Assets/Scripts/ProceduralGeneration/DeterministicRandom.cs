using System;

[Serializable]
public struct DeterministicRandom
{
    private ulong state;

    public DeterministicRandom(int seed, int streamId = 0)
    {
        unchecked
        {
            ulong seedValue = (uint)seed;
            ulong streamValue = (uint)streamId;

            state =
                seedValue ^
                (0xD1B54A32D192ED03UL * (streamValue + 1UL));
        }
    }

    private ulong NextUInt64()
    {
        unchecked
        {
            state += 0x9E3779B97F4A7C15UL;

            ulong value = state;

            value =
                (value ^ (value >> 30)) *
                0xBF58476D1CE4E5B9UL;

            value =
                (value ^ (value >> 27)) *
                0x94D049BB133111EBUL;

            return value ^ (value >> 31);
        }
    }

    public uint NextUInt()
    {
        return (uint)(NextUInt64() >> 32);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                "maxExclusive must be greater than minInclusive."
            );
        }

        unchecked
        {
            uint range =
                (uint)(maxExclusive - minInclusive);

            uint threshold =
                (0u - range) % range;

            uint value;

            do
            {
                value = NextUInt();
            }
            while (value < threshold);

            return minInclusive +
                   (int)(value % range);
        }
    }

    public float Range(float minInclusive, float maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            float temporary = minInclusive;
            minInclusive = maxInclusive;
            maxInclusive = temporary;
        }

        uint randomValue = NextUInt() >> 8;

        float normalizedValue =
            randomValue * (1f / 16777216f);

        return minInclusive +
               ((maxInclusive - minInclusive) * normalizedValue);
    }

    public bool Chance(float probability)
    {
        probability = Math.Clamp(probability, 0f, 1f);

        return Range(0f, 1f) < probability;
    }
}