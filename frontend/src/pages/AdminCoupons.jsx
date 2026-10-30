import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import axiosInstance from '../api/axiosInstance'
import Navbar from '../components/Navbar'

const emptyForm = {
  code: '', discountType: 'Percentage', value: '', maxDiscount: '',
  minOrderAmount: '0', startsAt: '', expiresAt: '', usageLimit: '',
}

export default function AdminCoupons() {
  const [coupons, setCoupons] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [saving, setSaving] = useState(false)

  function load() {
    axiosInstance.get('/coupon').then((res) => setCoupons(res.data)).catch(() => toast.error('Could not load coupons'))
  }
  useEffect(() => { load() }, [])

  function set(field, value) {
    setForm((f) => ({ ...f, [field]: value }))
  }

  async function create(e) {
    e.preventDefault()
    setSaving(true)
    try {
      await axiosInstance.post('/coupon', {
        code: form.code,
        discountType: form.discountType,
        value: parseFloat(form.value),
        maxDiscount: form.maxDiscount ? parseFloat(form.maxDiscount) : null,
        minOrderAmount: parseFloat(form.minOrderAmount || '0'),
        startsAt: form.startsAt ? new Date(form.startsAt).toISOString() : null,     // local time to UTC
        expiresAt: form.expiresAt ? new Date(form.expiresAt).toISOString() : null,
        usageLimit: form.usageLimit ? parseInt(form.usageLimit, 10) : null,
        isActive: true,
      })
      toast.success('Coupon created')
      setForm(emptyForm)
      load()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not create the coupon')
    } finally {
      setSaving(false)
    }
  }

  async function toggle(id) {
    try {
      const res = await axiosInstance.post(`/coupon/${id}/toggle`)
      toast.success(res.data.message)
      load()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed')
    }
  }

  return (
    <div>
      <Navbar />
      <div className="home-page admin-theme">
        <h1>Coupons</h1>

        <div className="panel">
          <h2>New coupon</h2>
          <form onSubmit={create} className="inline-form">
            <label>Code</label>
            <input value={form.code} onChange={(e) => set('code', e.target.value)} required maxLength={30} />

            <label>Type</label>
            <select value={form.discountType} onChange={(e) => set('discountType', e.target.value)}>
              <option value="Percentage">Percentage (%)</option>
              <option value="Fixed">Fixed amount (₹)</option>
            </select>

            <label>Value</label>
            <input type="number" step="0.01" min="0.01" value={form.value} onChange={(e) => set('value', e.target.value)} required />

            <label>Max discount (₹, optional)</label>
            <input type="number" step="0.01" min="0" value={form.maxDiscount} onChange={(e) => set('maxDiscount', e.target.value)} />

            <label>Minimum order (₹)</label>
            <input type="number" step="0.01" min="0" value={form.minOrderAmount} onChange={(e) => set('minOrderAmount', e.target.value)} />

            <label>Starts at (optional)</label>
            <input type="datetime-local" value={form.startsAt} onChange={(e) => set('startsAt', e.target.value)} />

            <label>Expires at (optional)</label>
            <input type="datetime-local" value={form.expiresAt} onChange={(e) => set('expiresAt', e.target.value)} />

            <label>Total usage limit (optional)</label>
            <input type="number" min="1" value={form.usageLimit} onChange={(e) => set('usageLimit', e.target.value)} />

            <button type="submit" disabled={saving}>{saving ? 'Saving...' : 'Create coupon'}</button>
          </form>
        </div>

        <div className="panel" style={{ marginTop: 24 }}>
          <h2>All coupons ({coupons.length})</h2>
          <table className="product-table">
            <thead>
              <tr><th>Code</th><th>Discount</th><th>Min order</th><th>Used</th><th>Expires</th><th>Status</th><th></th></tr>
            </thead>
            <tbody>
              {coupons.map((c) => (
                <tr key={c.id}>
                  <td>{c.code}</td>
                  <td>{c.discountType === 'Percentage' ? `${c.value}%` : `₹${c.value}`}{c.maxDiscount ? ` (max ₹${c.maxDiscount})` : ''}</td>
                  <td>₹{c.minOrderAmount}</td>
                  <td>{c.usedCount}{c.usageLimit ? ` / ${c.usageLimit}` : ''}</td>
                  <td>{c.expiresAt ? new Date(c.expiresAt).toLocaleString() : 'Never'}</td>
                  <td>{c.isActive ? 'Active' : 'Inactive'}</td>
                  <td>
                    <button className={c.isActive ? 'cancel-btn' : 'edit-btn'} onClick={() => toggle(c.id)}>
                      {c.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}