import type { ReactNode } from 'react'
import { Button } from './Button'

type Props = {
  children: ReactNode
  /** Shown only where retrying applies. */
  onRetry?: () => void
  retryBusy?: boolean
}

/** Errors are inline, role="alert", and never auto-dismiss. */
export function InlineError({ children, onRetry, retryBusy }: Props) {
  return (
    <div className="inline-error" role="alert">
      <p>{children}</p>
      {onRetry && (
        <Button onClick={onRetry} busyLabel={retryBusy ? 'Retrying...' : undefined}>
          Retry
        </Button>
      )}
    </div>
  )
}
