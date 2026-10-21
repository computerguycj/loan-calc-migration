namespace LoanCalc.Core
{
    /// <summary>What the user enters for one loan.</summary>
    public class LoanInput
    {
        public decimal Principal { get; set; }

        /// <summary>Note rate as a percentage, e.g. 6.5 for 6.5%.</summary>
        public decimal AnnualRatePercent { get; set; }

        public int TermYears { get; set; }

        /// <summary>Closing costs and fees, paid up front (not financed).</summary>
        public decimal Fees { get; set; }

        /// <summary>Optional extra principal paid every month on top of the scheduled payment.</summary>
        public decimal ExtraMonthlyPayment { get; set; }
    }
}
