import { useEffect, useState } from 'react'
import axiosInstance from '../api/axiosInstance'
import Navbar from '../components/Navbar'
import toast from 'react-hot-toast'
import { getPageNumbers } from '../utils/pagination'

export default function Products() {
  const [products, setProducts] = useState([])
  const [categories, setCategories] = useState([])
  const [loading, setLoading] = useState(true)
  const [quantities, setQuantities] = useState({}) // { productId: qty }
  const [statusByProduct, setStatusByProduct] = useState({}) // { productId: 'Added!' }

  // Search + filter state
  const [searchTerm, setSearchTerm] = useState('')
  const [minPrice, setMinPrice] = useState('')
  const [maxPrice, setMaxPrice] = useState('')
  const [category, setCategory] = useState('')
  const [inStockOnly, setInStockOnly] = useState(false)

  const [pageNumber, setPageNumber] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const pageSize = 12

  // Load the category dropdown options once, on page load
  useEffect(() => {
    axiosInstance
      .get('/product/categories')
      .then((res) => setCategories(res.data))
      .catch(() => setCategories([]))
  }, [])

  // Re-fetch the product list whenever any search/filter value changes
  useEffect(() => {
    loadProducts()
  }, [searchTerm, minPrice, maxPrice, category, inStockOnly, pageNumber])

  function loadProducts() {
    setLoading(true)
    axiosInstance
      .get('/product', {
        params: {
          search: searchTerm || undefined,
          minPrice: minPrice || undefined,
          maxPrice: maxPrice || undefined,
          category: category || undefined,
          inStockOnly: inStockOnly || undefined,
          pageNumber,
          pageSize,
        },
      })
      .then((res) => {
        setProducts(res.data.items)           // ← was res.data, now res.data.items
        setTotalPages(res.data.totalPages)     // ← new
        setQuantities((prev) => {
          const updated = { ...prev }
          res.data.items.forEach((p) => {      // ← also .items here
            if (!(p.id in updated)) updated[p.id] = 1
          })
          return updated
        })
      })
      .finally(() => setLoading(false))
  }

  function changeQuantity(productId, delta, maxStock) {
    setQuantities((prev) => {
      const current = prev[productId] || 1
      // never go below 1, never go above available stock
      const next = Math.min(maxStock, Math.max(1, current + delta))
      return { ...prev, [productId]: next }
    })
  }

  async function handleAddToCart(productId, availableStock) {
    const quantity = quantities[productId] || 1

    if (availableStock <= 0) {
      toast.error('Out of stock')
      setStatusByProduct((prev) => ({ ...prev, [productId]: 'Out of stock' }))
      return
    }

    if (quantity > availableStock) {
      toast.error(`Only ${availableStock} left`)
      setStatusByProduct((prev) => ({ ...prev, [productId]: `Only ${availableStock} left` }))
      return
    }

    try {
      await axiosInstance.post('/cart/add', { productId, quantity })
      setStatusByProduct((prev) => ({ ...prev, [productId]: 'Added!' }))
      toast.success('Added to cart!')
      setTimeout(() => {
        setStatusByProduct((prev) => ({ ...prev, [productId]: '' }))
      }, 1500)
    } catch (err) {
      const message = err.response?.data?.message || 'Failed to add'
      toast.error(message)
      setStatusByProduct((prev) => ({ ...prev, [productId]: message }))
    }
  }

  function clearFilters() {
    setSearchTerm('')
    setMinPrice('')
    setMaxPrice('')
    setCategory('')
    setInStockOnly(false)
    setPageNumber(1)
  }
  return (
    <div>
      <Navbar />
      <div className="home-page user-theme">
        <h1>Products</h1>

        <input
          type="text"
          placeholder="Search products..."
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setPageNumber(1) }}
          className="search-input"
        />  

        <div className="filter-bar">
          <input
            type="number"
            placeholder="Min price"
            value={minPrice}
            onChange={(e) => { setMinPrice(e.target.value); setPageNumber(1) }}
          />
          <input
            type="number"
            placeholder="Max price"
            value={maxPrice}
            onChange={(e) => { setMaxPrice(e.target.value); setPageNumber(1) }}
          />

          <select value={category} onChange={(e) => { setCategory(e.target.value); setPageNumber(1) }}>
            <option value="">All categories</option>
            {categories.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>

          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={inStockOnly}
              onChange={(e) => { setInStockOnly(e.target.checked); setPageNumber(1) }}
            />
            In stock only
          </label>

          <button className="cancel-btn" onClick={clearFilters}>
            Clear filters
          </button>
        </div>

        {loading ? (
          <p>Loading...</p>
        ) : products.length === 0 ? (
          <p>No products match your search/filters.</p>
        ) : (
          <div className="product-grid">
            {products.map((p) => (
              <div className="product-card" key={p.id}>
                <div className="product-image-wrap">
                  {p.imageUrl ? (
                    <img src={p.imageUrl} alt={p.name} className="product-image" />
                  ) : (
                    <div className="product-image-placeholder">No image</div>
                  )}
                </div>

                <h3>{p.name}</h3>
                {p.category && <p className="product-category">{p.category}</p>}
                <p className="product-desc">{p.description}</p>
                <p className="product-price">₹{p.price.toLocaleString()}</p>
                <p className="product-stock">Stock: {p.quantity}</p>

                <div className="qty-stepper">
                  <button onClick={() => changeQuantity(p.id, -1, p.quantity)}>-</button>
                  <span>{quantities[p.id] || 1}</span>
                  <button
                    onClick={() => changeQuantity(p.id, 1, p.quantity)}
                    disabled={(quantities[p.id] || 1) >= p.quantity}
                  >
                    +
                  </button>
                </div>

                <button
                  className="add-to-cart-btn"
                  onClick={() => handleAddToCart(p.id, p.quantity)}
                  disabled={p.quantity <= 0}
                >
                  {p.quantity <= 0 ? 'Out of Stock' : 'Add to Cart'}
                </button>

                {statusByProduct[p.id] && (
                  <p className="cart-status">{statusByProduct[p.id]}</p>
                )}
              </div>
            ))}
          </div>
        )}
        {totalPages > 1 && (
          <div className="pagination-bar">
            <button
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              disabled={pageNumber <= 1}
            >
              «
            </button>

            {getPageNumbers(pageNumber, totalPages).map((page, index) =>
              page === '...' ? (
                <span key={`ellipsis-${index}`} className="pagination-ellipsis">...</span>
              ) : (
                <button
                  key={page}
                  className={page === pageNumber ? 'pagination-active' : ''}
                  onClick={() => setPageNumber(page)}
                >
                  {page}
                </button>
              )
            )}

            <button
              onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
              disabled={pageNumber >= totalPages}
            >
              »
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
