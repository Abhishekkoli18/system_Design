import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import axiosInstance from '../api/axiosInstance'
import Navbar from '../components/Navbar'

export default function Checkout() {
  const navigate = useNavigate()
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [couponInput, setCouponInput] = useState('')
  const [applied, setApplied] = useState(null)      // the preview from the server, when the coupon is valid
  const [applying, setApplying] = useState(false)
  const [paying, setPaying] = useState(false)

  useEffect(() => {
    axiosInstance
      .get('/cart')
      .then((res) => setItems(res.data))
      .catch(() => toast.error('Could not load your cart'))
      .finally(() => setLoading(false))
  }, [])

  const subTotal = items.reduce((sum, i) => sum + i.price * i.quantity, 0)
  const discount = applied ? applied.discount : 0
  const total = subTotal - discount

  async function applyCoupon() {
    if (!couponInput.trim()) return
    setApplying(true)
    try {
      const res = await axiosInstance.post('/coupon/validate', { code: couponInput })
      if (res.data.valid) {
        setApplied(res.data)
        toast.success(res.data.message)
      } else {
        setApplied(null)
        toast.error(res.data.message)
      }
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not check the coupon')
    } finally {
      setApplying(false)
    }
  }

  function removeCoupon() {
    setApplied(null)
    setCouponInput('')
  }

  async function handlePayNow() {
    setPaying(true)
    try {
      // the server prices the cart again and re-checks the coupon: it never trusts the numbers on this page
      const res = await axiosInstance.post('/order/create', { couponCode: applied ? applied.code : null })
      const { razorpayOrderId, keyId, amountInPaise } = res.data

      const options = {
        key: keyId,
        amount: amountInPaise,
        currency: 'INR',
        name: 'Ecommerce App',
        order_id: razorpayOrderId,
        handler: async function (response) {
          try {
            await axiosInstance.post('/order/verify', {
              razorpayOrderId: response.razorpay_order_id,
              razorpayPaymentId: response.razorpay_payment_id,
              razorpaySignature: response.razorpay_signature,
            })
            toast.success('Order placed successfully!')
            navigate('/orders')
          } catch (err) {
            toast.error(err.response?.data?.message || 'Payment verification failed')
          }
        },
        modal: { ondismiss: () => toast('Payment cancelled') },
        theme: { color: '#4f46e5' },
      }
      new window.Razorpay(options).open()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not start checkout')
    } finally {
      setPaying(false)
    }
  }

  return (
    <div>
      <Navbar />
      <div className="home-page user-theme">
        <h1>Checkout</h1>

        {loading ? (
          <p>Loading...</p>
        ) : items.length === 0 ? (
          <p>Your cart is empty. <Link to="/products">Browse products</Link></p>
        ) : (
          <div className="checkout-panel">
            <ul className="checkout-items">
              {items.map((i, idx) => (
                <li key={idx}>
                  {i.name} × {i.quantity}
                  <span>₹{(i.price * i.quantity).toLocaleString()}</span>
                </li>
              ))}
            </ul>

            <div className="coupon-row">
              <input
                placeholder="Coupon code"
                value={couponInput}
                onChange={(e) => setCouponInput(e.target.value)}
                disabled={!!applied}
              />
              {applied ? (
                <button className="cancel-btn" onClick={removeCoupon}>Remove</button>
              ) : (
                <button className="edit-btn" onClick={applyCoupon} disabled={applying}>
                  {applying ? 'Checking...' : 'Apply'}
                </button>
              )}
            </div>

            <div className="price-lines">
              <p><span>Subtotal</span><span>₹{subTotal.toLocaleString()}</span></p>
              {applied && (
                <p className="discount-line">
                  <span>Discount ({applied.code})</span>
                  <span>-₹{discount.toLocaleString()}</span>
                </p>
              )}
              <p className="total-line"><span>Total</span><span>₹{total.toLocaleString()}</span></p>
            </div>

            <button className="add-to-cart-btn" onClick={handlePayNow} disabled={paying}>
              {paying ? 'Starting...' : `Pay ₹${total.toLocaleString()}`}
            </button>
          </div>
        )}
      </div>
    </div>
  )
}