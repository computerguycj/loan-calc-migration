<script setup>
import { computed } from 'vue'
import ResultsTable from './ResultsTable.vue'

// As a custom element this is the `results` attribute, written by the Web Forms code-behind after each
// postback. Attributes are strings, so it arrives as JSON. Display only: this element never calls the API.
const props = defineProps({
  results: { type: String, default: '' },
})

const parsed = computed(() => {
  try {
    return props.results ? JSON.parse(props.results) : null
  } catch {
    return null
  }
})
</script>

<template>
  <!-- Nothing here renders the light-DOM children (no <slot>), so the server's table only shows without JS. -->
  <ResultsTable v-if="parsed" :results="parsed" />
</template>

<style src="./theme.css"></style>
