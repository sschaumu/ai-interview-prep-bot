import { EmptyState } from '../components/EmptyState'

/** Placeholder: the interview surface arrives in Epic 2. */
export function InterviewPage() {
  return (
    <>
      <h1 tabIndex={-1}>Interview</h1>
      <EmptyState>The interview isn't available yet.</EmptyState>
    </>
  )
}
