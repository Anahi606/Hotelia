using UnityEngine;

public class YSortOrder : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int offset = 0;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + offset;
    }
}