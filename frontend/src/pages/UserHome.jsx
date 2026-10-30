import { useEffect, useState } from 'react'
import axiosInstance from '../api/axiosInstance'
import { useAuth } from '../context/AuthContext'
import Navbar from '../components/Navbar'

export default function UserHome() {
  const { user } = useAuth()
  const [message, setMessage] = useState('')
  const [products, setProducts] = useState([])
  const [loadingProducts, setLoadingProducts] = useState(true)

  useEffect(() => {
    // Calls a PROTECTED endpoint - axiosInstance automatically attaches
    // the JWT token via the interceptor we set up.
    axiosInstance.get('/home/user').then((res) => {
      setMessage(res.data.message)
    })

    // Same /api/product GET endpoint the Admin uses to list products -
    // it's open to any logged-in user (User or Admin), just not the
    // add/upload endpoints, which are Admin-only.
    axiosInstance
      .get('/product', { params: { pageSize: 1000 } })
      .then((res) => setProducts(res.data.items))
      .finally(() => setLoadingProducts(false))
  }, [])

  return (
    <div>
      <Navbar />
      <div className="home-page user-theme">
        <h1>User Dashboard</h1>
        <p>Welcome back, {user?.fullName}!</p>
        <p className="server-message">{message}</p>

        <div className="panel" style={{ marginTop: 24 }}>
          <h2>Available products ({products.length})</h2>
          {loadingProducts ? (
            <p>Loading...</p>
          ) : products.length === 0 ? (
            <p>No products available yet.</p>
          ) : (
            <table className="product-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Price</th>
                  <th>In stock</th>
                </tr>
              </thead>
              <tbody>
                {products.map((p) => (
                  <tr key={p.id}>
                    <td>{p.name}</td>
                    <td>₹{p.price}</td>
                    <td>{p.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}
