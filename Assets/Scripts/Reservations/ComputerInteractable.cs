using UnityEngine;

public class ComputerInteractable : MonoBehaviour
{
    public CheckInFlowController flowController;
    private bool canUse;

    public void SetEnabled(bool value)
    {
        canUse = value;
    }

    public void Interact()
    {
        if (!canUse) return;

        flowController.OpenMap();
    }
}