import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import axiosInstance from '../api/axiosInstance'
import Navbar from '../components/Navbar'

export default function Checkout() {
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  async function handlePayNow() {
    setLoading(true)
    try {
      // Step 1: ask OUR backend to create a Razorpay order
      const res = await axiosInstance.post('/order/create')
      const { razorpayOrderId, keyId, amountInPaise } = res.data

      // Step 2: open RAZORPAY'S popup, using their config shape
      const options = {
        key: keyId,
        amount: amountInPaise,
        currency: 'INR',
        name: 'Ecommerce App',
        order_id: razorpayOrderId,
        handler: async function (response) {
          // Step 3: Razorpay's popup succeeded - send its proof to OUR backend to verify
          try {
            await axiosInstance.post('/order/verify', {
              razorpayOrderId: response.razorpay_order_id,
              razorpayPaymentId: response.razorpay_payment_id,
              razorpaySignature: response.razorpay_signature,
            })
            toast.success('Order placed successfully!')
            navigate('/orders')
          } catch (err) {
            toast.error('Payment verification failed.')
          }
        },
        theme: { color: '#4f46e5' },
      }

      const rzp = new window.Razorpay(options)
      rzp.open()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not start checkout.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <Navbar />
      <div className="home-page user-theme">
        <h1>Checkout</h1>
        <p>Click below to pay securely via Razorpay (Test Mode).</p>
        <button className="add-to-cart-btn" onClick={handlePayNow} disabled={loading}>
          {loading ? 'Starting...' : 'Pay Now'}
        </button>
      </div>
    </div>
  )
}