import { useState } from 'react'
import Login from './pages/Login'
import Register from './pages/Register'
import CitizenDashboard from './pages/CitizenDashboard'
import './App.css'
import GovernmentDashboard from './pages/GovernmentDashboard'

function App() {
  const [user, setUser] = useState(null)
  const [showRegister, setShowRegister] = useState(false)
  const [useEF, setUseEF] = useState(true) // true = EF, false = SP

  const handleLoginSuccess = (userData) => {
    setUser(userData)
  }

  const handleLogout = () => {
    setUser(null)
  }

  const toggleImplementation = () => {
    setUseEF(!useEF)
  }

  const handleShowRegister = () => {
    setShowRegister(true)
  }

  const handleBackToLogin = () => {
    setShowRegister(false)
  }

  const handleRegisterSuccess = () => {
    setShowRegister(false)
  }

  return (
    <>
      {!user ? (
        showRegister ? (
          <Register
            onRegisterSuccess={handleRegisterSuccess}
            onBackToLogin={handleBackToLogin}
          />
        ) : (
          <Login
            onLoginSuccess={handleLoginSuccess}
            onShowRegister={handleShowRegister}
          />
        )
      ) : (
        user.roleID == 2 ?
          <CitizenDashboard
            user={user}
            onLogout={handleLogout}
            useEF={useEF}
            onToggleImplementation={toggleImplementation}
          />
          : user.roleID == 1 ?
            <GovernmentDashboard
              user={user}
              onLogout={handleLogout}
              useEF={useEF}
              onToggleImplementation={toggleImplementation}
            />
            : <div>Role not recognized.</div>
      )}
    </>
  )
}

export default App
