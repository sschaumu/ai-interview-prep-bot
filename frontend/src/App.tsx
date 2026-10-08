import { useEffect, useRef } from 'react'
import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router'
import { Header } from './components/Header'
import { ToastProvider } from './components/Toast'
import { InterviewPage } from './pages/InterviewPage'
import { QuestionBankPage } from './pages/QuestionBankPage'

function Layout() {
  const { pathname } = useLocation()
  const first = useRef(true)

  // A route change moves focus to the new view's h1.
  useEffect(() => {
    if (first.current) {
      first.current = false
      return
    }
    document.querySelector<HTMLElement>('main h1')?.focus()
  }, [pathname])

  return (
    <ToastProvider>
      <Header />
      <main className="page">
        <Outlet />
      </main>
    </ToastProvider>
  )
}

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Navigate to="/interview" replace />} />
        <Route path="/interview" element={<InterviewPage />} />
        <Route path="/question-bank" element={<QuestionBankPage />} />
      </Route>
    </Routes>
  )
}
