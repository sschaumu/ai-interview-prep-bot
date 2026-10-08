import { useState } from 'react'
import { applyMode, nextMode, readSavedMode, saveMode, type ThemeMode } from '../theme/theme'

const LABEL: Record<ThemeMode, string> = { system: 'System', light: 'Light', dark: 'Dark' }

function Icon({ mode }: { mode: ThemeMode }) {
  const common = { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeWidth: 2, 'aria-hidden': true }
  if (mode === 'light')
    return (
      <svg {...common}>
        <circle cx="12" cy="12" r="4" />
        <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
      </svg>
    )
  if (mode === 'dark')
    return (
      <svg {...common}>
        <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" />
      </svg>
    )
  return (
    <svg {...common}>
      <rect x="3" y="4" width="18" height="12" rx="2" />
      <path d="M8 20h8M12 16v4" />
    </svg>
  )
}

export function ThemeToggle() {
  const [mode, setMode] = useState<ThemeMode>(readSavedMode)
  const next = nextMode(mode)

  function cycle() {
    setMode(next)
    applyMode(next)
    saveMode(next)
  }

  return (
    <button
      type="button"
      className="theme-toggle"
      aria-label={`Theme: ${LABEL[mode]}. Switch to ${LABEL[next]}.`}
      onClick={cycle}
    >
      <Icon mode={mode} />
      <span className="theme-toggle__label" aria-hidden="true">
        {LABEL[mode]}
      </span>
    </button>
  )
}
