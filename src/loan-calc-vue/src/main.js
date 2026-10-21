import { defineCustomElement } from 'vue'
import LoanCompare from './LoanCompare.vue'

// Guard against the script being included twice on one page.
if (!customElements.get('loan-compare')) {
  customElements.define('loan-compare', defineCustomElement(LoanCompare))
}
