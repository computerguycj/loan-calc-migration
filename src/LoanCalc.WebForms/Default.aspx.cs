using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Web.UI;
using LoanCalc.Core;

namespace LoanCalc.WebForms
{
    public partial class _Default : Page
    {
        /// <summary>
        /// The <c>&lt;loan-results&gt;</c> attribute: both results in the same camelCase shape the API returns,
        /// or empty when there's nothing to show.
        /// </summary>
        protected string ResultsJson { get; private set; } = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoanA.SetValues(new LoanInput { Principal = 300_000m, AnnualRatePercent = 6.5m, TermYears = 30, Fees = 6_000m });
                LoanB.SetValues(new LoanInput { Principal = 300_000m, AnnualRatePercent = 6.125m, TermYears = 30, Fees = 9_000m });
            }
        }

        protected void CompareButton_Click(object sender, EventArgs e)
        {
            // Calculate both, even if the first fails, so both sides show their errors.
            var a = LoanA.TryCalculate();
            var b = LoanB.TryCalculate();

            ResultsPanel.Visible = a != null && b != null;
            if (!ResultsPanel.Visible)
                return;

            ResultRows.DataSource = ResultRow.Build(a, b);
            ResultRows.DataBind();
            ResultsJson = new JavaScriptSerializer().Serialize(new { loanA = Shape(a), loanB = Shape(b) });
        }

        private static object Shape(LoanResult r)
        {
            return new
            {
                monthlyPayment = r.MonthlyPayment,
                totalInterest = r.TotalInterest,
                totalPayback = r.TotalPayback,
                aprPercent = r.AprPercent,
                payoffMonths = r.PayoffMonths,
            };
        }
    }

    /// <summary>One row of the results table. Lower is better for every metric shown.</summary>
    public class ResultRow
    {
        private static readonly CultureInfo Display = CultureInfo.GetCultureInfo("en-US");

        public string Label { get; private set; }
        public string A { get; private set; }
        public string B { get; private set; }
        public string ACssClass { get; private set; }
        public string BCssClass { get; private set; }

        public static List<ResultRow> Build(LoanResult a, LoanResult b)
        {
            return new List<ResultRow>
            {
                Row("Monthly payment", a.MonthlyPayment, b.MonthlyPayment, Money),
                Row("Total interest", a.TotalInterest, b.TotalInterest, Money),
                Row("Total payback (incl. fees)", a.TotalPayback, b.TotalPayback, Money),
                Row("APR (incl. fees)", a.AprPercent, b.AprPercent, v => v.ToString("0.000", Display) + "%"),
                Row("Paid off after", a.PayoffMonths, b.PayoffMonths, v => Duration((int)v)),
            };
        }

        private static ResultRow Row(string label, decimal a, decimal b, Func<decimal, string> format)
        {
            return new ResultRow
            {
                Label = label,
                A = format(a),
                B = format(b),
                ACssClass = a < b ? "better" : "",
                BCssClass = b < a ? "better" : "",
            };
        }

        private static string Money(decimal value) => value.ToString("C2", Display);

        private static string Duration(int months)
        {
            int years = months / 12, rest = months % 12;
            return rest == 0 ? years + " years" : years + " yr " + rest + " mo";
        }
    }
}
