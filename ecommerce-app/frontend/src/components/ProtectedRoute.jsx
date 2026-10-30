import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

// Wraps a page. If nobody is logged in, redirect to /login.
// If "role" is passed and doesn't match, redirect away too -
// e.g. a normal User trying to type /admin-home in the URL bar directly.
export default function ProtectedRoute({ children, role }) {
  const { user } = useAuth()

  if (!user) {
    return <Navigate to="/login" replace />
  }

  if (role && user.role !== role) {
    return <Navigate to="/login" replace />
  }

  return children
}
