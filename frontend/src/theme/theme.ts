export type ThemeMode = 'system' | 'light' | 'dark'

export const STORAGE_KEY = 'theme'

const NEXT: Record<ThemeMode, ThemeMode> = { system: 'light', light: 'dark', dark: 'system' }

export const nextMode = (mode: ThemeMode): ThemeMode => NEXT[mode]

export function readSavedMode(): ThemeMode {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved === 'light' || saved === 'dark') return saved
  } catch {
    // Storage unavailable: fall back to following the system.
  }
  return 'system'
}

/** Light and Dark are saved; System clears the saved choice. */
export function saveMode(mode: ThemeMode): void {
  try {
    if (mode === 'system') localStorage.removeItem(STORAGE_KEY)
    else localStorage.setItem(STORAGE_KEY, mode)
  } catch {
    // Ignore: the choice still applies for this page view.
  }
}

export function applyMode(mode: ThemeMode): void {
  const root = document.documentElement
  if (mode === 'system') root.removeAttribute('data-theme')
  else root.setAttribute('data-theme', mode)
}
