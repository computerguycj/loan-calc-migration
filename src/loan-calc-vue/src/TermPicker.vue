<script setup>
import { computed, onBeforeUnmount, ref, useHost } from 'vue'

// <term-picker label="Term" invalid="false"><select name="…">…</select></term-picker>
// The light-DOM <select> is the real form field: it's what posts back, and what shows when JavaScript is off.
// It isn't slotted, so with JavaScript on only these buttons show; each click writes through to the select.
const props = defineProps({
  label: { type: String, default: '' },
  // A string attribute, so "false" must mean false. Vue re-renders when the attribute changes in place.
  invalid: { type: String, default: 'false' },
})

// The element owns its child, so no ids are needed to find it, and two pickers on one page can't mix them up.
const select = useHost().querySelector('select')

// Options and the current value come from the server-rendered select.
const options = select ? [...select.options].map((o) => ({ value: o.value, text: o.text })) : []
const value = ref(select?.value ?? '')
const invalid = computed(() => props.invalid === 'true' || props.invalid === '')
const buttons = ref([])

function choose(v) {
  value.value = v
  if (select.value === v) return
  select.value = v
  select.dispatchEvent(new Event('change', { bubbles: true }))
}

// Keep in step if anything else changes the select (e.g. the browser restoring form state).
const sync = () => (value.value = select.value)
select?.addEventListener('change', sync)
onBeforeUnmount(() => select?.removeEventListener('change', sync))

// Radio-group keyboard behavior: arrows move the selection, and only the checked button is a tab stop.
function onKey(e, index) {
  const step = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[e.key]
  if (!step) return
  e.preventDefault()
  const next = (index + step + options.length) % options.length
  choose(options[next].value)
  buttons.value[next]?.focus()
}
</script>

<template>
  <div class="terms" role="radiogroup" :aria-label="label" :aria-invalid="invalid ? 'true' : null" :class="{ invalid }">
    <button
      v-for="(o, i) in options"
      :key="o.value"
      ref="buttons"
      type="button"
      role="radio"
      :aria-checked="o.value === value ? 'true' : 'false'"
      :tabindex="o.value === value ? 0 : -1"
      @click="choose(o.value)"
      @keydown="onKey($event, i)"
    >{{ o.text }}</button>
  </div>
</template>

<style src="./theme.css"></style>

<style>
.terms {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.terms button {
  padding: 6px 10px;
  font: inherit;
  font-size: 0.9rem;
  color: var(--_text);
  background: var(--_surface);
  border: 1px solid var(--_border);
  border-radius: var(--_radius);
  cursor: pointer;
}
.terms button:hover { border-color: var(--_accent); }
.terms button[aria-checked="true"] {
  color: var(--_accent-text);
  background: var(--_accent);
  border-color: var(--_accent);
  font-weight: 600;
}
.terms button:focus-visible { outline: 2px solid var(--_accent); outline-offset: 2px; }
.terms.invalid button { border-color: var(--_error); }
</style>
