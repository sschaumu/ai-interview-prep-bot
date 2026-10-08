import { NavLink } from 'react-router'
import { ThemeToggle } from './ThemeToggle'

export function Header() {
  return (
    <header className="header">
      <div className="page header__inner">
        <p className="header__name">AI Interview Prep Bot</p>
        <nav className="nav" aria-label="Main">
          <NavLink className="nav__link" to="/interview">
            Interview
          </NavLink>
          <NavLink className="nav__link" to="/question-bank">
            Question Bank
          </NavLink>
        </nav>
        <ThemeToggle />
      </div>
    </header>
  )
}
