using UnityEngine;

public class CloudScroller : MonoBehaviour
{
    public float speed = 2f;
    public float resetX;
    public float exitX;

    [Tooltip("A qué distancia vertical de la cámara se activan las nubes")]
    public float activationDistance = 10f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (transform.position.x < exitX)
        {
            transform.position = new Vector3(resetX, transform.position.y, transform.position.z);
            return;
        }

        float verticalDistance = Mathf.Abs(mainCamera.transform.position.y - transform.position.y);
        if (verticalDistance > activationDistance) return;

        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}