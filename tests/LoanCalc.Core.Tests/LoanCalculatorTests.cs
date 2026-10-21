using System;
using LoanCalc.Core;
using Xunit;

namespace LoanCalc.Core.Tests
{
    // Expected values were computed independently with the closed-form annuity formula
    // (double precision), not by running this code.
    public class LoanCalculatorTests
    {
        private static LoanInput Loan(decimal principal = 200_000m, decimal rate = 6m, int term = 30,
            decimal fees = 0m, decimal extra = 0m) =>
            new LoanInput
            {
                Principal = principal,
                AnnualRatePercent = rate,
                TermYears = term,
                Fees = fees,
                ExtraMonthlyPayment = extra,
            };

        [Fact]
        public void StandardLoan_MatchesKnownPayment()
        {
            var result = LoanCalculator.Calculate(Loan());

            Assert.Equal(1199.10m, result.MonthlyPayment);
            Assert.Equal(360, result.PayoffMonths);
            // 1199.10 * 360 - 200,000 = 231,676; the final payment absorbs a few cents of rounding.
            Assert.InRange(result.TotalInterest, 231_670m, 231_682m);
        }

        [Fact]
        public void TotalPayback_IsPrincipalPlusInterestPlusFees()
        {
            var result = LoanCalculator.Calculate(Loan(fees: 4_000m, extra: 150m));

            Assert.Equal(200_000m + result.TotalInterest + 4_000m, result.TotalPayback);
        }

        [Fact]
        public void NoFees_AprEqualsNoteRate()
        {
            var result = LoanCalculator.Calculate(Loan(rate: 6.5m));

            Assert.Equal(6.5m, result.AprPercent);
        }

        [Theory]
        [InlineData(4_000, 6.189)]
        [InlineData(10_000, 6.485)]
        public void Fees_RaiseApr(decimal fees, decimal expectedApr)
        {
            var result = LoanCalculator.Calculate(Loan(fees: fees));

            Assert.Equal(expectedApr, result.AprPercent);
            Assert.Equal(1199.10m, result.MonthlyPayment); // fees don't change the payment
        }

        [Fact]
        public void ZeroRate_PaysPrincipalOnly()
        {
            var result = LoanCalculator.Calculate(Loan(principal: 100_000m, rate: 0m, term: 10));

            Assert.Equal(833.33m, result.MonthlyPayment);
            Assert.Equal(0m, result.TotalInterest);
            Assert.Equal(100_000m, result.TotalPayback); // last payment is 833.73 to clear the cents
            Assert.Equal(0m, result.AprPercent);
        }

        [Fact]
        public void ZeroRate_WithFees_HasPositiveApr()
        {
            var result = LoanCalculator.Calculate(Loan(principal: 120_000m, rate: 0m, term: 10, fees: 6_000m));

            Assert.Equal(1.027m, result.AprPercent);
        }

        [Fact]
        public void ExtraPayment_PaysOffEarlyAndSavesInterest()
        {
            var baseline = LoanCalculator.Calculate(Loan());
            var withExtra = LoanCalculator.Calculate(Loan(extra: 200m));

            Assert.Equal(252, withExtra.PayoffMonths); // 251.5 months by formula, so 252 payments
            Assert.True(withExtra.TotalInterest < baseline.TotalInterest - 60_000m);
            Assert.Equal(baseline.MonthlyPayment, withExtra.MonthlyPayment);
        }

        [Fact]
        public void ExtraPayment_DoesNotChangeApr()
        {
            var baseline = LoanCalculator.Calculate(Loan(fees: 4_000m));
            var withExtra = LoanCalculator.Calculate(Loan(fees: 4_000m, extra: 500m));

            Assert.Equal(baseline.AprPercent, withExtra.AprPercent);
        }

        [Fact]
        public void HugeExtraPayment_PaysOffInFirstMonth()
        {
            var result = LoanCalculator.Calculate(Loan(principal: 10_000m, extra: 10_000m));

            Assert.Equal(1, result.PayoffMonths);
            Assert.Equal(50m, result.TotalInterest); // one month at 0.5%
            Assert.Equal(10_050m, result.TotalPayback);
        }

        [Fact]
        public void InvalidInput_Throws()
        {
            var ex = Assert.Throws<LoanValidationException>(() => LoanCalculator.Calculate(Loan(term: 12)));

            Assert.Contains(ex.Errors, e => e.Field == nameof(LoanInput.TermYears));
        }
    }
}
