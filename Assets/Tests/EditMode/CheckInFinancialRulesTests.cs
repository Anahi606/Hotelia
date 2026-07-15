using Hotelia.Core;
using NUnit.Framework;

public class CheckInFinancialRulesTests
{
    [Test]
    public void CalculateQuote_Applies13PercentVatAnd10PercentService()
    {
        CheckInQuoteResult result =
            CheckInFinancialRules.CalculateQuote(
                subtotal: 100m,
                budget: 150m,
                vatRate: 0.13m,
                applyServiceFee: true,
                serviceFeeRate: 0.10m
            );

        Assert.AreEqual(13m, result.Vat);
        Assert.AreEqual(10m, result.ServiceFee);
        Assert.AreEqual(123m, result.Total);
        Assert.IsTrue(result.WithinBudget);
    }

    [Test]
    public void CalculateQuote_ServiceDisabled_DoesNotApplyServiceFee()
    {
        CheckInQuoteResult result =
            CheckInFinancialRules.CalculateQuote(
                subtotal: 100m,
                budget: 150m,
                vatRate: 0.13m,
                applyServiceFee: false,
                serviceFeeRate: 0.10m
            );

        Assert.AreEqual(13m, result.Vat);
        Assert.AreEqual(0m, result.ServiceFee);
        Assert.AreEqual(113m, result.Total);
    }

    [Test]
    public void IsSevereBudgetOver_MoreThan25Percent_ReturnsTrue()
    {
        bool result =
            CheckInFinancialRules.IsSevereBudgetOver(
                total: 126m,
                budget: 100m,
                severeBudgetOverPercent: 0.25m
            );

        Assert.IsTrue(result);
    }

    [Test]
    public void IsSevereBudgetOver_Exactly25Percent_ReturnsFalse()
    {
        bool result =
            CheckInFinancialRules.IsSevereBudgetOver(
                total: 125m,
                budget: 100m,
                severeBudgetOverPercent: 0.25m
            );

        Assert.IsFalse(result);
    }

    [Test]
    public void IsSevereBudgetOver_WithinBudget_ReturnsFalse()
    {
        bool result =
            CheckInFinancialRules.IsSevereBudgetOver(
                total: 90m,
                budget: 100m,
                severeBudgetOverPercent: 0.25m
            );

        Assert.IsFalse(result);
    }
}