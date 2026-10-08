import { useRef, useState, type FormEvent } from 'react'
import { addQuestion } from '../api/questionBank'
import { ApiError } from '../api/types'
import { Button } from './Button'
import { FormField } from './FormField'
import { InlineError } from './InlineError'
import { useToast } from './toastContext'

const EMPTY_TEXT = 'Enter a Question first.'
const PROVIDER_DOWN = "I couldn't reach the Provider. Check that it is running, then try again."
const DUPLICATE = 'That Question is already in the Question Bank.'
const GENERIC = "I couldn't add that Question. Try again."

type Failure = { message: string; retry: boolean }

function describe(error: unknown): Failure {
  if (error instanceof ApiError) {
    if (error.type === 'duplicate-question') return { message: DUPLICATE, retry: false }
    if (error.type === 'provider-unavailable' || error.type === 'network-error')
      return { message: PROVIDER_DOWN, retry: true }
  }
  return { message: GENERIC, retry: true }
}

export function AddQuestionForm() {
  const toast = useToast()
  const textRef = useRef<HTMLTextAreaElement>(null)
  const [text, setText] = useState('')
  const [reference, setReference] = useState('')
  const [fieldError, setFieldError] = useState<string>()
  const [failure, setFailure] = useState<Failure>()
  const [busy, setBusy] = useState(false)

  async function submit() {
    if (busy) return
    setFailure(undefined)
    if (!text.trim()) {
      setFieldError(EMPTY_TEXT)
      textRef.current?.focus()
      return
    }
    setFieldError(undefined)
    setBusy(true)
    try {
      await addQuestion(text.trim(), reference.trim())
      setText('')
      setReference('')
      toast('Question added.')
    } catch (error) {
      setFailure(describe(error)) // the entered text is kept
    } finally {
      setBusy(false)
    }
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    void submit()
  }

  return (
    <section aria-labelledby="add-question-heading" className="form">
      <h2 id="add-question-heading">Add a Question</h2>
      <form className="form" onSubmit={onSubmit} noValidate>
        <FormField
          ref={textRef}
          label="Question"
          rows={3}
          value={text}
          onChange={(e) => setText(e.target.value)}
          error={fieldError}
        />
        <FormField
          label="Reference Answer (optional)"
          rows={4}
          value={reference}
          onChange={(e) => setReference(e.target.value)}
        />
        {failure && (
          <InlineError onRetry={failure.retry ? () => void submit() : undefined} retryBusy={busy}>
            {failure.message}
          </InlineError>
        )}
        <div className="form__actions">
          <Button type="submit" variant="primary" busyLabel={busy ? 'Adding...' : undefined}>
            Add
          </Button>
        </div>
      </form>
    </section>
  )
}
