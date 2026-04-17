using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private int activePanelCount = 0;

    public bool IsAnyPanelOpen => activePanelCount > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterPanelOpen()
    {
        activePanelCount++;
    }

    public void RegisterPanelClose()
    {
        activePanelCount = Mathf.Max(0, activePanelCount - 1);
    }
}