import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import App from './App'

vi.mock('./api/questionBank', () => ({ addQuestion: vi.fn() }))

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

describe('shell', () => {
  it('redirects / to /interview and marks the active nav link', () => {
    renderAt('/')

    expect(screen.getByRole('heading', { level: 1, name: 'Interview' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Interview' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('link', { name: 'Question Bank' })).not.toHaveAttribute('aria-current')
  })

  it('shows the product name, both nav links and the Theme toggle', () => {
    renderAt('/interview')

    expect(screen.getByText('AI Interview Prep Bot')).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Main' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^Theme:/ })).toBeInTheDocument()
  })

  it('moves focus to the new view h1 on navigation', async () => {
    renderAt('/interview')
    await userEvent.setup().click(screen.getByRole('link', { name: 'Question Bank' }))

    const h1 = await screen.findByRole('heading', { level: 1, name: 'Question Bank' })
    expect(h1).toHaveFocus()
    expect(screen.getByRole('heading', { level: 2, name: 'Add a Question' })).toBeInTheDocument()
  })
})
