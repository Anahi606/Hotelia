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

        Debug.Log(
            $"[PauseWhilePanelActive] REQUEST | " +
            $"Object: {gameObject.name} | ID: {GetInstanceID()}"
        );
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

        hasPauseRequest = false;
        HotelGamePause.ReleasePause();

        Debug.Log(
            $"[PauseWhilePanelActive] RELEASE | " +
            $"Object: {gameObject.name} | ID: {GetInstanceID()}"
        );
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}