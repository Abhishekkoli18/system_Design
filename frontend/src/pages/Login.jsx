import { useState } from 'react'
import toast from 'react-hot-toast'
import { useNavigate, Link } from 'react-router-dom'
import axiosInstance from '../api/axiosInstance'
import { useAuth } from '../context/AuthContext'
import { GoogleLogin } from '@react-oauth/google'

export default function Login() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const navigate = useNavigate()
  const { login } = useAuth()

  // Same stricter email check as Register.jsx - requires a dot after the @
  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')

    if (!emailPattern.test(email)) {
      setError('Please enter a valid email address (e.g. name@example.com)')
      return
    }

    setLoading(true)

    try {
      const response = await axiosInstance.post('/auth/login', { email, password })
      login(response.data)
      toast.success('Login successful!')
      navigate(response.data.role === 'Admin' ? '/admin-home' : '/user-home')
    } catch (err) {
      const message = err.response?.data?.message || 'Login failed. Check your credentials.'
      setError(message)
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }
  // for google function logic
  async function handleGoogleSuccess(credentialResponse) {
    try {
      const response = await axiosInstance.post('/auth/google-login', {
        idToken: credentialResponse.credential,
      })

      login(response.data)
      toast.success('Login successful!')

      if (response.data.role === 'Admin') {
        navigate('/admin-home')
      } else {
        navigate('/user-home')
      }
    } catch (err) {
      toast.error(err.response?.data?.message || 'Google sign-in failed')
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h2>Login</h2>

        {error && <p className="error-text">{error}</p>}

        <label>Email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />

        <label>Password</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />

        <button type="submit" disabled={loading}>
          {loading ? 'Logging in...' : 'Login'}
        </button>

        {/* for continoue with google */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '16px 0' }}>
          <div style={{ flex: 1, height: 1, background: '#ddd' }}></div>
          <span style={{ fontSize: 12, color: '#888' }}>or</span>
          <div style={{ flex: 1, height: 1, background: '#ddd' }}></div>
        </div>

        <GoogleLogin
          onSuccess={handleGoogleSuccess}
          onError={() => toast.error('Google sign-in failed')}
        />

        <p className="switch-link">
          No account? <Link to="/register">Register</Link>
        </p>
      </form>
    </div>
  )
}
