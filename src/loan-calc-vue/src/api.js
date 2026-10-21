/** Thrown for a 400 with field errors; `errors` is keyed like "loanA.principal". */
export class ValidationError extends Error {
  constructor(errors) {
    super('One or more fields are invalid.')
    this.errors = errors
  }
}

/** Calls POST /api/loans/compare. `apiBase` is '' for same-origin or e.g. 'http://localhost:5090'. */
export async function compareLoans(apiBase, loanA, loanB) {
  const url = `${apiBase.replace(/\/+$/, '')}/api/loans/compare`

  let response
  try {
    response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ loanA, loanB }),
    })
  } catch {
    // Network failures and CORS rejections both land here; the browser hides which one it was.
    throw new Error("Couldn't reach the loan service. Check that the API is running.")
  }

  if (response.ok) return response.json()

  const problem = await response.json().catch(() => null)
  if (response.status === 400 && problem?.errors) throw new ValidationError(problem.errors)
  throw new Error(problem?.detail || problem?.title || `The loan service returned ${response.status}.`)
}
