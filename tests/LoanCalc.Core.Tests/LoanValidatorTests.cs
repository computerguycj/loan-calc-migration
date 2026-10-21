using System.Linq;
using LoanCalc.Core;
using Xunit;

namespace LoanCalc.Core.Tests
{
    public class LoanValidatorTests
    {
        private static LoanInput Valid() => new LoanInput
        {
            Principal = 300_000m,
            AnnualRatePercent = 5m,
            TermYears = 30,
            Fees = 3_000m,
            ExtraMonthlyPayment = 0m,
        };

        [Fact]
        public void ValidInput_HasNoErrors()
        {
            Assert.Empty(LoanValidator.Validate(Valid()));
        }

        [Fact]
        public void NullInput_IsAnError()
        {
            Assert.Single(LoanValidator.Validate(null));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(30)]
        [InlineData(40)]
        public void AllowedTerms_AreAccepted(int term)
        {
            var input = Valid();
            input.TermYears = term;

            Assert.Empty(LoanValidator.Validate(input));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        [InlineData(35)]
        public void OtherTerms_AreRejected(int term)
        {
            var input = Valid();
            input.TermYears = term;

            AssertSingleError(input, nameof(LoanInput.TermYears));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(100_000_001)]
        public void Principal_OutOfRange(decimal principal)
        {
            var input = Valid();
            input.Principal = principal;
            input.Fees = 0m;

            AssertSingleError(input, nameof(LoanInput.Principal));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(30.01)]
        public void Rate_OutOfRange(decimal rate)
        {
            var input = Valid();
            input.AnnualRatePercent = rate;

            AssertSingleError(input, nameof(LoanInput.AnnualRatePercent));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(300_000)]
        public void Fees_OutOfRange(decimal fees)
        {
            var input = Valid();
            input.Fees = fees;

            AssertSingleError(input, nameof(LoanInput.Fees));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(300_001)]
        public void Extra_OutOfRange(decimal extra)
        {
            var input = Valid();
            input.ExtraMonthlyPayment = extra;

            AssertSingleError(input, nameof(LoanInput.ExtraMonthlyPayment));
        }

        private static void AssertSingleError(LoanInput input, string field)
        {
            var errors = LoanValidator.Validate(input);
            Assert.Single(errors);
            Assert.Equal(field, errors.Single().Field);
        }
    }
}
