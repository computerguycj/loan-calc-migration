import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// The bundle goes straight into the Web Forms site, which serves it as a static file.
const outDir = fileURLToPath(new URL('../LoanCalc.WebForms/Scripts/loan-compare', import.meta.url))

export default defineConfig({
  // customElement: true makes every SFC's <style> ship inside the JS and attach to the shadow root,
  // so the build is one file with no separate CSS.
  plugins: [vue({ customElement: true })],

  // Library builds don't replace this automatically, and Vue checks it to drop dev-only code.
  define: { 'process.env.NODE_ENV': JSON.stringify('production') },

  build: {
    outDir,
    emptyOutDir: true,
    lib: {
      entry: 'src/main.js',
      name: 'LoanCompare',
      // IIFE loads with a plain <script> tag, so the Web Forms page needs no module support.
      formats: ['iife'],
      fileName: () => 'loan-compare.js',
    },
  },

  // `npm run dev` serves index.html on its own origin; forwarding /api keeps the element's
  // relative URLs working without CORS.
  server: {
    port: 5173,
    proxy: { '/api': 'http://localhost:5090' },
  },
})
