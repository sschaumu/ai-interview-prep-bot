import { ApiError, type ProblemType, type Question } from './types'

const KNOWN: ProblemType[] = ['provider-unavailable', 'duplicate-question', 'invalid-request']

async function toApiError(response: Response): Promise<ApiError> {
  let type: ProblemType = 'internal-error'
  try {
    const body = (await response.json()) as { type?: string }
    const slug = KNOWN.find((k) => k === body.type)
    if (slug) type = slug
  } catch {
    // Non-JSON error body: keep the generic slug.
  }
  return new ApiError(type, response.status)
}

/** POST /api/question-bank/questions */
export async function addQuestion(text: string, referenceAnswer: string): Promise<Question> {
  let response: Response
  try {
    response = await fetch('/api/question-bank/questions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ text, referenceAnswer: referenceAnswer || null }),
    })
  } catch {
    throw new ApiError('network-error', 0)
  }
  if (!response.ok) throw await toApiError(response)
  return (await response.json()) as Question
}
