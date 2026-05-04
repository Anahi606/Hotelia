using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RandomGridRoamer : MonoBehaviour
{
    [Header("Area")]
    public GuestArea currentArea;
    public bool autoFindZones = true;

    [Header("Grid")]
    public float cellSize = 1f;
    public Vector2 gridOrigin = Vector2.zero;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float arrivalDistance = 0.08f;

    [Header("Waiting")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 5f;
    public float minStartDelay = 0f;
    public float maxStartDelay = 3f;

    [Header("Collision")]
    public LayerMask obstacleMask;
    public Vector2 collisionCheckSize = new Vector2(0.5f, 0.5f);

    [Header("Pathfinding")]
    public int maxSearchIterations = 900;
    public int nearbySearchRadius = 5;

    private Rigidbody2D rb;
    private NPCRoamZone[] zones;
    private Coroutine roamRoutine;
    private Vector3 forcedDestination;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (autoFindZones)
            FindZones();

        StartRoaming();
    }

    public void Initialize(GuestArea area)
    {
        currentArea = area;

        if (autoFindZones)
            FindZones();
    }

    private void StartRoaming()
    {
        if (roamRoutine != null)
            StopCoroutine(roamRoutine);

        roamRoutine = StartCoroutine(RoamLoop());
    }

    private void FindZones()
    {
        NPCRoamZone[] allZones = FindObjectsByType<NPCRoamZone>(FindObjectsSortMode.None);
        List<NPCRoamZone> validZones = new List<NPCRoamZone>();

        foreach (NPCRoamZone zone in allZones)
        {
            if (zone != null && zone.area == currentArea)
                validZones.Add(zone);
        }

        zones = validZones.ToArray();

        if (zones.Length == 0)
            Debug.LogWarning(gameObject.name + " no encontró NPCRoamZone para área: " + currentArea);
    }

    public void MoveToDestination(Vector3 destination)
    {
        forcedDestination = destination;

        if (roamRoutine != null)
            StopCoroutine(roamRoutine);

        roamRoutine = StartCoroutine(MoveToForcedDestination());
    }

    private IEnumerator MoveToForcedDestination()
    {
        NPCRoamZone zone = GetZoneContainingNPC();

        if (zone == null)
        {
            zone = GetClosestZoneToNPC();

            if (zone == null)
            {
                Debug.LogWarning(gameObject.name + " no encontró zona para moverse al destino.");
                StartRoaming();
                yield break;
            }

            rb.position = zone.GetClosestPoint(rb.position);
        }

        Vector2 safeDestination = forcedDestination;

        if (!zone.ContainsWorldPosition(safeDestination))
            safeDestination = zone.GetClosestPoint(safeDestination);

        Vector2Int startCell = WorldToCell(rb.position);
        Vector2Int targetCell = WorldToCell(safeDestination);

        if (!IsCellWalkable(targetCell, zone))
        {
            if (!TryFindNearbyWalkableCell(safeDestination, zone, out targetCell))
            {
                Debug.LogWarning(gameObject.name + " no encontró celda caminable cerca del trigger.");
                StartRoaming();
                yield break;
            }
        }

        List<Vector2Int> path = FindPath(startCell, targetCell, zone);

        if (path != null && path.Count > 0)
        {
            yield return MoveAlongPath(path);
        }
        else
        {
            Debug.LogWarning(gameObject.name + " no encontró camino hacia el trigger.");
        }

        StartRoaming();
    }

    private IEnumerator RoamLoop()
    {
        yield return new WaitForSeconds(Random.Range(minStartDelay, maxStartDelay));

        while (true)
        {
            if (zones == null || zones.Length == 0)
            {
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(1f);
                continue;
            }

            NPCRoamZone zone = GetZoneContainingNPC();

            if (zone == null)
            {
                zone = GetClosestZoneToNPC();

                if (zone == null)
                {
                    rb.linearVelocity = Vector2.zero;
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                rb.position = zone.GetClosestPoint(rb.position);
            }

            Vector2Int startCell = WorldToCell(rb.position);

            if (!IsCellWalkable(startCell, zone))
            {
                if (TryFindNearbyWalkableCell(rb.position, zone, out Vector2Int fixedCell))
                {
                    rb.position = CellToWorld(fixedCell);
                    startCell = fixedCell;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                    yield return new WaitForSeconds(1f);
                    continue;
                }
            }

            if (!TryGetRandomWalkableCell(zone, out Vector2Int targetCell))
            {
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(1f);
                continue;
            }

            List<Vector2Int> path = FindPath(startCell, targetCell, zone);

            if (path != null && path.Count > 0)
                yield return MoveAlongPath(path);

            rb.linearVelocity = Vector2.zero;

            float wait = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(wait);
        }
    }

    private NPCRoamZone GetZoneContainingNPC()
    {
        if (zones == null || zones.Length == 0)
            return null;

        foreach (NPCRoamZone zone in zones)
        {
            if (zone != null && zone.ContainsWorldPosition(rb.position))
                return zone;
        }

        return null;
    }

    private NPCRoamZone GetClosestZoneToNPC()
    {
        if (zones == null || zones.Length == 0)
            return null;

        NPCRoamZone closestZone = null;
        float closestDistance = float.MaxValue;

        foreach (NPCRoamZone zone in zones)
        {
            if (zone == null)
                continue;

            Vector2 closestPoint = zone.GetClosestPoint(rb.position);
            float distance = Vector2.Distance(rb.position, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestZone = zone;
            }
        }

        return closestZone;
    }

    private bool TryGetRandomWalkableCell(NPCRoamZone zone, out Vector2Int cell)
    {
        Bounds bounds = zone.Bounds;

        for (int i = 0; i < 60; i++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);

            Vector2Int randomCell = WorldToCell(new Vector2(randomX, randomY));

            if (IsCellWalkable(randomCell, zone))
            {
                cell = randomCell;
                return true;
            }
        }

        cell = Vector2Int.zero;
        return false;
    }

    private bool TryFindNearbyWalkableCell(Vector2 targetPosition, NPCRoamZone zone, out Vector2Int validCell)
    {
        Vector2Int centerCell = WorldToCell(targetPosition);

        for (int radius = 1; radius <= nearbySearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int testCell = new Vector2Int(centerCell.x + x, centerCell.y + y);

                    if (IsCellWalkable(testCell, zone))
                    {
                        validCell = testCell;
                        return true;
                    }
                }
            }
        }

        validCell = Vector2Int.zero;
        return false;
    }

    private IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 targetPosition = CellToWorld(path[i]);

            while (Vector2.Distance(rb.position, targetPosition) > arrivalDistance)
            {
                Vector2 direction = (targetPosition - rb.position).normalized;
                rb.linearVelocity = direction * moveSpeed;

                yield return new WaitForFixedUpdate();
            }

            rb.linearVelocity = Vector2.zero;
            rb.position = targetPosition;
        }
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, NPCRoamZone zone)
    {
        List<Vector2Int> open = new List<Vector2Int>();
        HashSet<Vector2Int> closed = new HashSet<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> gCost = new Dictionary<Vector2Int, int>();

        open.Add(start);
        gCost[start] = 0;

        int iterations = 0;

        while (open.Count > 0)
        {
            iterations++;

            if (iterations > maxSearchIterations)
                break;

            Vector2Int current = GetLowestCostCell(open, target, gCost);

            if (current == target)
                return ReconstructPath(cameFrom, current);

            open.Remove(current);
            closed.Add(current);

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (closed.Contains(neighbor))
                    continue;

                if (!IsCellWalkable(neighbor, zone))
                    continue;

                int tentativeGCost = gCost[current] + 1;

                if (!open.Contains(neighbor))
                    open.Add(neighbor);
                else if (tentativeGCost >= gCost[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gCost[neighbor] = tentativeGCost;
            }
        }

        return null;
    }

    private Vector2Int GetLowestCostCell(
        List<Vector2Int> open,
        Vector2Int target,
        Dictionary<Vector2Int, int> gCost)
    {
        Vector2Int bestCell = open[0];
        int bestCost = GetFCost(bestCell, target, gCost);

        for (int i = 1; i < open.Count; i++)
        {
            int cost = GetFCost(open[i], target, gCost);

            if (cost < bestCost)
            {
                bestCost = cost;
                bestCell = open[i];
            }
        }

        return bestCell;
    }

    private int GetFCost(Vector2Int cell, Vector2Int target, Dictionary<Vector2Int, int> gCost)
    {
        int g = gCost.ContainsKey(cell) ? gCost[cell] : 999999;
        int h = Mathf.Abs(cell.x - target.x) + Mathf.Abs(cell.y - target.y);

        return g + h;
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        return path;
    }

    private Vector2Int[] GetNeighbors(Vector2Int cell)
    {
        return new Vector2Int[]
        {
            new Vector2Int(cell.x + 1, cell.y),
            new Vector2Int(cell.x - 1, cell.y),
            new Vector2Int(cell.x, cell.y + 1),
            new Vector2Int(cell.x, cell.y - 1)
        };
    }

    private bool IsCellWalkable(Vector2Int cell, NPCRoamZone zone)
    {
        Vector2 worldPosition = CellToWorld(cell);

        if (!zone.ContainsWorldPosition(worldPosition))
            return false;

        Collider2D hit = Physics2D.OverlapBox(
            worldPosition,
            collisionCheckSize,
            0f,
            obstacleMask
        );

        return hit == null;
    }

    private Vector2Int WorldToCell(Vector2 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPosition.y - gridOrigin.y) / cellSize);

        return new Vector2Int(x, y);
    }

    private Vector2 CellToWorld(Vector2Int cell)
    {
        float x = gridOrigin.x + cell.x * cellSize;
        float y = gridOrigin.y + cell.y * cellSize;

        return new Vector2(x, y);
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}