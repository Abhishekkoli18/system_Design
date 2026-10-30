import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import axiosInstance from '../api/axiosInstance'
import Navbar from '../components/Navbar'

export default function OrderHistory() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    axiosInstance
      .get('/order/my-orders')
      .then((res) => setOrders(res.data))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div>
      <Navbar />
      <div className="home-page user-theme">
        <h1>My Orders</h1>
        {loading ? (
          <p>Loading...</p>
        ) : orders.length === 0 ? (
          <p>No orders yet.</p>
        ) : (
          orders.map((o) => (
            <div className="panel" key={o.id} style={{ marginBottom: 16 }}>
              <p><strong>Order #{o.id}</strong> — {o.status} · {new Date(o.createdAt).toLocaleDateString()}</p>
              <p>Subtotal: ₹{o.subTotal.toLocaleString()}</p>
              {o.discountAmount > 0 && (
                <p>Discount{o.couponCode ? ` (${o.couponCode})` : ''}: -₹{o.discountAmount.toLocaleString()}</p>
              )}
              <p><strong>Paid: ₹{o.totalAmount.toLocaleString()}</strong></p>
              <ul>
                {o.items.map((i, idx) => (
                  <li key={idx}>
                    <Link to={`/products/${i.productId}`}>{i.productName}</Link> × {i.quantity} — ₹{i.price}{' '}
                    · <Link to={`/products/${i.productId}`}>Write a review</Link>
                  </li>
                ))}
              </ul>
            </div>
          ))
        )}
      </div>
    </div>
  )
}