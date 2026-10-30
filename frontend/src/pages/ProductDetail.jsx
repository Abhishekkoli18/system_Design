import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import toast from 'react-hot-toast'
import axiosInstance from '../api/axiosInstance'
import Navbar from '../components/Navbar'
import ReviewSection from '../components/ReviewSection'
import StarRating from '../components/StarRating'

export default function ProductDetail() {
  const { id } = useParams()                       // the 5 in /products/5
  const [product, setProduct] = useState(null)
  const [related, setRelated] = useState([])
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [quantity, setQuantity] = useState(1)
  const [adding, setAdding] = useState(false)

  // Runs on first load AND whenever the id in the URL changes (clicking a related product)
  useEffect(() => {
    let cancelled = false            // ignore answers that arrive after the user already moved on
    setLoading(true)
    setNotFound(false)
    setQuantity(1)
    window.scrollTo(0, 0)

    axiosInstance
      .get(`/product/${id}`)
      .then((res) => { if (!cancelled) setProduct(res.data) })
      .catch(() => { if (!cancelled) { setProduct(null); setNotFound(true) } })
      .finally(() => { if (!cancelled) setLoading(false) })

    axiosInstance
      .get(`/product/${id}/related`, { params: { count: 6 } })
      .then((res) => { if (!cancelled) setRelated(res.data) })
      .catch(() => { if (!cancelled) setRelated([]) })   // recommendations are optional

    return () => { cancelled = true }
  }, [id])

  function changeQuantity(delta) {
    setQuantity((q) => Math.min(product.quantity, Math.max(1, q + delta)))
  }

  async function handleAddToCart() {
    if (product.quantity <= 0) {
      toast.error('Out of stock')
      return
    }
    setAdding(true)
    try {
      await axiosInstance.post('/cart/add', { productId: product.id, quantity })
      toast.success('Added to cart!')
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to add')
    } finally {
      setAdding(false)
    }
  }

  return (
    <div>
      <Navbar />
      <div className="home-page user-theme">
        <Link to="/products" className="back-link">← Back to products</Link>

        {loading ? (
          <p>Loading...</p>
        ) : notFound || !product ? (
          <p>This product does not exist.</p>
        ) : (
          <>
            <div className="detail-layout">
              <div className="detail-image">
                {product.imageUrl ? (
                  <img src={product.imageUrl} alt={product.name} />
                ) : (
                  <div className="product-image-placeholder">No image</div>
                )}
              </div>

              <div className="detail-info">
                <h1>{product.name}</h1>
                {product.reviewCount > 0 && (
                  <p><StarRating value={product.averageRating} /> {product.averageRating} ({product.reviewCount} reviews)</p>
                )}
                {product.category && <p className="product-category">{product.category}</p>}
                <p className="detail-price">₹{product.price.toLocaleString()}</p>
                <p className="product-stock">
                  {product.quantity > 0 ? `In stock: ${product.quantity}` : 'Out of stock'}
                </p>
                <p>{product.description}</p>

                <div className="detail-actions">
                  <div className="qty-stepper">
                    <button onClick={() => changeQuantity(-1)}>-</button>
                    <span>{quantity}</span>
                    <button
                      onClick={() => changeQuantity(1)}
                      disabled={quantity >= product.quantity}
                    >
                      +
                    </button>
                  </div>
                  <button
                    className="add-to-cart-btn"
                    onClick={handleAddToCart}
                    disabled={adding || product.quantity <= 0}
                  >
                    {product.quantity <= 0 ? 'Out of Stock' : adding ? 'Adding...' : 'Add to Cart'}
                  </button>
                </div>
              </div>
            </div>

            {related.length > 0 && (
              <>
                <h2 className="related-title">More in {product.category}</h2>
                <div className="product-grid">
                  {related.map((r) => (
                    <Link to={`/products/${r.id}`} className="product-card card-link" key={r.id}>
                      <div className="product-image-wrap">
                        {r.imageUrl ? (
                          <img src={r.imageUrl} alt={r.name} className="product-image" />
                        ) : (
                          <div className="product-image-placeholder">No image</div>
                        )}
                      </div>
                      <h3>{r.name}</h3>
                      <p className="product-price">₹{r.price.toLocaleString()}</p>
                      <p className="product-stock">Stock: {r.quantity}</p>
                    </Link>
                  ))}
                </div>
              </>
            )}

            <ReviewSection productId={id} />
          </>
        )}
      </div>
    </div>
  )
}