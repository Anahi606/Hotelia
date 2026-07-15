using System;
using Hotelia.Core;
using NUnit.Framework;

public class CleaningRulesTests
{
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void CanStartCleaning_ReturnsExpectedResult(
        bool roomIsDirty,
        bool expectedResult)
    {
        bool result =
            CleaningRules.CanStartCleaning(roomIsDirty);

        Assert.AreEqual(expectedResult, result);
    }

    [Test]
    public void EvaluateProgress_AllTasksCompleted_CalculatesExpectedResult()
    {
        CleaningProgressResult result =
            CleaningRules.EvaluateProgress(
                totalTrash: 4,
                remainingTrash: 0,
                totalBeds: 2,
                madeBeds: 2,
                currentTime: 30f,
                totalTime: 60f
            );

        Assert.AreEqual(4, result.CleanedTrash);
        Assert.AreEqual(0, result.RemainingTrash);
        Assert.AreEqual(0, result.RemainingBeds);
        Assert.AreEqual(0, result.TotalErrors);

        Assert.AreEqual(100, result.TrashScore);
        Assert.AreEqual(100, result.BedScore);
        Assert.AreEqual(50, result.TimeScore);
        Assert.AreEqual(90, result.FinalScore);

        Assert.IsTrue(result.CompletedEverything);
    }

    [Test]
    public void EvaluateProgress_IncompleteCleaning_RegistersErrors()
    {
        CleaningProgressResult result =
            CleaningRules.EvaluateProgress(
                totalTrash: 4,
                remainingTrash: 2,
                totalBeds: 2,
                madeBeds: 1,
                currentTime: 0f,
                totalTime: 60f
            );

        Assert.AreEqual(2, result.CleanedTrash);
        Assert.AreEqual(2, result.RemainingTrash);
        Assert.AreEqual(1, result.RemainingBeds);
        Assert.AreEqual(3, result.TotalErrors);

        Assert.AreEqual(50, result.TrashScore);
        Assert.AreEqual(50, result.BedScore);
        Assert.AreEqual(0, result.TimeScore);
        Assert.AreEqual(40, result.FinalScore);

        Assert.IsFalse(result.CompletedEverything);
    }

    [Test]
    public void EvaluateProgress_NoTrashOrBeds_IsCompleted()
    {
        CleaningProgressResult result =
            CleaningRules.EvaluateProgress(
                totalTrash: 0,
                remainingTrash: 0,
                totalBeds: 0,
                madeBeds: 0,
                currentTime: 60f,
                totalTime: 60f
            );

        Assert.AreEqual(100, result.TrashScore);
        Assert.AreEqual(100, result.BedScore);
        Assert.AreEqual(100, result.TimeScore);
        Assert.AreEqual(100, result.FinalScore);

        Assert.IsTrue(result.CompletedEverything);
    }

    [Test]
    public void EvaluateProgress_TimeGreaterThanTotal_IsClamped()
    {
        CleaningProgressResult result =
            CleaningRules.EvaluateProgress(
                totalTrash: 1,
                remainingTrash: 0,
                totalBeds: 1,
                madeBeds: 1,
                currentTime: 90f,
                totalTime: 60f
            );

        Assert.AreEqual(100, result.TimeScore);
        Assert.AreEqual(100, result.FinalScore);
    }

    [Test]
    public void EvaluateProgress_InvalidRemainingTrash_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            CleaningRules.EvaluateProgress(
                totalTrash: 2,
                remainingTrash: 3,
                totalBeds: 1,
                madeBeds: 0,
                currentTime: 30f,
                totalTime: 60f
            );
        });
    }

    [TestCase(
        true,
        false,
        CleaningRoomOutcome.Available
    )]
    [TestCase(
        true,
        true,
        CleaningRoomOutcome.Occupied
    )]
    [TestCase(
        false,
        false,
        CleaningRoomOutcome.Dirty
    )]
    [TestCase(
        false,
        true,
        CleaningRoomOutcome.OccupiedNeedsCleaning
    )]
    public void ResolveRoomOutcome_ReturnsExpectedState(
        bool completedEverything,
        bool reservationStillActive,
        CleaningRoomOutcome expectedOutcome)
    {
        CleaningRoomOutcome result =
            CleaningRules.ResolveRoomOutcome(
                completedEverything,
                reservationStillActive
            );

        Assert.AreEqual(expectedOutcome, result);
    }
}