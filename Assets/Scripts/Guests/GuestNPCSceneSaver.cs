using UnityEngine;

public static class GuestNPCSceneSaver
{
    public static void SaveAllVisibleGuests()
    {
        GuestNPC[] guests = Object.FindObjectsByType<GuestNPC>(FindObjectsSortMode.None);

        foreach (GuestNPC guest in guests)
        {
            if (guest != null)
            {
                guest.SaveStateNow();
            }
        }
    }
}