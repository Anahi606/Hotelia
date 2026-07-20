using System;

namespace Hotelia.Core
{
    public enum CleaningRoomOutcome
    {
        Available,
        Occupied,
        Dirty,
        OccupiedNeedsCleaning
    }

    public readonly struct CleaningProgressResult
    {
        public int CleanedTrash { get; }
        public int RemainingTrash { get; }
        public int RemainingBeds { get; }
        public int TotalErrors { get; }

        public int TrashScore { get; }
        public int BedScore { get; }
        public int TimeScore { get; }
        public int FinalScore { get; }

        public bool CompletedEverything { get; }

        public CleaningProgressResult(
            int cleanedTrash,
            int remainingTrash,
            int remainingBeds,
            int totalErrors,
            int trashScore,
            int bedScore,
            int timeScore,
            int finalScore,
            bool completedEverything)
        {
            CleanedTrash = cleanedTrash;
            RemainingTrash = remainingTrash;
            RemainingBeds = remainingBeds;
            TotalErrors = totalErrors;

            TrashScore = trashScore;
            BedScore = bedScore;
            TimeScore = timeScore;
            FinalScore = finalScore;

            CompletedEverything = completedEverything;
        }
    }

    public static class CleaningRules
    {
        public static bool CanStartCleaning(bool roomIsDirty)
        {
            return roomIsDirty;
        }

        public static CleaningProgressResult EvaluateProgress(
            int totalTrash,
            int remainingTrash,
            int totalBeds,
            int madeBeds,
            float currentTime,
            float totalTime)
        {
            if (totalTrash < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalTrash)
                );

            if (remainingTrash < 0 ||
                remainingTrash > totalTrash)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingTrash)
                );
            }

            if (totalBeds < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalBeds)
                );

            if (madeBeds < 0 || madeBeds > totalBeds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(madeBeds)
                );
            }

            int cleanedTrash =
                totalTrash - remainingTrash;

            int remainingBeds =
                totalBeds - madeBeds;

            int totalErrors =
                remainingTrash + remainingBeds;

            int trashScore = totalTrash == 0
                ? 100
                : RoundPercentage(
                    cleanedTrash / (double)totalTrash
                );

            int bedScore = totalBeds == 0
                ? 100
                : RoundPercentage(
                    madeBeds / (double)totalBeds
                );

            double safeCurrentTime = Math.Max(
                0d,
                Math.Min(currentTime, totalTime)
            );

            int timeScore = totalTime <= 0f
                ? 0
                : RoundPercentage(
                    safeCurrentTime / totalTime
                );

            int finalScore = (int)Math.Round(
                (trashScore * 0.4d) +
                (bedScore * 0.4d) +
                (timeScore * 0.2d),
                MidpointRounding.AwayFromZero
            );

            finalScore = Math.Max(
                0,
                Math.Min(finalScore, 100)
            );

            bool completedEverything =
                remainingTrash == 0 &&
                remainingBeds == 0;

            return new CleaningProgressResult(
                cleanedTrash,
                remainingTrash,
                remainingBeds,
                totalErrors,
                trashScore,
                bedScore,
                timeScore,
                finalScore,
                completedEverything
            );
        }

        public static CleaningRoomOutcome ResolveRoomOutcome(
            bool completedEverything,
            bool reservationStillActive)
        {
            if (completedEverything)
            {
                return reservationStillActive
                    ? CleaningRoomOutcome.Occupied
                    : CleaningRoomOutcome.Available;
            }

            return reservationStillActive
                ? CleaningRoomOutcome.OccupiedNeedsCleaning
                : CleaningRoomOutcome.Dirty;
        }

        private static int RoundPercentage(double value)
        {
            int result = (int)Math.Round(
                value * 100d,
                MidpointRounding.AwayFromZero
            );

            return Math.Max(0, Math.Min(result, 100));
        }
    }
}