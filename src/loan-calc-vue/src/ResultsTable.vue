<script setup>
import { computed } from 'vue'

// Same shape as the API response: { loanA: { monthlyPayment, … }, loanB: { … } }.
const props = defineProps({
  results: { type: Object, required: true },
})

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const duration = (months) => (months % 12 === 0 ? `${months / 12} years` : `${Math.floor(months / 12)} yr ${months % 12} mo`)

// Lower is better for every metric shown.
const rows = computed(() => {
  const { loanA, loanB } = props.results
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
  <div class="results">
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
