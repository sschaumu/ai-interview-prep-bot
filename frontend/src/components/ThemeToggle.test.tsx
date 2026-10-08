import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ThemeToggle } from './ThemeToggle'

const root = document.documentElement

describe('ThemeToggle', () => {
  it('defaults to System with no data-theme and an accessible name naming the next action', () => {
    render(<ThemeToggle />)
    expect(screen.getByRole('button', { name: 'Theme: System. Switch to Light.' })).toBeInTheDocument()
    expect(root).not.toHaveAttribute('data-theme')
  })

  it('cycles System -> Light -> Dark -> System, applying and persisting instantly', async () => {
    const user = userEvent.setup()
    render(<ThemeToggle />)

    await user.click(screen.getByRole('button'))
    expect(root).toHaveAttribute('data-theme', 'light')
    expect(localStorage.getItem('theme')).toBe('light')
    expect(screen.getByRole('button', { name: 'Theme: Light. Switch to Dark.' })).toBeInTheDocument()

    await user.click(screen.getByRole('button'))
    expect(root).toHaveAttribute('data-theme', 'dark')
    expect(localStorage.getItem('theme')).toBe('dark')
    expect(screen.getByRole('button', { name: 'Theme: Dark. Switch to System.' })).toBeInTheDocument()

    await user.click(screen.getByRole('button'))
    expect(root).not.toHaveAttribute('data-theme')
    expect(localStorage.getItem('theme')).toBeNull()
  })

  it('starts from a saved choice', () => {
    localStorage.setItem('theme', 'dark')
    render(<ThemeToggle />)
    expect(screen.getByRole('button', { name: 'Theme: Dark. Switch to System.' })).toBeInTheDocument()
  })

  it('falls back to System when storage throws', async () => {
    const original = Storage.prototype.getItem
    Storage.prototype.getItem = () => {
      throw new Error('blocked')
    }
    try {
      render(<ThemeToggle />)
      expect(screen.getByRole('button', { name: 'Theme: System. Switch to Light.' })).toBeInTheDocument()
    } finally {
      Storage.prototype.getItem = original
    }
  })
})
