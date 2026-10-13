import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import axiosInstance from '../api/axiosInstance'
import { useAuth } from '../context/AuthContext'

export default function Register() {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const navigate = useNavigate()
  const { login } = useAuth()

  // Stricter email check than the browser's built-in type="email":
  // requires a dot after the @ (so "abc@abc" is rejected, "abc@abc.com" passes)
  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  async function handleSubmit(e) {
    e.preventDefault() // stop the browser's default full-page-reload form submit
    setError('')

    if (!emailPattern.test(email)) {
      setError('Please enter a valid email address (e.g. name@example.com)')
      return
    }

    setLoading(true)

    try {
      const response = await axiosInstance.post('/auth/register', { fullName, email, password })
      login(response.data)
      toast.success('Account created successfully!')
      navigate(response.data.role === 'Admin' ? '/admin-home' : '/user-home')
    } catch (err) {
      const message = err.response?.data?.message || 'Registration failed. Try again.'
      setError(message)
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h2>Create account</h2>

        {error && <p className="error-text">{error}</p>}

        <label>Full name</label>
        <input value={fullName} onChange={(e) => setFullName(e.target.value)} required />

        <label>Email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />

        <label>Password</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />

        <button type="submit" disabled={loading}>
          {loading ? 'Creating account...' : 'Register'}
        </button>

        <p className="switch-link">
          Already have an account? <Link to="/login">Login</Link>
        </p>
      </form>
    </div>
  )
}
