import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import axiosInstance from '../api/axiosInstance'
import { useAuth } from '../context/AuthContext'
import StarRating from '../components/StarRating'

export default function ReviewSection({ productId }) {
  const { user } = useAuth()
  const [data, setData] = useState(null)
  const [page, setPage] = useState(1)
  const [rating, setRating] = useState(5)
  const [comment, setComment] = useState('')
  const [editing, setEditing] = useState(false)
  const [saving, setSaving] = useState(false)

  function load() {
    axiosInstance
      .get(`/review/product/${productId}`, { params: { pageNumber: page, pageSize: 5 } })
      .then((res) => setData(res.data))
      .catch(() => setData(null))
  }

  useEffect(() => { setPage(1); setEditing(false) }, [productId])
  useEffect(() => { load() }, [productId, page])

  async function submit(e) {
    e.preventDefault()
    setSaving(true)
    try {
      if (editing) {
        await axiosInstance.put(`/review/${data.myReview.id}`, { rating, comment })
        toast.success('Review updated')
      } else {
        await axiosInstance.post('/review', { productId: Number(productId), rating, comment })
        toast.success('Thanks for your review!')
      }
      setEditing(false)
      setComment('')
      setRating(5)
      load()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not save review')
    } finally {
      setSaving(false)
    }
  }

  function startEdit() {
    setRating(data.myReview.rating)
    setComment(data.myReview.comment)
    setEditing(true)
  }

  async function remove(id) {
    if (!window.confirm('Delete this review?')) return
    try {
      await axiosInstance.delete(`/review/${id}`)
      toast.success('Review deleted')
      load()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not delete review')
    }
  }

  if (!data) return null
  const { summary, reviews, canReview, myReview } = data
  const isAdmin = user?.role === 'Admin'

  return (
    <div className="reviews" id="reviews">
      <h2>Customer reviews</h2>

      {summary.count === 0 ? (
        <p>No reviews yet.</p>
      ) : (
        <div className="review-summary">
          <div className="review-average">
            <span className="review-average-number">{summary.average}</span>
            <StarRating value={summary.average} />
            <span>{summary.count} review{summary.count === 1 ? '' : 's'}</span>
          </div>
          <div className="review-bars">
            {[5, 4, 3, 2, 1].map((star) => {
              const count = summary.breakdown[star - 1]
              const pct = (count / summary.count) * 100
              return (
                <div className="review-bar-row" key={star}>
                  <span>{star}★</span>
                  <div className="review-bar"><div className="review-bar-fill" style={{ width: `${pct}%` }} /></div>
                  <span>{count}</span>
                </div>
              )
            })}
          </div>
        </div>
      )}

      {canReview || editing ? (
        <form className="review-form" onSubmit={submit}>
          <h3>{editing ? 'Edit your review' : 'Write a review'}</h3>
          <StarRating value={rating} onChange={setRating} size={28} />
          <textarea
            placeholder="What did you think of this product?"
            value={comment}
            maxLength={1000}
            onChange={(e) => setComment(e.target.value)}
          />
          <div>
            <button className="add-to-cart-btn" disabled={saving}>
              {saving ? 'Saving...' : editing ? 'Update review' : 'Submit review'}
            </button>{' '}
            {editing && <button type="button" className="cancel-btn" onClick={() => setEditing(false)}>Cancel</button>}
          </div>
        </form>
      ) : (
        !myReview && <p className="hint-text">Only customers who bought this product can review it.</p>
      )}

      {reviews.items.map((r) => (
        <div className="review-item" key={r.id}>
          <StarRating value={r.rating} size={16} />
          <p className="review-meta">
            {r.userName} · {new Date(r.createdAt).toLocaleDateString()} · Verified purchase
          </p>
          {r.comment && <p>{r.comment}</p>}
          {r.isMine && <button className="edit-btn" onClick={startEdit}>Edit</button>}{' '}
          {(r.isMine || isAdmin) && <button className="cancel-btn" onClick={() => remove(r.id)}>Delete</button>}
        </div>
      ))}

      {reviews.totalPages > 1 && (
        <div className="pagination-bar">
          <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}>«</button>
          <span>Page {page} of {reviews.totalPages}</span>
          <button onClick={() => setPage((p) => Math.min(reviews.totalPages, p + 1))} disabled={page >= reviews.totalPages}>»</button>
        </div>
      )}
    </div>
  )
}