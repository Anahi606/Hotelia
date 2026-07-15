using System;
using System.Collections.Generic;
using Hotelia.Core;
using NUnit.Framework;

public class RestaurantPriorityRulesTests
{
    [TestCase(false, false, false, 0)]
    [TestCase(true, false, false, 5)]
    [TestCase(false, true, false, 4)]
    [TestCase(false, false, true, 2)]
    [TestCase(true, true, false, 9)]
    [TestCase(true, true, true, 11)]
    public void CalculatePriority_ReturnsExpectedScore(
        bool hasAllergy,
        bool isUrgent,
        bool isRoomService,
        int expectedScore)
    {
        int result =
            RestaurantPriorityRules.CalculatePriority(
                hasAllergy,
                isUrgent,
                isRoomService
            );

        Assert.AreEqual(expectedScore, result);
    }

    [Test]
    public void CalculatePriority_AllergyHasMorePriorityThanUrgency()
    {
        int allergyPriority =
            RestaurantPriorityRules.CalculatePriority(
                hasAllergy: true,
                isUrgent: false,
                isRoomService: false
            );

        int urgentPriority =
            RestaurantPriorityRules.CalculatePriority(
                hasAllergy: false,
                isUrgent: true,
                isRoomService: false
            );

        Assert.Greater(
            allergyPriority,
            urgentPriority
        );
    }

    [Test]
    public void Evaluate_CorrectDescendingOrder_GrantsBonus()
    {
        List<int> selectedPriorities =
            new List<int>
            {
                11,
                6,
                2
            };

        RestaurantOrderEvaluation result =
            RestaurantPriorityRules.Evaluate(
                selectedPriorities
            );

        Assert.AreEqual(3, result.CorrectPositions);
        Assert.AreEqual(0, result.Errors);
        Assert.IsTrue(result.IsPerfect);

        Assert.AreEqual(
            RestaurantPriorityRules.PerfectOrderBonus,
            result.Bonus
        );
    }

    [Test]
    public void Evaluate_IncorrectOrder_DoesNotGrantBonus()
    {
        List<int> selectedPriorities =
            new List<int>
            {
                2,
                11,
                6
            };

        RestaurantOrderEvaluation result =
            RestaurantPriorityRules.Evaluate(
                selectedPriorities
            );

        Assert.AreEqual(0, result.CorrectPositions);
        Assert.AreEqual(3, result.Errors);
        Assert.IsFalse(result.IsPerfect);
        Assert.AreEqual(0, result.Bonus);
    }

    [Test]
    public void Evaluate_OrdersWithEqualPriority_AreAccepted()
    {
        List<int> selectedPriorities =
            new List<int>
            {
                9,
                6,
                6,
                2
            };

        RestaurantOrderEvaluation result =
            RestaurantPriorityRules.Evaluate(
                selectedPriorities
            );

        Assert.AreEqual(4, result.CorrectPositions);
        Assert.AreEqual(0, result.Errors);
        Assert.IsTrue(result.IsPerfect);

        Assert.AreEqual(
            RestaurantPriorityRules.PerfectOrderBonus,
            result.Bonus
        );
    }

    [Test]
    public void Evaluate_PartiallyCorrectOrder_CountsErrors()
    {
        List<int> selectedPriorities =
            new List<int>
            {
                11,
                2,
                6
            };

        RestaurantOrderEvaluation result =
            RestaurantPriorityRules.Evaluate(
                selectedPriorities
            );

        Assert.AreEqual(1, result.CorrectPositions);
        Assert.AreEqual(2, result.Errors);
        Assert.IsFalse(result.IsPerfect);
        Assert.AreEqual(0, result.Bonus);
    }

    [Test]
    public void Evaluate_EmptyList_DoesNotGrantBonus()
    {
        List<int> selectedPriorities =
            new List<int>();

        RestaurantOrderEvaluation result =
            RestaurantPriorityRules.Evaluate(
                selectedPriorities
            );

        Assert.AreEqual(0, result.CorrectPositions);
        Assert.AreEqual(0, result.Errors);
        Assert.IsFalse(result.IsPerfect);
        Assert.AreEqual(0, result.Bonus);
    }

    [Test]
    public void Evaluate_NullList_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            RestaurantPriorityRules.Evaluate(null);
        });
    }
}