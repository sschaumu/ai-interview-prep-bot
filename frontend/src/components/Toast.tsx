import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { TOAST_MS, ToastContext, type ShowToast } from './toastContext'

type Toast = { id: number; message: string }

/** Confirmation toasts: role="status", auto-dismiss after 5 seconds. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const nextId = useRef(0)
  const timers = useRef(new Set<ReturnType<typeof setTimeout>>())

  const show = useCallback<ShowToast>((message) => {
    const id = nextId.current++
    setToasts((current) => [...current, { id, message }])
    const timer = setTimeout(() => {
      timers.current.delete(timer)
      setToasts((current) => current.filter((t) => t.id !== id))
    }, TOAST_MS)
    timers.current.add(timer)
  }, [])

  useEffect(() => {
    const active = timers.current
    return () => active.forEach(clearTimeout)
  }, [])

  return (
    <ToastContext.Provider value={show}>
      {children}
      <div className="toast-region" role="status">
        {toasts.map((t) => (
          <div key={t.id} className="toast">
            {t.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
