import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";

const Product = () => {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    price: "",
    stockQuantity: "",
    category: "",
    imageUrl: "",
  });

  const navigate = useNavigate();
  const API_BASE_URL = "https://localhost:7062/api";

  // Kategoriler
  const categories = [
    "Bilgisayar",
    "Telefon",
    "Ses",
    "Tablet",
    "Giyilebilir",
    "Depolama",
  ];

  // Sayfa yüklendiğinde ürünleri getir
  useEffect(() => {
    fetchProducts();
  }, []);

  // Ürünleri API'den getir
  const fetchProducts = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem("authToken");
      const response = await fetch(`${API_BASE_URL}/Products/All2`, {
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: "application/json",
        },
      });

      if (response.ok) {
        const contentType = response.headers.get("content-type");
        if (contentType && contentType.includes("application/json")) {
          const data = await response.json();
          setProducts(data);
        } else {
          console.error("Response is not JSON");
          setProducts([]);
        }
      } else {
        console.error(
          "Failed to fetch products:",
          response.status,
          response.statusText
        );
        setProducts([]);
      }
    } catch (error) {
      console.error("API Hatası:", error);
      setProducts([]);
    } finally {
      setLoading(false);
    }
  };

  // Form verilerini güncelle
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  // Yeni ürün ekle
  const handleAddProduct = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      const token = localStorage.getItem("authToken");

      const productData = {
        name: formData.name,
        description: formData.description,
        price: parseFloat(formData.price),
        stockQuantity: parseInt(formData.stockQuantity),
        category: formData.category,
        imageUrl: formData.imageUrl,
      };

      const response = await fetch(`${API_BASE_URL}/Products`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify(productData),
      });

      if (response.ok) {
        alert("Ürün başarıyla eklendi!");
        setFormData({
          name: "",
          description: "",
          price: "",
          stockQuantity: "",
          category: "",
          imageUrl: "",
        });
        setShowAddForm(false);
        fetchProducts(); // Ürün listesini yenile
      } else {
        // Response'un JSON olup olmadığını kontrol et
        const contentType = response.headers.get("content-type");
        let errorMessage = "Bilinmeyen hata";

        if (contentType && contentType.includes("application/json")) {
          try {
            const errorData = await response.json();
            errorMessage =
              errorData.message ||
              errorData.title ||
              "Ürün eklenirken hata oluştu";
          } catch (jsonError) {
            errorMessage = `HTTP ${response.status}: ${response.statusText}`;
          }
        } else {
          const errorText = await response.text();
          errorMessage =
            errorText || `HTTP ${response.status}: ${response.statusText}`;
        }

        console.error("Backend Error:", errorMessage);
        alert("Ürün eklenirken hata oluştu: " + errorMessage);
      }
    } catch (error) {
      console.error("Ürün ekleme hatası:", error);
      alert("Ürün eklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  // Ürünleri filtrele
  const filteredProducts = products.filter((product) => {
    const matchesSearch =
      product.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      product.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory =
      categoryFilter === "" || product.category === categoryFilter;

    return matchesSearch && matchesCategory;
  });

  // Para formatı
  const formatPrice = (price) => {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
    }).format(price);
  };

  // Tarih formatı
  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString("tr-TR");
  };

  return (
    <div className="container-fluid py-4">
      {/* Header */}
      <div className="row mb-4">
        <div className="col-12">
          <div className="d-flex justify-content-between align-items-center">
            <h1 className="h2 text-primary fw-bold">
              <i className="bi bi-box-seam me-2"></i>
              Ürün Yönetimi
            </h1>
            <button
              className="btn btn-success btn-lg shadow"
              onClick={() => setShowAddForm(!showAddForm)}
            >
              <i className="bi bi-plus-circle me-2"></i>
              {showAddForm ? "Formu Kapat" : "Yeni Ürün Ekle"}
            </button>
          </div>
        </div>
      </div>

      {/* Ürün Ekleme Formu */}
      {showAddForm && (
        <div className="row mb-5">
          <div className="col-12">
            <div className="card shadow-lg border-0">
              <div className="card-header bg-gradient bg-primary text-white">
                <h4 className="mb-0">
                  <i className="bi bi-plus-square me-2"></i>
                  Yeni Ürün Ekle
                </h4>
              </div>
              <div className="card-body p-4">
                <form onSubmit={handleAddProduct}>
                  <div className="row g-3">
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Ürün Adı *
                      </label>
                      <input
                        type="text"
                        className="form-control form-control-lg"
                        name="name"
                        value={formData.name}
                        onChange={handleInputChange}
                        required
                        placeholder="Ürün adını girin"
                      />
                    </div>

                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Kategori *
                      </label>
                      <select
                        className="form-select form-select-lg"
                        name="category"
                        value={formData.category}
                        onChange={handleInputChange}
                        required
                      >
                        <option value="">Kategori seçin</option>
                        {categories.map((cat) => (
                          <option key={cat} value={cat}>
                            {cat}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="col-12">
                      <label className="form-label fw-semibold">Açıklama</label>
                      <textarea
                        className="form-control"
                        name="description"
                        value={formData.description}
                        onChange={handleInputChange}
                        rows="3"
                        placeholder="Ürün açıklaması"
                      />
                    </div>

                    <div className="col-md-4">
                      <label className="form-label fw-semibold">
                        Fiyat (₺) *
                      </label>
                      <input
                        type="number"
                        className="form-control form-control-lg"
                        name="price"
                        value={formData.price}
                        onChange={handleInputChange}
                        min="0"
                        step="0.01"
                        required
                        placeholder="0.00"
                      />
                    </div>

                    <div className="col-md-4">
                      <label className="form-label fw-semibold">
                        Stok Miktarı *
                      </label>
                      <input
                        type="number"
                        className="form-control form-control-lg"
                        name="stockQuantity"
                        value={formData.stockQuantity}
                        onChange={handleInputChange}
                        min="0"
                        required
                        placeholder="0"
                      />
                    </div>

                    <div className="col-md-4">
                      <label className="form-label fw-semibold">
                        Ürün Resmi URL
                      </label>
                      <input
                        type="text"
                        className="form-control form-control-lg"
                        name="imageUrl"
                        value={formData.imageUrl}
                        onChange={handleInputChange}
                        placeholder="Resim URL'sini girin"
                      />
                      <div className="form-text">
                        <i className="bi bi-info-circle me-1"></i>
                        Ürün resmi için URL bağlantısı girin
                      </div>
                    </div>

                    <div className="col-12 text-end mt-4">
                      <button
                        type="button"
                        className="btn btn-outline-secondary btn-lg me-3"
                        onClick={() => setShowAddForm(false)}
                      >
                        <i className="bi bi-x-circle me-2"></i>
                        İptal
                      </button>
                      <button
                        type="submit"
                        className="btn btn-success btn-lg px-5"
                        disabled={loading}
                      >
                        <i className="bi bi-check-circle me-2"></i>
                        {loading ? "Ekleniyor..." : "Ürün Ekle"}
                      </button>
                    </div>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Arama ve Filtreleme */}
      <div className="row mb-4">
        <div className="col-md-8">
          <div className="input-group input-group-lg">
            <span className="input-group-text bg-primary text-white">
              <i className="bi bi-search"></i>
            </span>
            <input
              type="text"
              className="form-control"
              placeholder="Ürün adı veya açıklamasında ara..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
        </div>
        <div className="col-md-4">
          <select
            className="form-select form-select-lg"
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
          >
            <option value="">Tüm Kategoriler</option>
            {categories.map((cat) => (
              <option key={cat} value={cat}>
                {cat}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Ürün Listesi */}
      <div className="row">
        <div className="col-12">
          <div className="card shadow-lg border-0">
            <div className="card-header bg-gradient bg-info text-white d-flex justify-content-between align-items-center">
              <h4 className="mb-0">
                <i className="bi bi-grid me-2"></i>
                Ürün Listesi ({filteredProducts.length} ürün)
              </h4>
              <button
                className="btn btn-light btn-sm"
                onClick={fetchProducts}
                disabled={loading}
              >
                <i className="bi bi-arrow-clockwise me-1"></i>
                Yenile
              </button>
            </div>
            <div className="card-body p-0">
              {loading ? (
                <div className="text-center py-5">
                  <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Yükleniyor...</span>
                  </div>
                  <p className="mt-3 text-muted">Ürünler yükleniyor...</p>
                </div>
              ) : filteredProducts.length === 0 ? (
                <div className="text-center py-5">
                  <i className="bi bi-inbox display-1 text-muted"></i>
                  <h4 className="text-muted mt-3">Ürün bulunamadı</h4>
                  <p className="text-muted">
                    Henüz hiç ürün eklenmemiş veya arama kriterlerinize uygun
                    ürün yok.
                  </p>
                </div>
              ) : (
                <div className="table-responsive">
                  <table className="table table-hover table-striped mb-0">
                    <thead className="table-dark">
                      <tr>
                        <th scope="col">#</th>
                        <th scope="col">Ürün Adı</th>
                        <th scope="col">Kategori</th>
                        <th scope="col">Fiyat</th>
                        <th scope="col">Stok</th>
                        <th scope="col">Durum</th>
                        <th scope="col">Eklenme Tarihi</th>
                        <th scope="col">Açıklama</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredProducts.map((product, index) => (
                        <tr key={product.id}>
                          <td className="fw-bold">{index + 1}</td>
                          <td>
                            <div className="d-flex align-items-center">
                              {product.imageUrl ? (
                                <img
                                  src={product.imageUrl}
                                  alt={product.name}
                                  className="rounded me-3"
                                  style={{
                                    width: "50px",
                                    height: "50px",
                                    objectFit: "cover",
                                  }}
                                  onError={(e) => {
                                    e.target.style.display = "none";
                                    e.target.nextSibling.style.display = "flex";
                                  }}
                                />
                              ) : null}
                              <div
                                className="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-3"
                                style={{
                                  width: "50px",
                                  height: "50px",
                                  fontSize: "18px",
                                  display: product.imageUrl ? "none" : "flex",
                                }}
                              >
                                {product.name.charAt(0).toUpperCase()}
                              </div>
                              <div>
                                <div className="fw-semibold">
                                  {product.name}
                                </div>
                                <small className="text-muted">
                                  ID: {product.id}
                                </small>
                              </div>
                            </div>
                          </td>
                          <td>
                            <span className="badge bg-secondary rounded-pill">
                              {product.category}
                            </span>
                          </td>
                          <td className="fw-bold text-success">
                            {formatPrice(product.price)}
                          </td>
                          <td>
                            <span
                              className={`badge rounded-pill ${
                                product.stockQuantity > 10
                                  ? "bg-success"
                                  : product.stockQuantity > 0
                                  ? "bg-warning"
                                  : "bg-danger"
                              }`}
                            >
                              {product.stockQuantity} adet
                            </span>
                          </td>
                          <td>
                            {product.stockQuantity > 0 ? (
                              <span className="badge bg-success">
                                <i className="bi bi-check-circle me-1"></i>
                                Stokta
                              </span>
                            ) : (
                              <span className="badge bg-danger">
                                <i className="bi bi-x-circle me-1"></i>
                                Tükendi
                              </span>
                            )}
                          </td>
                          <td className="text-muted">
                            {formatDate(product.createdAt)}
                          </td>
                          <td>
                            <small className="text-muted">
                              {product.description
                                ? product.description.length > 50
                                  ? product.description.substring(0, 50) + "..."
                                  : product.description
                                : "Açıklama yok"}
                            </small>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Özet İstatistikler */}
      {!loading && products.length > 0 && (
        <div className="row mt-4">
          <div className="col-md-3">
            <div className="card bg-primary text-white">
              <div className="card-body text-center">
                <i className="bi bi-box display-4 mb-2"></i>
                <h4>{products.length}</h4>
                <small>Toplam Ürün</small>
              </div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="card bg-success text-white">
              <div className="card-body text-center">
                <i className="bi bi-check-circle display-4 mb-2"></i>
                <h4>{products.filter((p) => p.stockQuantity > 0).length}</h4>
                <small>Stokta Olan</small>
              </div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="card bg-warning text-white">
              <div className="card-body text-center">
                <i className="bi bi-exclamation-triangle display-4 mb-2"></i>
                <h4>
                  {
                    products.filter(
                      (p) => p.stockQuantity <= 10 && p.stockQuantity > 0
                    ).length
                  }
                </h4>
                <small>Düşük Stok</small>
              </div>
            </div>
          </div>
          <div className="col-md-3">
            <div className="card bg-danger text-white">
              <div className="card-body text-center">
                <i className="bi bi-x-circle display-4 mb-2"></i>
                <h4>{products.filter((p) => p.stockQuantity === 0).length}</h4>
                <small>Tükenen</small>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Product;
