using UnityEngine;

public class TrashSpawnArea : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(2f, 2f);

    public Vector3 GetRandomPosition()
    {
        Vector3 center = transform.position;

        float randomX = Random.Range(-size.x / 2f, size.x / 2f);
        float randomY = Random.Range(-size.y / 2f, size.y / 2f);

        return new Vector3(center.x + randomX, center.y + randomY, 0f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}