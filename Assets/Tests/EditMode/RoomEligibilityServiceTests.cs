using System;
using Hotelia.Core;
using NUnit.Framework;

public class RoomEligibilityServiceTests
{
    [TestCase(2, false, 2, false, true)]
    [TestCase(1, false, 2, false, false)]
    [TestCase(2, false, 1, true, false)]
    [TestCase(2, true, 1, true, true)]
    public void CanAssignRoom_ReturnsExpectedResult(
        int availableBeds,
        bool roomIsAccessible,
        int guestCount,
        bool guestNeedsAccessibility,
        bool expectedResult)
    {
        bool result = RoomEligibilityService.CanAssignRoom(
            availableBeds,
            roomIsAccessible,
            guestCount,
            guestNeedsAccessibility
        );

        Assert.AreEqual(expectedResult, result);
    }

    [Test]
    public void CanAssignRoom_WithZeroGuests_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            RoomEligibilityService.CanAssignRoom(
                availableBeds: 2,
                roomIsAccessible: true,
                guestCount: 0,
                guestNeedsAccessibility: false
            );
        });
    }

    [Test]
    public void CanAssignRoom_AccessibleGuestAndAccessibleRoom_ReturnsTrue()
    {
        bool result = RoomEligibilityService.CanAssignRoom(
            availableBeds: 2,
            roomIsAccessible: true,
            guestCount: 1,
            guestNeedsAccessibility: true
        );

        Assert.IsTrue(result);
    }

    [Test]
    public void CanAssignRoom_InsufficientBeds_ReturnsFalse()
    {
        bool result = RoomEligibilityService.CanAssignRoom(
            availableBeds: 1,
            roomIsAccessible: true,
            guestCount: 3,
            guestNeedsAccessibility: false
        );

        Assert.IsFalse(result);
    }
}