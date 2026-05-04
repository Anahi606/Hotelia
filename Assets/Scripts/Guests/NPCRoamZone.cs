using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class NPCRoamZone : MonoBehaviour
{
    public GuestArea area;
    public string zoneId;

    private BoxCollider2D zoneCollider;

    public Bounds Bounds => zoneCollider.bounds;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;
    }

    public bool ContainsWorldPosition(Vector2 worldPosition)
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<BoxCollider2D>();

        return zoneCollider.OverlapPoint(worldPosition);
    }

    public Vector2 GetClosestPoint(Vector2 worldPosition)
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<BoxCollider2D>();

        return zoneCollider.ClosestPoint(worldPosition);
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();

        if (box == null)
            return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(box.bounds.center, box.bounds.size);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}