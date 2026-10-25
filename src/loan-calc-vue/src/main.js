import { defineCustomElement } from 'vue'
import LoanCompare from './LoanCompare.vue'
import LoanResults from './LoanResults.vue'

// One bundle registers every element, so Vue loads once per page whichever elements it uses.
const elements = {
  'loan-compare': LoanCompare,
  'loan-results': LoanResults,
}

// Guard against the script being included twice on one page.
for (const [tag, component] of Object.entries(elements)) {
  if (!customElements.get(tag)) customElements.define(tag, defineCustomElement(component))
}
