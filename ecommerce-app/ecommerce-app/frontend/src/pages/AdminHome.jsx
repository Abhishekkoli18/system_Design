import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import axiosInstance from '../api/axiosInstance'
import { useAuth } from '../context/AuthContext'
import Navbar from '../components/Navbar'
import { getPageNumbers } from '../utils/pagination'

export default function AdminHome() {
  const { user } = useAuth()

  const [products, setProducts] = useState([])
  const [loadingProducts, setLoadingProducts] = useState(true)

  // Manual "add one product" form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [category, setCategory] = useState('')
  const [price, setPrice] = useState('')
  const [quantity, setQuantity] = useState('')
  const [addError, setAddError] = useState('')
  const [addSuccess, setAddSuccess] = useState('')
  const [adding, setAdding] = useState(false)

  // CSV upload state
  const [csvFile, setCsvFile] = useState(null)
  const [csvResult, setCsvResult] = useState(null)
  const [uploading, setUploading] = useState(false)

  // Editing state - which product is currently being edited, and its form values
  const [editingId, setEditingId] = useState(null)
  const [editForm, setEditForm] = useState({ name: '', description: '', imageUrl: '', category: '', price: '', quantity: '' })
  const [editError, setEditError] = useState('')
  const [savingEdit, setSavingEdit] = useState(false)


  const [categories, setCategories] = useState([])
  const [searchTerm, setSearchTerm] = useState('')
  const [minPrice, setMinPrice] = useState('')
  const [maxPrice, setMaxPrice] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [inStockOnly, setInStockOnly] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const pageSize = 12

  useEffect(() => {
    axiosInstance
      .get('/product/categories')
      .then((res) => setCategories(res.data))
      .catch(() => setCategories([]))
  }, [])

  // This single effect handles both the initial load AND every
  // search/filter/page change - no separate "on mount" effect needed.
  useEffect(() => {
    loadProducts()
  }, [searchTerm, minPrice, maxPrice, categoryFilter, inStockOnly, pageNumber])

  async function loadProducts() {
    setLoadingProducts(true)
    try {
      const res = await axiosInstance.get('/product', {
        params: {
          search: searchTerm || undefined,
          minPrice: minPrice || undefined,
          maxPrice: maxPrice || undefined,
          category: categoryFilter || undefined,
          inStockOnly: inStockOnly || undefined,
          pageNumber,
          pageSize,
        },
      })
      setProducts(res.data.items)
      setTotalPages(res.data.totalPages)
      setTotalCount(res.data.totalCount)
    } catch (err) {
      console.error('Failed to load products', err)
    } finally {
      setLoadingProducts(false)
    }
  }

  // --- Manual single product add ---
  async function handleAddProduct(e) {
    e.preventDefault()
    setAddError('')
    setAddSuccess('')
    setAdding(true)

    try {
      await axiosInstance.post('/product/add', {
        name,
        description,
        imageUrl,
        category,
        price: parseFloat(price),
        quantity: parseInt(quantity, 10),
      })
      setAddSuccess(`"${name}" added successfully.`)
      toast.success(`"${name}" added successfully.`)
      setName('')
      setDescription('')
      setImageUrl('')
      setCategory('')
      setPrice('')
      setQuantity('')
      loadProducts() // refresh the list to show the new product
    } catch (err) {
      setAddError(err.response?.data?.message || 'Failed to add product.')
      toast.error(err.response?.data?.message || 'Failed to add product.')
    } finally {
      setAdding(false)
    }
  }

  // --- CSV bulk upload ---
  async function handleCsvUpload(e) {
    e.preventDefault()
    setCsvResult(null)

    if (!csvFile) {
      setCsvResult({ error: 'Please choose a .csv file first.' })
      return
    }

    setUploading(true)

    // File uploads use FormData instead of a plain JSON body.
    const formData = new FormData()
    formData.append('file', csvFile)

    try {
      const res = await axiosInstance.post('/product/upload-csv', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      setCsvResult(res.data)
      loadProducts() // refresh the list to show newly imported products
      toast.success(res.data.message)
    } catch (err) {
      setCsvResult({ error: err.response?.data?.message || 'Upload failed.' })
    } finally {
      setUploading(false)
    }
  }

  // --- Editing an existing product ---
  function startEdit(product) {
    setEditingId(product.id)
    setEditError('')
    setEditForm({
      name: product.name,
      description: product.description || '',
      imageUrl: product.imageUrl || '',
      category: product.category || '',
      price: product.price,
      quantity: product.quantity,
    })
  }

  function cancelEdit() {
    setEditingId(null)
    setEditError('')
  }

  async function handleSaveEdit(productId) {
    setEditError('')
    setSavingEdit(true)

    try {
      await axiosInstance.put(`/product/${productId}`, {
        name: editForm.name,
        description: editForm.description,
        imageUrl: editForm.imageUrl,
        category: editForm.category,
        price: parseFloat(editForm.price),
        quantity: parseInt(editForm.quantity, 10),
      })
      toast.success('Product updated successfully.')
      setEditingId(null)
      loadProducts() // refresh to show the saved changes
    } catch (err) {
      setEditError(err.response?.data?.message || 'Failed to save changes.')
      toast.error(err.response?.data?.message || 'Failed to save changes.')
    } finally {
      setSavingEdit(false)
    }
  }

  // --- Delete a single product ---
  async function handleDelete(productId, productName) {
    const confirmed = window.confirm(`Delete "${productName}"? This cannot be undone.`)
    if (!confirmed) return

    try {
      await axiosInstance.delete(`/product/${productId}`)
      toast.success(`"${productName}" deleted.`)
      loadProducts()
    } catch (err) {
      console.error('Failed to delete product', err)
    }
  }

  // --- Clear every product ---
  async function handleClearAll() {
    const confirmed = window.confirm('Delete ALL products? This cannot be undone.')
    if (!confirmed) return

    try {
      const res = await axiosInstance.delete('/product/delete-all')
      toast.success(res.data.message)
      loadProducts()
    } catch (err) {
      console.error('Failed to clear products', err)
    }
  }
  function clearFilters() {
    setSearchTerm('')
    setMinPrice('')
    setMaxPrice('')
    setCategoryFilter('')
    setInStockOnly(false)
    setPageNumber(1)
  }


  return (
    <div>
      <Navbar />
      <div className="home-page admin-theme">
        <h1>Admin Dashboard</h1>
        <p>Logged in as Admin: {user?.fullName}</p>

        <div className="admin-grid">
          {/* ---- Manual add product ---- */}
          <div className="panel">
            <h2>Add product manually</h2>
            {addError && <p className="error-text">{addError}</p>}
            {addSuccess && <p className="success-text">{addSuccess}</p>}

            <form onSubmit={handleAddProduct} className="inline-form">
              <label>Product name</label>
              <input value={name} onChange={(e) => setName(e.target.value)} required />

              <label>Description</label>
              <input value={description} onChange={(e) => setDescription(e.target.value)} />

              <label>Image URL</label>
              <input
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                placeholder="https://..."
              />

              <label>Category</label>
              <input
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                placeholder="e.g. Electronics"
              />

              <label>Price</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                required
              />

              <label>Quantity</label>
              <input
                type="number"
                min="0"
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                required
              />

              <button type="submit" disabled={adding}>
                {adding ? 'Adding...' : 'Add product'}
              </button>
            </form>
          </div>

          {/* ---- CSV bulk upload ---- */}
          <div className="panel">
            <h2>Bulk upload via CSV</h2>
            <p className="hint-text">
              CSV format, with a header row: <code>Name,Price,Quantity,Description,ImageUrl,Category</code>
              {' '}(Description, ImageUrl, and Category are optional)
            </p>

            <form onSubmit={handleCsvUpload} className="inline-form">
              <input
                type="file"
                accept=".csv"
                onChange={(e) => setCsvFile(e.target.files[0])}
              />
              <button type="submit" disabled={uploading}>
                {uploading ? 'Uploading...' : 'Upload CSV'}
              </button>
            </form>

            {csvResult && (
              <div className="csv-result">
                {csvResult.error ? (
                  <p className="error-text">{csvResult.error}</p>
                ) : (
                  <>
                    <p className="success-text">{csvResult.message}</p>
                    {csvResult.errorCount > 0 && (
                      <ul className="csv-errors">
                        {csvResult.errors.map((e, i) => (
                          <li key={i}>{e}</li>
                        ))}
                      </ul>
                    )}
                  </>
                )}
              </div>
            )}
          </div>
        </div>

        {/* ---- Product list (card view, editable) ---- */}
        <div className="panel" style={{ marginTop: 24 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h2>All products ({totalCount})</h2>
            {totalCount > 0 && (
              <button className="cancel-btn" onClick={handleClearAll}>
                Clear all products
              </button>
            )}
          </div>

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
            <select value={categoryFilter} onChange={(e) => { setCategoryFilter(e.target.value); setPageNumber(1) }}>
              <option value="">All categories</option>
              {categories.map((c) => (
                <option key={c} value={c}>{c}</option>
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
          {loadingProducts ? (
            <p>Loading...</p>
          ) : products.length === 0 ? (
            <p>No products yet — add one above.</p>
          ) : (
            <div className="product-grid">
              {products.map((p) =>
                editingId === p.id ? (
                  // ---- Edit mode for this one card ----
                  <div className="product-card" key={p.id}>
                    {editError && <p className="error-text">{editError}</p>}

                    <label>Name</label>
                    <input
                      value={editForm.name}
                      onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                    />

                    <label>Description</label>
                    <input
                      value={editForm.description}
                      onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                    />

                    <label>Image URL</label>
                    <input
                      value={editForm.imageUrl}
                      onChange={(e) => setEditForm({ ...editForm, imageUrl: e.target.value })}
                      placeholder="https://..."
                    />

                    <label>Category</label>
                    <input
                      value={editForm.category}
                      onChange={(e) => setEditForm({ ...editForm, category: e.target.value })}
                    />

                    <label>Price</label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={editForm.price}
                      onChange={(e) => setEditForm({ ...editForm, price: e.target.value })}
                    />

                    <label>Quantity</label>
                    <input
                      type="number"
                      min="0"
                      value={editForm.quantity}
                      onChange={(e) => setEditForm({ ...editForm, quantity: e.target.value })}
                    />

                    <div className="edit-actions">
                      <button
                        className="add-to-cart-btn"
                        onClick={() => handleSaveEdit(p.id)}
                        disabled={savingEdit}
                      >
                        {savingEdit ? 'Saving...' : 'Save'}
                      </button>
                      <button className="cancel-btn" onClick={cancelEdit}>
                        Cancel
                      </button>
                    </div>
                  </div>
                ) : (
                  // ---- Normal display mode for this card ----
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

                    <div className="edit-actions">
                      <button className="edit-btn" onClick={() => startEdit(p)}>
                        Edit
                      </button>
                      <button className="cancel-btn" onClick={() => handleDelete(p.id, p.name)}>
                        Delete
                      </button>
                    </div>
                  </div>
                )
              )}
            </div>
          )}
        </div>
        {totalPages > 1 && (
          <div className="pagination-bar">
            <button onClick={() => setPageNumber((p) => Math.max(1, p - 1))} disabled={pageNumber <= 1}>
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

            <button onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))} disabled={pageNumber >= totalPages}>
              »
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
