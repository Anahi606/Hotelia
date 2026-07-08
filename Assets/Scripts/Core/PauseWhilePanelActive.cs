using UnityEngine;

public class PauseWhilePanelActive : MonoBehaviour
{
    private bool hasPauseRequest;

    private void OnEnable()
    {
        if (hasPauseRequest)
            return;

        HotelGamePause.RequestPause();
        hasPauseRequest = true;
    }

    private void OnDisable()
    {
        ReleasePause();
    }

    private void OnDestroy()
    {
        ReleasePause();
    }

    private void ReleasePause()
    {
        if (!hasPauseRequest)
            return;

        HotelGamePause.ReleasePause();
        hasPauseRequest = false;
    }
}