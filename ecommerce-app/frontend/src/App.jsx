import { Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import Login from './pages/Login'
import Register from './pages/Register'
import UserHome from './pages/UserHome'
import AdminHome from './pages/AdminHome'
import Products from './pages/Products'
import Cart from './pages/Cart'
import ProtectedRoute from './components/ProtectedRoute'
import Checkout from './pages/Checkout'
import OrderHistory from './pages/OrderHistory'

function App() {
  return (
    <>
      <Toaster position="top-right" />
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/checkout" element={<ProtectedRoute><Checkout /></ProtectedRoute>} />
        <Route path="/orders" element={<ProtectedRoute><OrderHistory /></ProtectedRoute>} />

        <Route
          path="/user-home"
          element={
            <ProtectedRoute role="User">
              <UserHome />
            </ProtectedRoute>
          }
        />

        {/* No role restriction on purpose - both User and Admin can browse products and use the cart */}
        <Route
          path="/products"
          element={
            <ProtectedRoute>
              <Products />
            </ProtectedRoute>
          }
        />

        <Route
          path="/cart"
          element={
            <ProtectedRoute>
              <Cart />
            </ProtectedRoute>
          }
        />

        <Route
          path="/admin-home"
          element={
            <ProtectedRoute role="Admin">
              <AdminHome />
            </ProtectedRoute>
          }
        />
      </Routes>
    </>
  )
}

export default App
