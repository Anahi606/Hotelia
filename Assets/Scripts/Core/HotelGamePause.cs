using System;
using UnityEngine;

public static class HotelGamePause
{
    private static int pauseRequests = 0;

    public static bool IsPaused => pauseRequests > 0;

    public static event Action<bool> OnPauseChanged;

    public static void RequestPause()
    {
        pauseRequests++;

        if (pauseRequests == 1)
        {
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
        }
    }

    public static void ReleasePause()
    {
        pauseRequests = Mathf.Max(0, pauseRequests - 1);

        if (pauseRequests == 0)
        {
            Time.timeScale = 1f;
            OnPauseChanged?.Invoke(false);
        }
    }

    public static void ForceResume()
    {
        pauseRequests = 0;
        Time.timeScale = 1f;
        OnPauseChanged?.Invoke(false);
    }
}