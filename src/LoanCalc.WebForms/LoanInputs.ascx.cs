using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using LoanCalc.Core;

namespace LoanCalc.WebForms
{
    /// <summary>Inputs for one loan. Used twice on Default.aspx.</summary>
    public partial class LoanInputs : UserControl
    {
        public string Title { get; set; }

        /// <summary>Rendered as the <c>&lt;term-picker&gt;</c> <c>invalid</c> attribute, since the element can't see the select's state change.</summary>
        protected bool TermInvalid { get; private set; }

        protected void Page_Init(object sender, EventArgs e)
        {
            // Rebuilt on every request (before post data loads) so the list never depends on view state.
            Term.Items.Clear();
            foreach (int years in LoanValidator.AllowedTerms)
                Term.Items.Add(new ListItem(years + " years", years.ToString(CultureInfo.InvariantCulture)));
        }

        public void SetValues(LoanInput input)
        {
            Principal.Text = input.Principal.ToString(CultureInfo.InvariantCulture);
            Rate.Text = input.AnnualRatePercent.ToString(CultureInfo.InvariantCulture);
            Term.SelectedValue = input.TermYears.ToString(CultureInfo.InvariantCulture);
            Fees.Text = input.Fees.ToString(CultureInfo.InvariantCulture);
            Extra.Text = input.ExtraMonthlyPayment == 0m ? "" : input.ExtraMonthlyPayment.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the posted text, runs the Core validation rules, and calculates.
        /// Returns null and shows the errors if anything is wrong.
        /// </summary>
        public LoanResult TryCalculate()
        {
            var errors = new List<ValidationError>();

            int termYears;
            int.TryParse(Term.SelectedValue, NumberStyles.None, CultureInfo.InvariantCulture, out termYears);

            var input = new LoanInput
            {
                Principal = ParseAmount(Principal, nameof(LoanInput.Principal), "Principal", true, errors),
                AnnualRatePercent = ParseAmount(Rate, nameof(LoanInput.AnnualRatePercent), "Interest rate", true, errors),
                TermYears = termYears,
                Fees = ParseAmount(Fees, nameof(LoanInput.Fees), "Fees", false, errors),
                ExtraMonthlyPayment = ParseAmount(Extra, nameof(LoanInput.ExtraMonthlyPayment), "Extra payment", false, errors),
            };

            // Only check ranges once every field is a number, so a typo doesn't also report "must be greater than 0".
            if (errors.Count == 0)
                errors.AddRange(LoanValidator.Validate(input));

            ShowErrors(errors);
            return errors.Count == 0 ? LoanCalculator.Calculate(input) : null;
        }

        private void ShowErrors(IList<ValidationError> errors)
        {
            Errors.Items.Clear();
            foreach (var error in errors)
                Errors.Items.Add(error.Message);
            Errors.Visible = errors.Count > 0;

            var fields = new Dictionary<string, WebControl>
            {
                { nameof(LoanInput.Principal), Principal },
                { nameof(LoanInput.AnnualRatePercent), Rate },
                { nameof(LoanInput.TermYears), Term },
                { nameof(LoanInput.Fees), Fees },
                { nameof(LoanInput.ExtraMonthlyPayment), Extra },
            };
            TermInvalid = errors.Any(e => e.Field == nameof(LoanInput.TermYears));
            foreach (var pair in fields)
            {
                bool invalid = errors.Any(e => e.Field == pair.Key);
                pair.Value.CssClass = invalid ? "invalid" : "";
                if (invalid)
                    pair.Value.Attributes["aria-invalid"] = "true";
                else
                    pair.Value.Attributes.Remove("aria-invalid");
            }
        }

        private static decimal ParseAmount(TextBox box, string field, string label, bool required,
            List<ValidationError> errors)
        {
            string text = box.Text.Trim();
            if (text.Length == 0)
            {
                if (required)
                    errors.Add(new ValidationError(field, label + " is required."));
                return 0m;
            }

            decimal value;
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                errors.Add(new ValidationError(field, label + " must be a number."));
            return value;
        }
    }
}
