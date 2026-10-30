import { useNavigate, Link } from 'react-router-dom'
import toast from 'react-hot-toast'
import { useAuth } from '../context/AuthContext'

export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const isAdmin = user?.role === 'Admin'
  const homePath = isAdmin ? '/admin-home' : '/user-home'

  function handleLogout() {
    toast.success('Logged out successfully')
    logout()
    navigate('/login')
  }

  return (
    <nav className="navbar">
      <Link to={homePath} className="brand" style={{ color: 'white', textDecoration: 'none' }}>
        Ecommerce App
      </Link>

      <div className="navbar-right">
        {isAdmin && <Link to="/admin-home" className="nav-link">Dashboard</Link>}

        <Link to="/products" className="nav-link">Products</Link>

        {!isAdmin && (
          <>
            <Link to="/cart" className="nav-link">Cart</Link>
            <Link to="/orders" className="nav-link">Orders</Link>
          </>
        )}

        <span className="role-badge">{user?.role}</span>
        <button onClick={handleLogout}>Logout</button>
      </div>
    </nav>
  )
}