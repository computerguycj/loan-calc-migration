<script setup>
import { reactive, ref } from 'vue'
import LoanFields from './LoanFields.vue'
import ResultsTable from './ResultsTable.vue'
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

  <ResultsTable v-if="results" :results="results" />
</template>

<style src="./theme.css"></style>

<style>
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
</style>
