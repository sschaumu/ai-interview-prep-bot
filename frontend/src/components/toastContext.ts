import { createContext, useContext } from 'react'

export const TOAST_MS = 5000

export type ShowToast = (message: string) => void

export const ToastContext = createContext<ShowToast>(() => {})

export const useToast = () => useContext(ToastContext)
