using System;

namespace Hotelia.Core
{
    public static class RoomEligibilityService
    {
        public static bool CanAssignRoom(
            int availableBeds,
            bool roomIsAccessible,
            int guestCount,
            bool guestNeedsAccessibility)
        {
            if (availableBeds < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableBeds),
                    "The room must have at least one bed."
                );
            }

            if (guestCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(guestCount),
                    "There must be at least one guest."
                );
            }

            if (availableBeds < guestCount)
                return false;

            if (guestNeedsAccessibility && !roomIsAccessible)
                return false;

            return true;
        }
    }
}