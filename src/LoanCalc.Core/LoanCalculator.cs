using System;
using System.Collections.Generic;
using System.Linq;

namespace LoanCalc.Core
{
    public class LoanValidationException : ArgumentException
    {
        public LoanValidationException(IReadOnlyList<ValidationError> errors)
            : base(string.Join(" ", errors.Select(e => e.Message)))
        {
            Errors = errors;
        }

        public IReadOnlyList<ValidationError> Errors { get; }
    }

    public static class LoanCalculator
    {
        public static LoanResult Calculate(LoanInput input)
        {
            var errors = LoanValidator.Validate(input);
            if (errors.Count > 0)
                throw new LoanValidationException(errors);

            int months = input.TermYears * 12;
            decimal monthlyRate = input.AnnualRatePercent / 100m / 12m;
            decimal payment = ScheduledPayment(input.Principal, monthlyRate, months);

            var actual = Amortize(input.Principal, monthlyRate, months, payment, input.ExtraMonthlyPayment);

            // APR follows the Truth in Lending convention: it is based on the
            // scheduled payments only, because extra payments are voluntary.
            var scheduled = input.ExtraMonthlyPayment == 0m
                ? actual
                : Amortize(input.Principal, monthlyRate, months, payment, 0m);

            return new LoanResult
            {
                MonthlyPayment = payment,
                TotalInterest = actual.TotalInterest,
                TotalPayback = actual.Payments.Sum() + input.Fees,
                AprPercent = SolveAprPercent(input.Principal - input.Fees, scheduled.Payments),
                PayoffMonths = actual.Payments.Count,
            };
        }

        /// <summary>Standard annuity payment, rounded to the cent.</summary>
        private static decimal ScheduledPayment(decimal principal, decimal monthlyRate, int months)
        {
            if (monthlyRate == 0m)
                return RoundMoney(principal / months);

            decimal growth = Pow(1m + monthlyRate, months);
            return RoundMoney(principal * monthlyRate * growth / (growth - 1m));
        }

        private class Schedule
        {
            public List<decimal> Payments { get; } = new List<decimal>();
            public decimal TotalInterest { get; set; }
        }

        /// <summary>
        /// Month-by-month schedule. Interest is rounded to the cent each month, and the
        /// final payment is whatever clears the balance, so rounding drift never accumulates.
        /// </summary>
        private static Schedule Amortize(decimal principal, decimal monthlyRate, int months,
            decimal payment, decimal extra)
        {
            var schedule = new Schedule();
            decimal balance = principal;

            for (int month = 1; month <= months && balance > 0m; month++)
            {
                decimal interest = RoundMoney(balance * monthlyRate);
                decimal due = payment + extra;
                decimal paid = (month == months || balance + interest <= due) ? balance + interest : due;

                balance -= paid - interest;
                schedule.TotalInterest += interest;
                schedule.Payments.Add(paid);
            }

            return schedule;
        }

        /// <summary>
        /// Finds the monthly rate i where the present value of the payments equals the
        /// amount financed (principal minus fees), then annualizes it as i * 12.
        /// Uses bisection: present value falls steadily as the rate rises, so it always converges.
        /// </summary>
        private static decimal SolveAprPercent(decimal amountFinanced, IList<decimal> payments)
        {
            double target = (double)amountFinanced;
            double[] flows = payments.Select(p => (double)p).ToArray();

            if (PresentValue(flows, 0.0) <= target)
                return 0m;

            double low = 0.0, high = 1.0;
            while (PresentValue(flows, high) > target)
                high *= 2.0;

            for (int i = 0; i < 200; i++)
            {
                double mid = (low + high) / 2.0;
                if (PresentValue(flows, mid) > target)
                    low = mid;
                else
                    high = mid;
            }

            return Math.Round((decimal)((low + high) / 2.0 * 12.0 * 100.0), 3, MidpointRounding.AwayFromZero);
        }

        private static double PresentValue(double[] flows, double monthlyRate)
        {
            double discount = 1.0 / (1.0 + monthlyRate);
            double factor = 1.0;
            double total = 0.0;
            foreach (double flow in flows)
            {
                factor *= discount;
                total += flow * factor;
            }
            return total;
        }

        private static decimal Pow(decimal value, int exponent)
        {
            decimal result = 1m;
            while (exponent > 0)
            {
                if ((exponent & 1) == 1)
                    result *= value;
                value *= value;
                exponent >>= 1;
            }
            return result;
        }

        private static decimal RoundMoney(decimal amount) =>
            Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
