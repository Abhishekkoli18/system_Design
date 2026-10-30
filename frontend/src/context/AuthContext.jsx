import { createContext, useContext, useState } from 'react'

// Context = a way to share data (like "who is logged in") with any
// component in the tree, without manually passing it down through props.
const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  // On first load, check localStorage in case the user refreshed the page -
  // this keeps them logged in across refreshes.
  const [user, setUser] = useState(() => {
    const saved = localStorage.getItem('user')
    return saved ? JSON.parse(saved) : null
  })

  // Called after a successful login/register.
  function login(authResponse) {
    const userData = {
      fullName: authResponse.fullName,
      role: authResponse.role,
    }
    localStorage.setItem('token', authResponse.token)
    localStorage.setItem('user', JSON.stringify(userData))
    setUser(userData)
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

// Custom hook - lets any component do: const { user, login, logout } = useAuth()
export function useAuth() {
  return useContext(AuthContext)
}
