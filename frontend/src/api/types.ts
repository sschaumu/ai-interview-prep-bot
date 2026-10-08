// Hand-written mirror of the API DTOs and error slugs (AD-6, AD-15).

export type Question = {
  id: string
  text: string
  referenceAnswer: string | null
  createdAt: string
}

export type ProblemType =
  | 'provider-unavailable'
  | 'duplicate-question'
  | 'invalid-request'
  | 'internal-error'
  | 'network-error'

export class ApiError extends Error {
  readonly type: ProblemType
  readonly status: number

  constructor(type: ProblemType, status: number) {
    super(type)
    this.type = type
    this.status = status
  }
}
