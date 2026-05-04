public static class PlayerSpawnMemory
{
    public static string nextSpawnId = "";

    public static void SetNextSpawn(string spawnId)
    {
        nextSpawnId = spawnId;
    }

    public static void Clear()
    {
        nextSpawnId = "";
    }
}