using UnityEngine;

public class TrashSpawnArea : MonoBehaviour
{
    [SerializeField]
    private Vector2 size = new Vector2(2f, 2f);

    public Vector2 Size => size;

    public Vector3 GetRandomPosition()
    {
        Vector3 center = transform.position;

        float randomX =
            UnityEngine.Random.Range(
                -size.x / 2f,
                size.x / 2f
            );

        float randomY =
            UnityEngine.Random.Range(
                -size.y / 2f,
                size.y / 2f
            );

        return new Vector3(
            center.x + randomX,
            center.y + randomY,
            0f
        );
    }

    public Vector2Int GetDeterministicGridPosition(
        ref DeterministicRandom random,
        float gridSize
    )
    {
        if (gridSize <= 0f)
        {
            gridSize = 0.05f;
        }

        int horizontalCells = Mathf.Max(
            0,
            Mathf.FloorToInt(
                size.x * 0.5f / gridSize
            )
        );

        int verticalCells = Mathf.Max(
            0,
            Mathf.FloorToInt(
                size.y * 0.5f / gridSize
            )
        );

        int gridX = random.Range(
            -horizontalCells,
            horizontalCells + 1
        );

        int gridY = random.Range(
            -verticalCells,
            verticalCells + 1
        );

        return new Vector2Int(gridX, gridY);
    }

    public Vector3 GridPositionToLocalPosition(
        Vector2Int gridPosition,
        float gridSize
    )
    {
        return new Vector3(
            gridPosition.x * gridSize,
            gridPosition.y * gridSize,
            0f
        );
    }

    public Vector3 GridPositionToWorldPosition(
        Vector2Int gridPosition,
        float gridSize
    )
    {
        Vector3 localPosition =
            GridPositionToLocalPosition(
                gridPosition,
                gridSize
            );

        return transform.TransformPoint(localPosition);
    }

    public Vector3 GetPositionFromSeed(
        int seed,
        float gridSize = 0.05f
    )
    {
        DeterministicRandom random =
            new DeterministicRandom(seed);

        Vector2Int gridPosition =
            GetDeterministicGridPosition(
                ref random,
                gridSize
            );

        return GridPositionToWorldPosition(
            gridPosition,
            gridSize
        );
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(0.01f, size.x);
        size.y = Mathf.Max(0.01f, size.y);
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            transform.localToWorldMatrix;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                size.x,
                size.y,
                0f
            )
        );

        Gizmos.matrix = previousMatrix;
    }
}