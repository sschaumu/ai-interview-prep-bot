import type { ButtonHTMLAttributes } from 'react'

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'destructive'
  /** While set, shows this label and ignores presses. */
  busyLabel?: string
}

export function Button({ variant = 'secondary', busyLabel, children, onClick, type = 'button', ...rest }: Props) {
  const busy = busyLabel !== undefined
  return (
    <button
      {...rest}
      type={type}
      className={`button button--${variant}`}
      aria-disabled={busy || undefined}
      onClick={(e) => {
        if (busy) {
          e.preventDefault()
          return
        }
        onClick?.(e)
      }}
    >
      {busy ? busyLabel : children}
    </button>
  )
}
