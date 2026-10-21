<script setup>
// Mirrors LoanValidator.AllowedTerms in LoanCalc.Core. The server still rejects anything else.
const TERMS = [10, 15, 20, 25, 30, 40]

const loan = defineModel({ type: Object, required: true })

const props = defineProps({
  title: { type: String, required: true },
  /** JSON name of this loan in the API request ("loanA"/"loanB"); also prefixes element ids. */
  prefix: { type: String, required: true },
  /** Server errors for the whole request, keyed like "loanA.principal". */
  errors: { type: Object, default: () => ({}) },
})

const fieldErrors = (field) => props.errors[`${props.prefix}.${field}`] ?? []
const id = (field) => `${props.prefix}-${field}`
</script>

<template>
  <fieldset class="loan">
    <legend>{{ title }}</legend>

    <ul v-if="errors[prefix]" class="errors" role="alert">
      <li v-for="message in errors[prefix]" :key="message">{{ message }}</li>
    </ul>

    <div class="field">
      <label :for="id('principal')">Principal ($)</label>
      <input :id="id('principal')" v-model="loan.principal" type="number" min="1" max="100000000" step="any" required
        :aria-invalid="fieldErrors('principal').length > 0" />
      <span v-for="message in fieldErrors('principal')" :key="message" class="error">{{ message }}</span>
    </div>

    <div class="field">
      <label :for="id('annualRatePercent')">Annual interest rate (%)</label>
      <input :id="id('annualRatePercent')" v-model="loan.annualRatePercent" type="number" min="0" max="30" step="any" required
        :aria-invalid="fieldErrors('annualRatePercent').length > 0" />
      <span v-for="message in fieldErrors('annualRatePercent')" :key="message" class="error">{{ message }}</span>
    </div>

    <div class="field">
      <label :for="id('termYears')">Term</label>
      <select :id="id('termYears')" v-model.number="loan.termYears" :aria-invalid="fieldErrors('termYears').length > 0">
        <option v-for="years in TERMS" :key="years" :value="years">{{ years }} years</option>
      </select>
      <span v-for="message in fieldErrors('termYears')" :key="message" class="error">{{ message }}</span>
    </div>

    <div class="field">
      <label :for="id('fees')">Closing costs and fees ($)</label>
      <input :id="id('fees')" v-model="loan.fees" type="number" min="0" step="any"
        :aria-invalid="fieldErrors('fees').length > 0" />
      <span v-for="message in fieldErrors('fees')" :key="message" class="error">{{ message }}</span>
    </div>

    <div class="field">
      <label :for="id('extraMonthlyPayment')">Extra monthly principal ($, optional)</label>
      <input :id="id('extraMonthlyPayment')" v-model="loan.extraMonthlyPayment" type="number" min="0" step="any"
        :aria-invalid="fieldErrors('extraMonthlyPayment').length > 0" />
      <span v-for="message in fieldErrors('extraMonthlyPayment')" :key="message" class="error">{{ message }}</span>
    </div>
  </fieldset>
</template>

<style>
.loan {
  margin: 0;
  padding: 16px;
  background: var(--_surface);
  border: 1px solid var(--_border);
  border-radius: var(--_radius);
}
.loan legend { padding: 0 6px; font-weight: 600; }

.field { display: flex; flex-direction: column; gap: 4px; margin-bottom: 12px; }
.field label { font-size: 0.9rem; }
.field input, .field select {
  padding: 8px;
  font: inherit;
  color: var(--_text);
  background: var(--_surface);
  border: 1px solid var(--_border);
  border-radius: var(--_radius);
}
.field [aria-invalid="true"] { border-color: var(--_error); outline: 1px solid var(--_error); }
.field .error { color: var(--_error); font-size: 0.85rem; }

.errors {
  margin: 0 0 12px;
  padding: 8px 8px 8px 28px;
  color: var(--_error);
  background: var(--_error-bg);
  border-radius: var(--_radius);
  font-size: 0.9rem;
}
</style>
