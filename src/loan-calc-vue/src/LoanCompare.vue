<script setup>
import { computed, reactive, ref } from 'vue'
import LoanFields from './LoanFields.vue'
import { compareLoans, ValidationError } from './api.js'

// As a custom element this is the `api-base` attribute: <loan-compare api-base="http://localhost:5090">.
const props = defineProps({
  apiBase: { type: String, default: '' },
})

const loans = reactive({
  loanA: { principal: 300000, annualRatePercent: 6.5, termYears: 30, fees: 6000, extraMonthlyPayment: '' },
  loanB: { principal: 300000, annualRatePercent: 6.125, termYears: 30, fees: 9000, extraMonthlyPayment: '' },
})

const errors = ref({})
const results = ref(null)
const failure = ref('')
const busy = ref(false)

// Empty number inputs come back as ''. Send 0 and let the server decide whether that's allowed.
const toRequest = (loan) =>
  Object.fromEntries(Object.entries(loan).map(([key, value]) => [key, value === '' ? 0 : Number(value)]))

async function compare() {
  busy.value = true
  failure.value = ''
  errors.value = {}
  try {
    results.value = await compareLoans(props.apiBase, toRequest(loans.loanA), toRequest(loans.loanB))
  } catch (e) {
    results.value = null
    if (e instanceof ValidationError) errors.value = e.errors
    else failure.value = e.message
  } finally {
    busy.value = false
  }
}

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const duration = (months) => (months % 12 === 0 ? `${months / 12} years` : `${Math.floor(months / 12)} yr ${months % 12} mo`)

// Lower is better for every metric shown.
const rows = computed(() => {
  if (!results.value) return []
  const { loanA, loanB } = results.value
  const row = (label, key, format) => ({
    label,
    a: format(loanA[key]),
    b: format(loanB[key]),
    better: loanA[key] < loanB[key] ? 'a' : loanB[key] < loanA[key] ? 'b' : '',
  })
  return [
    row('Monthly payment', 'monthlyPayment', money.format),
    row('Total interest', 'totalInterest', money.format),
    row('Total payback (incl. fees)', 'totalPayback', money.format),
    row('APR (incl. fees)', 'aprPercent', (v) => `${v.toFixed(3)}%`),
    row('Paid off after', 'payoffMonths', duration),
  ]
})
</script>

<template>
  <form @submit.prevent="compare">
    <div class="loans">
      <LoanFields v-model="loans.loanA" title="Loan A" prefix="loanA" :errors="errors" />
      <LoanFields v-model="loans.loanB" title="Loan B" prefix="loanB" :errors="errors" />
    </div>
    <button class="button" type="submit" :disabled="busy">{{ busy ? 'Comparing…' : 'Compare' }}</button>
  </form>

  <p v-if="failure" class="failure" role="alert">{{ failure }}</p>

  <div v-if="results" class="results">
    <table>
      <caption>Results <span class="muted">(the lower value in each row is highlighted)</span></caption>
      <thead>
        <tr><th scope="col"></th><th scope="col">Loan A</th><th scope="col">Loan B</th></tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r.label">
          <th scope="row">{{ r.label }}</th>
          <td :class="{ better: r.better === 'a' }">{{ r.a }}</td>
          <td :class="{ better: r.better === 'b' }">{{ r.b }}</td>
        </tr>
      </tbody>
    </table>
    <p class="muted note">APR includes fees and is based on the scheduled payments only. Extra payments
      lower total interest and total payback but don't change APR.</p>
  </div>
</template>

<style>
/*
 * Theme: the host page sets --lc-* custom properties (they inherit through the shadow root).
 * Each one falls back to a default here so the element also looks right on a page with no theme.
 */
:host {
  --_bg: var(--lc-bg, #f6f7f9);
  --_surface: var(--lc-surface, #ffffff);
  --_text: var(--lc-text, #1d2330);
  --_muted: var(--lc-muted, #5d6677);
  --_border: var(--lc-border, #d5d9e0);
  --_accent: var(--lc-accent, #2f5d8a);
  --_accent-text: var(--lc-accent-text, #ffffff);
  --_good-bg: var(--lc-good-bg, #e3f2e8);
  --_good-text: var(--lc-good-text, #1d5e35);
  --_error: var(--lc-error, #a32121);
  --_error-bg: var(--lc-error-bg, #fbeaea);
  --_radius: var(--lc-radius, 6px);

  display: block;
  color: var(--_text);
  font-family: var(--lc-font, system-ui, -apple-system, "Segoe UI", Roboto, sans-serif);
  line-height: 1.5;
}
* { box-sizing: border-box; }

.muted { color: var(--_muted); font-weight: normal; }

.loans {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}

.button {
  padding: 10px 24px;
  font: inherit;
  font-weight: 600;
  color: var(--_accent-text);
  background: var(--_accent);
  border: 0;
  border-radius: var(--_radius);
  cursor: pointer;
}
.button:hover { filter: brightness(1.1); }
.button:disabled { opacity: 0.6; cursor: progress; }

.failure {
  padding: 8px 12px;
  color: var(--_error);
  background: var(--_error-bg);
  border-radius: var(--_radius);
}

.results { margin-top: 24px; overflow-x: auto; }
.results table {
  width: 100%;
  border-collapse: collapse;
  background: var(--_surface);
  border: 1px solid var(--_border);
}
.results caption { text-align: left; font-weight: 600; padding-bottom: 8px; }
.results th, .results td { padding: 8px 12px; border-bottom: 1px solid var(--_border); text-align: right; }
.results th[scope="row"] { text-align: left; font-weight: normal; }
.results td.better { background: var(--_good-bg); color: var(--_good-text); font-weight: 600; }
.note { font-size: 0.85rem; }
</style>
