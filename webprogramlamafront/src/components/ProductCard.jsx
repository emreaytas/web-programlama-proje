// src/components/ProductCard.jsx - Düzeltilmiş sepet işlemi
import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { isAuthenticated } from "../utils/tokenUtils";
import { addToCart, getCart } from "../utils/cartUtils";

const ProductCard = ({ product, showAddToCart = false }) => {
  const navigate = useNavigate();
  const [imageError, setImageError] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isAddingToCart, setIsAddingToCart] = useState(false);
  const [isInCart, setIsInCart] = useState(false);

  // Ürünün sepette olup olmadığını kontrol et
  useEffect(() => {
    const checkIfInCart = () => {
      const localCart = getCart();
      const isProductInCart = localCart.some((item) => item.id === product.id);
      setIsInCart(isProductInCart);
    };

    checkIfInCart();

    // localStorage değişikliklerini dinle
    const handleStorageChange = (e) => {
      if (e.key === "shopping_cart") {
        checkIfInCart();
      }
    };

    window.addEventListener("storage", handleStorageChange);

    return () => {
      window.removeEventListener("storage", handleStorageChange);
    };
  }, [product.id]);

  // Sepete ekleme/çıkarma işlemi
  const handleAddToCart = async () => {
    if (isAddingToCart) return;

    setIsAddingToCart(true);

    try {
      if (isInCart) {
        // Sepetten çıkar
        await removeFromCart();
      } else {
        // Sepete ekle
        await addToCartFunction();
      }
    } catch (error) {
      console.error("Sepet işlemi başarısız:", error);
      alert("Sepet işlemi sırasında bir hata oluştu");
    } finally {
      setIsAddingToCart(false);
    }
  };

  // Sepete ekleme fonksiyonu
  const addToCartFunction = async () => {
    // 1. Önce localStorage'a ekle (her durumda çalışsın)
    addToCart(product);
    setIsInCart(true);

    // 2. Kullanıcı giriş yapmışsa backend'e de ekle
    if (isAuthenticated()) {
      try {
        const token = localStorage.getItem("authToken");
        const response = await fetch("https://localhost:7062/api/Urun", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
            Accept: "application/json",
          },
          body: JSON.stringify({ urunId: product.id }),
        });

        if (response.ok) {
          const data = await response.json();
          console.log("✅ Backend sepetine eklendi:", data.message);

          // Başarı mesajı göster
          showSuccessMessage(data.message || `${product.name} sepete eklendi!`);
        } else {
          const errorData = await response.json().catch(() => ({}));

          // Eğer ürün zaten sepette ise, sadece uyarı ver
          if (
            errorData.error &&
            errorData.error.includes("zaten sepetinizde")
          ) {
            console.warn("⚠️ Ürün zaten backend sepetinde mevcut");
            showWarningMessage("Ürün zaten sepetinizde mevcut");
          } else {
            console.warn("⚠️ Backend sepetine eklenemedi:", errorData.error);
            showSuccessMessage(`${product.name} sepete eklendi (yerel)!`);
          }
        }
      } catch (backendError) {
        console.warn("⚠️ Backend bağlantısı başarısız:", backendError);
        showSuccessMessage(`${product.name} sepete eklendi (yerel)!`);
      }
    } else {
      // Giriş yapmamış kullanıcı için mesaj
      showSuccessMessage(
        `${product.name} sepete eklendi! Hesap oluşturarak sepetinizi güvende tutun.`
      );
    }
  };

  // Sepetten çıkarma fonksiyonu
  const removeFromCart = async () => {
    // 1. localStorage'dan çıkar
    const currentCart = getCart();
    const updatedCart = currentCart.filter((item) => item.id !== product.id);
    localStorage.setItem("shopping_cart", JSON.stringify(updatedCart));
    setIsInCart(false);

    // 2. Backend'den de çıkar (kullanıcı giriş yapmışsa)
    if (isAuthenticated()) {
      try {
        const token = localStorage.getItem("authToken");
        const response = await fetch(
          `https://localhost:7062/api/Urun/product/${product.id}`,
          {
            method: "DELETE",
            headers: {
              Authorization: `Bearer ${token}`,
              Accept: "application/json",
            },
          }
        );

        if (response.ok) {
          const data = await response.json();
          console.log("✅ Backend sepetinden çıkarıldı:", data.message);
          showSuccessMessage(
            data.message || `${product.name} sepetten çıkarıldı!`
          );
        } else {
          console.warn("⚠️ Backend sepetinden çıkarılamadı");
          showSuccessMessage(`${product.name} sepetten çıkarıldı (yerel)!`);
        }
      } catch (backendError) {
        console.warn("⚠️ Backend bağlantısı başarısız:", backendError);
        showSuccessMessage(`${product.name} sepetten çıkarıldı (yerel)!`);
      }
    } else {
      showSuccessMessage(`${product.name} sepetten çıkarıldı!`);
    }
  };

  // Başarı mesajı göster
  const showSuccessMessage = (message) => {
    // Basit alert yerine daha güzel bir bildirim sistemi kullanılabilir
    const notification = document.createElement("div");
    notification.className = "alert alert-success position-fixed";
    notification.style.cssText =
      "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
    notification.innerHTML = `
      <i class="bi bi-check-circle me-2"></i>
      ${message}
      <button type="button" class="btn-close ms-auto" onclick="this.parentElement.remove()"></button>
    `;
    document.body.appendChild(notification);

    // 3 saniye sonra otomatik kaldır
    setTimeout(() => {
      if (notification.parentElement) {
        notification.remove();
      }
    }, 3000);
  };

  // Uyarı mesajı göster
  const showWarningMessage = (message) => {
    const notification = document.createElement("div");
    notification.className = "alert alert-warning position-fixed";
    notification.style.cssText =
      "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
    notification.innerHTML = `
      <i class="bi bi-exclamation-triangle me-2"></i>
      ${message}
      <button type="button" class="btn-close ms-auto" onclick="this.parentElement.remove()"></button>
    `;
    document.body.appendChild(notification);

    setTimeout(() => {
      if (notification.parentElement) {
        notification.remove();
      }
    }, 3000);
  };

  // Fiyat formatı
  const formatPrice = (price) => {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
    }).format(price);
  };

  // Placeholder resim URL'i oluştur
  const getPlaceholderImage = (productName) => {
    const encodedName = encodeURIComponent(productName);
    return `https://via.placeholder.com/300x200/6c757d/ffffff?text=${encodedName}`;
  };

  // Resim yükleme hatası
  const handleImageError = () => {
    setImageError(true);
    setIsLoading(false);
  };

  // Resim başarıyla yüklendi
  const handleImageLoad = () => {
    setIsLoading(false);
  };

  // Stok durumu badge'i
  const getStockBadge = () => {
    const stockQuantity = product.sold || product.stockQuantity || 0;

    if (stockQuantity === 0) {
      return (
        <span className="badge bg-danger position-absolute top-0 end-0 m-2">
          Tükendi
        </span>
      );
    } else if (stockQuantity <= 10) {
      return (
        <span className="badge bg-warning text-dark position-absolute top-0 end-0 m-2">
          Son {stockQuantity} adet!
        </span>
      );
    } else if (stockQuantity >= 50) {
      return (
        <span className="badge bg-success position-absolute top-0 end-0 m-2">
          Bol Stok
        </span>
      );
    }
    return null;
  };

  const stockQuantity = product.sold || product.stockQuantity || 0;
  const isOutOfStock = stockQuantity === 0;

  return (
    <div className="col-md-4 mb-4">
      <div className="card h-100 shadow-sm border-0 product-card">
        {/* Resim Container */}
        <div
          className="position-relative overflow-hidden"
          style={{ height: "200px" }}
        >
          {/* Loading Spinner */}
          {isLoading && (
            <div className="position-absolute top-50 start-50 translate-middle">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Yükleniyor...</span>
              </div>
            </div>
          )}

          {/* Ürün Resmi */}
          <img
            src={imageError ? getPlaceholderImage(product.name) : product.image}
            alt={product.name}
            className={`card-img-top ${isLoading ? "d-none" : ""}`}
            style={{
              height: "200px",
              objectFit: "cover",
              transition: "transform 0.3s ease",
            }}
            onError={handleImageError}
            onLoad={handleImageLoad}
          />

          {/* Kategori Badge */}
          <span className="badge bg-primary position-absolute top-0 start-0 m-2">
            {product.category}
          </span>

          {/* Stok Badge */}
          {getStockBadge()}

          {/* Sepette olma durumu badge'i */}
          {isInCart && (
            <span className="badge bg-info position-absolute top-0 start-50 translate-middle-x m-2">
              <i className="bi bi-cart-check me-1"></i>
              Sepette
            </span>
          )}

          {/* Hover Effect Overlay */}
          <div className="product-overlay position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center">
            <button
              className="btn btn-light btn-sm shadow-sm me-2"
              onClick={() => navigate(`/product/${product.id}`)}
            >
              <i className="bi bi-eye me-1"></i>
              Detay
            </button>
          </div>
        </div>

        <div className="card-body d-flex flex-column">
          {/* Ürün Adı */}
          <h5 className="card-title fw-bold text-truncate" title={product.name}>
            {product.name}
          </h5>

          {/* Ürün Açıklaması */}
          <p className="card-text flex-grow-1 text-muted small">
            {product.description && product.description.length > 100
              ? product.description.substring(0, 100) + "..."
              : product.description || "Açıklama mevcut değil"}
          </p>

          {/* Stok Bilgisi */}
          <div className="mb-2">
            <small className="text-muted">
              <i className="bi bi-box-seam me-1"></i>
              Stok: {stockQuantity} adet
            </small>
          </div>

          {/* Fiyat ve Buton */}
          <div className="d-flex justify-content-between align-items-center mt-auto">
            <div>
              <span className="fw-bold fs-5 text-success">
                {formatPrice(product.price)}
              </span>
              {stockQuantity <= 10 && stockQuantity > 0 && (
                <div>
                  <small className="text-warning">
                    <i className="bi bi-exclamation-triangle me-1"></i>
                    Az kaldı!
                  </small>
                </div>
              )}
            </div>

            <button
              className={`btn btn-sm ${
                isOutOfStock
                  ? "btn-outline-secondary"
                  : isInCart
                  ? "btn-outline-danger"
                  : "btn-success"
              }`}
              onClick={handleAddToCart}
              disabled={isOutOfStock || isAddingToCart}
            >
              {isAddingToCart ? (
                <>
                  <div
                    className="spinner-border spinner-border-sm me-1"
                    role="status"
                  >
                    <span className="visually-hidden">Yükleniyor...</span>
                  </div>
                  İşleniyor...
                </>
              ) : isOutOfStock ? (
                <>
                  <i className="bi bi-x-circle me-1"></i>
                  Tükendi
                </>
              ) : isInCart ? (
                <>
                  <i className="bi bi-cart-dash me-1"></i>
                  Sepetten Çıkar
                </>
              ) : (
                <>
                  <i className="bi bi-cart-plus me-1"></i>
                  Sepete Ekle
                </>
              )}
            </button>
          </div>

          {/* Sepete ekleme durumu göstergesi */}
          {isInCart && (
            <div className="mt-2">
              <small className="text-info">
                <i className="bi bi-check-circle me-1"></i>
                Bu ürün sepetinizde mevcut
              </small>
            </div>
          )}

          {/* Giriş önerisi */}
          {!isAuthenticated() && !isOutOfStock && (
            <div className="mt-2">
              <small className="text-muted">
                <i className="bi bi-info-circle me-1"></i>
                <span
                  className="text-primary"
                  style={{ cursor: "pointer" }}
                  onClick={() => navigate("/login")}
                >
                  Hesap oluşturun
                </span>{" "}
                ve sepetinizi güvende tutun
              </small>
            </div>
          )}
        </div>

        {/* CSS Styles */}
        <style jsx>{`
          .product-card {
            transition: all 0.3s ease;
            cursor: pointer;
          }

          .product-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15) !important;
          }

          .product-card:hover .card-img-top {
            transform: scale(1.05);
          }

          .product-overlay {
            background: rgba(0, 0, 0, 0.7);
            opacity: 0;
            transition: opacity 0.3s ease;
          }

          .product-card:hover .product-overlay {
            opacity: 1;
          }

          .alert {
            animation: slideInRight 0.3s ease-out;
          }

          @keyframes slideInRight {
            from {
              transform: translateX(100%);
              opacity: 0;
            }
            to {
              transform: translateX(0);
              opacity: 1;
            }
          }
        `}</style>
      </div>
    </div>
  );
};

export default ProductCard;
