using System;
using System.Collections.Generic;

namespace Hotelia.Core
{
    public readonly struct RestaurantOrderEvaluation
    {
        public int CorrectPositions { get; }
        public int Errors { get; }
        public bool IsPerfect { get; }
        public int Bonus { get; }

        public RestaurantOrderEvaluation(
            int correctPositions,
            int errors,
            bool isPerfect,
            int bonus)
        {
            CorrectPositions = correctPositions;
            Errors = errors;
            IsPerfect = isPerfect;
            Bonus = bonus;
        }
    }

    public static class RestaurantPriorityRules
    {
        public const int AllergyPoints = 5;
        public const int UrgencyPoints = 4;
        public const int RoomServicePoints = 2;
        public const int PerfectOrderBonus = 100;

        public static int CalculatePriority(
            bool hasAllergy,
            bool isUrgent,
            bool isRoomService)
        {
            int score = 0;

            if (hasAllergy)
                score += AllergyPoints;

            if (isUrgent)
                score += UrgencyPoints;

            if (isRoomService)
                score += RoomServicePoints;

            return score;
        }

        public static RestaurantOrderEvaluation Evaluate(
            IReadOnlyList<int> selectedPriorities)
        {
            if (selectedPriorities == null)
            {
                throw new ArgumentNullException(
                    nameof(selectedPriorities)
                );
            }

            if (selectedPriorities.Count == 0)
            {
                return new RestaurantOrderEvaluation(
                    correctPositions: 0,
                    errors: 0,
                    isPerfect: false,
                    bonus: 0
                );
            }

            int[] expectedPriorities =
                new int[selectedPriorities.Count];

            for (int i = 0; i < selectedPriorities.Count; i++)
            {
                expectedPriorities[i] =
                    selectedPriorities[i];
            }

            Array.Sort(expectedPriorities);
            Array.Reverse(expectedPriorities);

            int correctPositions = 0;

            for (int i = 0; i < selectedPriorities.Count; i++)
            {
                if (selectedPriorities[i] ==
                    expectedPriorities[i])
                {
                    correctPositions++;
                }
            }

            int errors =
                selectedPriorities.Count - correctPositions;

            bool isPerfect = errors == 0;

            int bonus = isPerfect
                ? PerfectOrderBonus
                : 0;

            return new RestaurantOrderEvaluation(
                correctPositions,
                errors,
                isPerfect,
                bonus
            );
        }
    }
}