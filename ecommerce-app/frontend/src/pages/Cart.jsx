import { useEffect, useState } from 'react'
import axiosInstance from '../api/axiosInstance'
import Navbar from '../components/Navbar'
import toast from 'react-hot-toast'
import { useNavigate } from 'react-router-dom'

export default function Cart() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [removingId, setRemovingId] = useState(null)
  const navigate = useNavigate()

  useEffect(() => {
    loadCart()
  }, [])

  async function loadCart() {
    setLoading(true)
    try {
      const res = await axiosInstance.get('/cart')
      setItems(res.data)
    } catch (err) {
      console.error('Failed to load cart', err)
    } finally {
      setLoading(false)
    }
  }

  async function handleRemove(cartItemId) {
    setRemovingId(cartItemId)
    try {
      await axiosInstance.delete(`/cart/remove/${cartItemId}`)
      setItems((prev) => prev.filter((i) => i.cartItemId !== cartItemId))
      toast.success('Removed from cart.')
    } catch (err) {
      console.error('Failed to remove item', err)
      toast.error('Failed to remove item.')
    }
  }

  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0)

  return (
    <div>
      <Navbar />
      <div className="home-page user-theme">
        <h1>Your Cart</h1>

        {loading ? (
          <p>Loading...</p>
        ) : items.length === 0 ? (
          <p>Your cart is empty. Go to Products to add something.</p>
        ) : (
          <div className="panel" style={{ marginTop: 20 }}>
            <table className="product-table">
              <thead>
                <tr>
                  <th>Product</th>
                  <th>Price</th>
                  <th>Quantity</th>
                  <th>Subtotal</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.cartItemId}>
                    <td>{item.name}</td>
                    <td>₹{item.price.toLocaleString()}</td>
                    <td>{item.quantity}</td>
                    <td>₹{(item.price * item.quantity).toLocaleString()}</td>
                    <td>
                      <button
                        className="cancel-btn"
                        onClick={() => handleRemove(item.cartItemId)}
                        disabled={removingId === item.cartItemId}
                      >
                        {removingId === item.cartItemId ? 'Removing...' : 'Remove'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <p className="cart-total">Total: ₹{total.toLocaleString()}</p>
            <button className="add-to-cart-btn" onClick={() => navigate('/checkout')} style={{ marginTop: 16 }}>
              Proceed to Checkout
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
