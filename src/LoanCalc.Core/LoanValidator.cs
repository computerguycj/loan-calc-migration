using System.Collections.Generic;

namespace LoanCalc.Core
{
    public class ValidationError
    {
        public ValidationError(string field, string message)
        {
            Field = field;
            Message = message;
        }

        /// <summary>Name of the <see cref="LoanInput"/> property at fault.</summary>
        public string Field { get; }

        public string Message { get; }
    }

    /// <summary>
    /// The single source of truth for input rules. Every host (Web Forms, API)
    /// calls this; client-side checks only mirror it.
    /// </summary>
    public static class LoanValidator
    {
        public static readonly int[] AllowedTerms = { 10, 15, 20, 25, 30, 40 };

        public const decimal MaxPrincipal = 100_000_000m;
        public const decimal MaxRatePercent = 30m;

        public static IReadOnlyList<ValidationError> Validate(LoanInput input)
        {
            var errors = new List<ValidationError>();
            if (input == null)
            {
                errors.Add(new ValidationError("", "Loan details are required."));
                return errors;
            }

            if (input.Principal <= 0 || input.Principal > MaxPrincipal)
                errors.Add(new ValidationError(nameof(LoanInput.Principal),
                    $"Principal must be greater than 0 and at most {MaxPrincipal:N0}."));

            if (input.AnnualRatePercent < 0 || input.AnnualRatePercent > MaxRatePercent)
                errors.Add(new ValidationError(nameof(LoanInput.AnnualRatePercent),
                    $"Interest rate must be between 0 and {MaxRatePercent}%."));

            if (System.Array.IndexOf(AllowedTerms, input.TermYears) < 0)
                errors.Add(new ValidationError(nameof(LoanInput.TermYears),
                    "Term must be 10, 15, 20, 25, 30, or 40 years."));

            if (input.Fees < 0)
                errors.Add(new ValidationError(nameof(LoanInput.Fees), "Fees cannot be negative."));
            else if (input.Principal > 0 && input.Fees >= input.Principal)
                errors.Add(new ValidationError(nameof(LoanInput.Fees), "Fees must be less than the principal."));

            if (input.ExtraMonthlyPayment < 0)
                errors.Add(new ValidationError(nameof(LoanInput.ExtraMonthlyPayment),
                    "Extra monthly payment cannot be negative."));
            else if (input.Principal > 0 && input.ExtraMonthlyPayment > input.Principal)
                errors.Add(new ValidationError(nameof(LoanInput.ExtraMonthlyPayment),
                    "Extra monthly payment cannot exceed the principal."));

            return errors;
        }
    }
}
