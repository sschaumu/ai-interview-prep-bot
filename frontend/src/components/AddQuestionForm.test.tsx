import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { addQuestion } from '../api/questionBank'
import { ApiError } from '../api/types'
import { AddQuestionForm } from './AddQuestionForm'
import { ToastProvider } from './Toast'
import { TOAST_MS } from './toastContext'

vi.mock('../api/questionBank', () => ({ addQuestion: vi.fn() }))
const mockAdd = vi.mocked(addQuestion)

const created = { id: '1', text: 'What is DI?', referenceAnswer: null, createdAt: '2026-10-02T00:00:00Z' }

function setup() {
  render(
    <ToastProvider>
      <AddQuestionForm />
    </ToastProvider>,
  )
  return userEvent.setup()
}

describe('AddQuestionForm', () => {
  beforeEach(() => {
    mockAdd.mockReset()
  })

  it('rejects empty text without calling the API and focuses the field', async () => {
    const user = setup()
    await user.click(screen.getByRole('button', { name: 'Add' }))

    const field = screen.getByLabelText('Question')
    expect(screen.getByText('Enter a Question first.')).toBeInTheDocument()
    expect(field).toHaveAttribute('aria-invalid', 'true')
    expect(field).toHaveFocus()
    expect(mockAdd).not.toHaveBeenCalled()
  })

  it('submits trimmed text, clears the form and shows a status toast that auto-dismisses', async () => {
    mockAdd.mockResolvedValue(created)
    const user = setup()
    await user.type(screen.getByLabelText('Question'), '  What is DI?  ')
    await user.type(screen.getByLabelText('Reference Answer (optional)'), 'Inversion of control.')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(mockAdd).toHaveBeenCalledWith('What is DI?', 'Inversion of control.')
    expect(await screen.findByText('Question added.')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Question added.')
    expect(screen.getByLabelText('Question')).toHaveValue('')
  })

  it('dismisses the toast after five seconds', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    try {
      mockAdd.mockResolvedValue(created)
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
      render(
        <ToastProvider>
          <AddQuestionForm />
        </ToastProvider>,
      )
      await user.type(screen.getByLabelText('Question'), 'Q?')
      await user.click(screen.getByRole('button', { name: 'Add' }))
      expect(await screen.findByText('Question added.')).toBeInTheDocument()

      await act(async () => {
        vi.advanceTimersByTime(TOAST_MS)
      })
      expect(screen.queryByText('Question added.')).not.toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })

  it('keeps the text and shows duplicate copy without Retry', async () => {
    mockAdd.mockImplementation(() => Promise.reject(new ApiError('duplicate-question', 409)))
    const user = setup()
    await user.type(screen.getByLabelText('Question'), 'Again?')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('That Question is already in the Question Bank.')
    expect(screen.queryByRole('button', { name: 'Retry' })).not.toBeInTheDocument()
    expect(screen.getByLabelText('Question')).toHaveValue('Again?')
  })

  it('shows Provider copy with Retry on failure, keeps the text, and Retry re-submits', async () => {
    mockAdd.mockImplementationOnce(() => Promise.reject(new ApiError('provider-unavailable', 503))).mockResolvedValueOnce(created)
    const user = setup()
    await user.type(screen.getByLabelText('Question'), 'Retry me?')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "I couldn't reach the Provider. Check that it is running, then try again.",
    )
    expect(screen.getByLabelText('Question')).toHaveValue('Retry me?')

    await user.click(screen.getByRole('button', { name: 'Retry' }))
    expect(await screen.findByText('Question added.')).toBeInTheDocument()
    expect(mockAdd).toHaveBeenCalledTimes(2)
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('ignores a second press while a request is in flight', async () => {
    let resolve!: (q: typeof created) => void
    mockAdd.mockReturnValue(new Promise((r) => (resolve = r)))
    const user = setup()
    await user.type(screen.getByLabelText('Question'), 'Slow?')
    await user.click(screen.getByRole('button', { name: 'Add' }))
    await user.click(screen.getByRole('button', { name: 'Adding...' }))

    expect(mockAdd).toHaveBeenCalledTimes(1)
    await act(async () => resolve(created))
  })
})
