namespace LoanCalc.Core
{
    public class LoanResult
    {
        /// <summary>Scheduled principal and interest payment, excluding any extra payment.</summary>
        public decimal MonthlyPayment { get; set; }

        /// <summary>Interest actually paid, after the effect of extra payments.</summary>
        public decimal TotalInterest { get; set; }

        /// <summary>Everything paid out: all payments plus up-front fees.</summary>
        public decimal TotalPayback { get; set; }

        /// <summary>Annual percentage rate including fees, as a percentage rounded to 3 places.</summary>
        public decimal AprPercent { get; set; }

        /// <summary>Number of monthly payments until the balance reaches zero.</summary>
        public int PayoffMonths { get; set; }
    }
}
