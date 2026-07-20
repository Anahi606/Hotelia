using System;

namespace Hotelia.Core
{
    public readonly struct CheckInQuoteResult
    {
        public decimal Vat { get; }
        public decimal ServiceFee { get; }
        public decimal Total { get; }
        public decimal Remaining { get; }
        public bool WithinBudget { get; }

        public CheckInQuoteResult(
            decimal vat,
            decimal serviceFee,
            decimal total,
            decimal remaining,
            bool withinBudget)
        {
            Vat = vat;
            ServiceFee = serviceFee;
            Total = total;
            Remaining = remaining;
            WithinBudget = withinBudget;
        }
    }

    public static class CheckInFinancialRules
    {
        public static CheckInQuoteResult CalculateQuote(
            decimal subtotal,
            decimal budget,
            decimal vatRate,
            bool applyServiceFee,
            decimal serviceFeeRate)
        {
            ValidateValues(
                subtotal,
                budget,
                vatRate,
                serviceFeeRate
            );

            decimal vat = decimal.Round(
                subtotal * vatRate,
                2,
                MidpointRounding.AwayFromZero
            );

            decimal serviceFee = applyServiceFee
                ? decimal.Round(
                    subtotal * serviceFeeRate,
                    2,
                    MidpointRounding.AwayFromZero
                )
                : 0m;

            decimal total = subtotal + vat + serviceFee;
            decimal remaining = budget - total;

            return new CheckInQuoteResult(
                vat,
                serviceFee,
                total,
                remaining,
                total <= budget
            );
        }

        public static decimal CalculateTotal(
            decimal subtotal,
            decimal vatRate,
            bool applyServiceFee,
            decimal serviceFeeRate)
        {
            if (subtotal < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(subtotal),
                    "The subtotal cannot be negative."
                );
            }

            if (vatRate < 0m || vatRate > 1m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vatRate)
                );
            }

            if (serviceFeeRate < 0m || serviceFeeRate > 1m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(serviceFeeRate)
                );
            }

            decimal vat = decimal.Round(
                subtotal * vatRate,
                2,
                MidpointRounding.AwayFromZero
            );

            decimal serviceFee = applyServiceFee
                ? decimal.Round(
                    subtotal * serviceFeeRate,
                    2,
                    MidpointRounding.AwayFromZero
                )
                : 0m;

            return subtotal + vat + serviceFee;
        }

        public static bool IsSevereBudgetOver(
            decimal total,
            decimal budget,
            decimal severeBudgetOverPercent)
        {
            if (budget <= 0m)
                return true;

            if (total <= budget)
                return false;

            if (severeBudgetOverPercent < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(severeBudgetOverPercent)
                );
            }

            decimal maximumAcceptedTotal =
                budget * (1m + severeBudgetOverPercent);

            // Se rechaza únicamente cuando supera el 25 %.
            return total > maximumAcceptedTotal;
        }

        private static void ValidateValues(
            decimal subtotal,
            decimal budget,
            decimal vatRate,
            decimal serviceFeeRate)
        {
            if (subtotal < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(subtotal),
                    "The subtotal cannot be negative."
                );
            }

            if (budget <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(budget),
                    "The budget must be greater than zero."
                );
            }

            if (vatRate < 0m || vatRate > 1m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vatRate)
                );
            }

            if (serviceFeeRate < 0m || serviceFeeRate > 1m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(serviceFeeRate)
                );
            }
        }
    }
}