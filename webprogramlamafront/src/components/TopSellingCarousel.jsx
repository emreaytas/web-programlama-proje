// src/components/TopSellingCarousel.jsx - Güncellenmiş sepet işlemi
import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { addToCart, isInCart, onCartChange } from "../utils/cartUtils";
import { isAuthenticated } from "../utils/tokenUtils";

const TopSellingCarousel = ({ products }) => {
  const navigate = useNavigate();
  const [cartItems, setCartItems] = useState(new Set());
  const [loadingProducts, setLoadingProducts] = useState(new Set());

  // Sepet durumunu takip et
  useEffect(() => {
    const updateCartItems = () => {
      const cartSet = new Set();
      products?.forEach((product) => {
        if (isInCart(product.id)) {
          cartSet.add(product.id);
        }
      });
      setCartItems(cartSet);
    };

    updateCartItems();

    // Sepet değişikliklerini dinle
    const unsubscribe = onCartChange(() => {
      updateCartItems();
    });

    return unsubscribe;
  }, [products]);

  const handleAddToCart = async (product) => {
    if (loadingProducts.has(product.id)) return;

    setLoadingProducts((prev) => new Set([...prev, product.id]));

    try {
      if (cartItems.has(product.id)) {
        // Sepetten çıkar ve sepete yönlendir
        navigate("/cart");
        return;
      }

      // 1. Önce localStorage'a ekle (her durumda çalışsın)
      const added = addToCart(product);

      if (added) {
        setCartItems((prev) => new Set([...prev, product.id]));

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
              console.log(
                "✅ Carousel - Backend sepetine eklendi:",
                data.message
              );
              showSuccessMessage(
                data.message || `${product.name} sepete eklendi!`
              );
            } else {
              const errorData = await response.json().catch(() => ({}));

              // Eğer ürün zaten sepette ise, sadece uyarı ver
              if (
                errorData.error &&
                errorData.error.includes("zaten sepetinizde")
              ) {
                console.warn("⚠️ Ürün zaten backend sepetinde mevcut");
                showSuccessMessage(`${product.name} sepete eklendi!`);
              } else {
                console.warn(
                  "⚠️ Backend sepetine eklenemedi:",
                  errorData.error
                );
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
      } else {
        showWarningMessage("Ürün zaten sepetinizde mevcut");
      }
    } catch (error) {
      console.error("Sepet işlemi başarısız:", error);
      showWarningMessage("Sepete eklenirken bir hata oluştu");
    } finally {
      setLoadingProducts((prev) => {
        const newSet = new Set(prev);
        newSet.delete(product.id);
        return newSet;
      });
    }
  };

  // Başarı mesajı göster
  const showSuccessMessage = (message) => {
    const notification = document.createElement("div");
    notification.className = "alert alert-success position-fixed";
    notification.style.cssText =
      "top: 80px; right: 20px; z-index: 9999; min-width: 350px; max-width: 400px;";
    notification.innerHTML = `
      <div class="d-flex align-items-center">
        <i class="bi bi-check-circle me-2"></i>
        <div class="flex-grow-1">${message}</div>
        <button type="button" class="btn-close ms-2" onclick="this.parentElement.parentElement.remove()"></button>
      </div>
    `;
    document.body.appendChild(notification);

    // 4 saniye sonra otomatik kaldır
    setTimeout(() => {
      if (notification.parentElement) {
        notification.remove();
      }
    }, 4000);
  };

  // Uyarı mesajı göster
  const showWarningMessage = (message) => {
    const notification = document.createElement("div");
    notification.className = "alert alert-warning position-fixed";
    notification.style.cssText =
      "top: 80px; right: 20px; z-index: 9999; min-width: 350px;";
    notification.innerHTML = `
      <div class="d-flex align-items-center">
        <i class="bi bi-exclamation-triangle me-2"></i>
        <div class="flex-grow-1">${message}</div>
        <button type="button" class="btn-close ms-2" onclick="this.parentElement.parentElement.remove()"></button>
      </div>
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

  // Ürün yoksa hiçbir şey render etme
  if (!products || products.length === 0) {
    return (
      <div className="container py-5">
        <div className="text-center">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="mt-3 text-muted">Öne çıkan ürünler yükleniyor...</p>
        </div>
      </div>
    );
  }

  return (
    <div
      id="topSellingCarousel"
      className="carousel slide carousel-fade mb-5"
      data-bs-ride="carousel"
      data-bs-interval="5000" // 5 saniyede bir otomatik geçiş
    >
      {/* Carousel Indicators */}
      <div className="carousel-indicators">
        {products.map((_, index) => (
          <button
            key={index}
            type="button"
            data-bs-target="#topSellingCarousel"
            data-bs-slide-to={index}
            className={index === 0 ? "active" : ""}
            aria-current={index === 0 ? "true" : "false"}
            aria-label={`Slide ${index + 1}`}
          ></button>
        ))}
      </div>

      <div className="carousel-inner rounded shadow-lg">
        {products.map((product, index) => {
          const isProductInCart = cartItems.has(product.id);
          const isLoading = loadingProducts.has(product.id);
          const stockQuantity = product.sold || product.stockQuantity || 0;
          const isOutOfStock = stockQuantity === 0;

          return (
            <div
              key={product.id}
              className={`carousel-item ${index === 0 ? "active" : ""}`}
            >
              {/* Ürün Resmi */}
              <img
                src={product.image}
                className="d-block w-100"
                alt={product.name}
                style={{
                  height: "650px",
                  objectFit: "cover",
                  filter: "brightness(70%)",
                }}
                onError={(e) => {
                  // Resim yüklenemezse kategoriye uygun placeholder göster
                  const encodedName = encodeURIComponent(product.name);
                  e.target.src = `https://via.placeholder.com/1200x650/6c757d/ffffff?text=${encodedName}`;
                  e.target.onerror = null; // Sonsuz döngüyü önle
                }}
              />

              {/* Sepette olma durumu badge'i */}
              {isProductInCart && (
                <div
                  className="position-absolute top-0 end-0 m-4"
                  style={{ zIndex: 3 }}
                >
                  <span className="badge bg-info fs-6 px-3 py-2">
                    <i className="bi bi-cart-check me-1"></i>
                    Sepette
                  </span>
                </div>
              )}

              {/* Carousel Caption */}
              <div className="carousel-caption d-none d-md-block">
                <div className="container">
                  <div className="row justify-content-center">
                    <div className="col-lg-8">
                      {/* Kategori Badge */}
                      <span className="badge bg-primary fs-6 mb-3">
                        {product.category}
                      </span>

                      {/* Ürün Adı */}
                      <h2 className="fw-bold display-5 mb-3 text-white">
                        {product.name}
                      </h2>

                      {/* Ürün Açıklaması */}
                      <p className="fs-5 mb-4 text-white-50">
                        {product.description && product.description.length > 150
                          ? product.description.substring(0, 150) + "..."
                          : product.description ||
                            "Bu ürün hakkında detaylı bilgi için ürün sayfasını ziyaret edin."}
                      </p>

                      {/* Stok Bilgisi */}
                      <div className="mb-3">
                        {!isOutOfStock && (
                          <span className="badge bg-success fs-6 me-2">
                            <i className="bi bi-box-seam me-1"></i>
                            {stockQuantity} adet stokta
                          </span>
                        )}
                        {stockQuantity <= 10 && stockQuantity > 0 && (
                          <span className="badge bg-warning text-dark fs-6 me-2">
                            <i className="bi bi-exclamation-triangle me-1"></i>
                            Son ürünler!
                          </span>
                        )}
                        {isOutOfStock && (
                          <span className="badge bg-danger fs-6">
                            <i className="bi bi-x-circle me-1"></i>
                            Tükendi
                          </span>
                        )}
                      </div>

                      {/* Fiyat ve Buton */}
                      <div className="d-flex align-items-center justify-content-center gap-3 mt-4">
                        <h3 className="fw-bold text-warning mb-0 display-6">
                          {formatPrice(product.price)}
                        </h3>

                        <div className="d-flex gap-2">
                          {/* Ana Sepet Butonu */}
                          <button
                            className={`btn btn-lg fw-semibold shadow-lg px-4 py-2 ${
                              isOutOfStock
                                ? "btn-outline-secondary"
                                : isProductInCart
                                ? "btn-outline-warning"
                                : "btn-success"
                            }`}
                            style={{
                              whiteSpace: "nowrap",
                              background: isOutOfStock
                                ? "transparent"
                                : isProductInCart
                                ? "transparent"
                                : "linear-gradient(45deg, #28a745, #20c997)",
                              border: isOutOfStock
                                ? "2px solid #6c757d"
                                : isProductInCart
                                ? "2px solid #ffc107"
                                : "none",
                              borderRadius: "25px",
                            }}
                            onClick={() => handleAddToCart(product)}
                            disabled={isOutOfStock || isLoading}
                            onMouseOver={(e) => {
                              if (!isOutOfStock && !isLoading) {
                                e.target.style.transform = "scale(1.05)";
                                e.target.style.transition = "transform 0.2s";
                              }
                            }}
                            onMouseOut={(e) => {
                              e.target.style.transform = "scale(1)";
                            }}
                          >
                            {isLoading ? (
                              <>
                                <div
                                  className="spinner-border spinner-border-sm me-2"
                                  role="status"
                                >
                                  <span className="visually-hidden">
                                    Yükleniyor...
                                  </span>
                                </div>
                                İşleniyor...
                              </>
                            ) : isOutOfStock ? (
                              <>
                                <i className="bi bi-x-circle me-2"></i>
                                Tükendi
                              </>
                            ) : isProductInCart ? (
                              <>
                                <i className="bi bi-cart-check me-2"></i>
                                Sepete Git
                              </>
                            ) : (
                              <>
                                <i className="bi bi-cart-plus me-2"></i>
                                Sepete Ekle
                              </>
                            )}
                          </button>

                          {/* Detay Butonu */}
                          <button
                            className="btn btn-outline-light btn-lg px-4 py-2"
                            style={{
                              borderRadius: "25px",
                              whiteSpace: "nowrap",
                            }}
                            onClick={() => navigate(`/product/${product.id}`)}
                          >
                            <i className="bi bi-eye me-2"></i>
                            Detay
                          </button>
                        </div>
                      </div>

                      {/* Giriş önerisi */}
                      {!isAuthenticated() && !isOutOfStock && (
                        <div className="mt-3">
                          <small className="text-white-50">
                            <i className="bi bi-info-circle me-1"></i>
                            <span
                              className="text-warning"
                              style={{
                                cursor: "pointer",
                                textDecoration: "underline",
                              }}
                              onClick={() => navigate("/login")}
                            >
                              Hesap oluşturun
                            </span>{" "}
                            ve sepetinizi güvende tutun
                          </small>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>

              {/* Mobile Caption */}
              <div className="carousel-caption d-md-none">
                <h5 className="fw-bold">{product.name}</h5>
                <p className="fs-6">{formatPrice(product.price)}</p>

                <div className="d-flex justify-content-center gap-2">
                  <button
                    className={`btn btn-sm ${
                      isOutOfStock
                        ? "btn-outline-secondary"
                        : isProductInCart
                        ? "btn-outline-warning"
                        : "btn-success"
                    }`}
                    onClick={() => handleAddToCart(product)}
                    disabled={isOutOfStock || isLoading}
                  >
                    {isLoading ? (
                      <div
                        className="spinner-border spinner-border-sm"
                        role="status"
                      >
                        <span className="visually-hidden">Yükleniyor...</span>
                      </div>
                    ) : isOutOfStock ? (
                      <>
                        <i className="bi bi-x-circle me-1"></i>
                        Tükendi
                      </>
                    ) : isProductInCart ? (
                      <>
                        <i className="bi bi-cart-check me-1"></i>
                        Sepette
                      </>
                    ) : (
                      <>
                        <i className="bi bi-cart-plus me-1"></i>
                        Sepete Ekle
                      </>
                    )}
                  </button>

                  <button
                    className="btn btn-outline-light btn-sm"
                    onClick={() => navigate(`/product/${product.id}`)}
                  >
                    <i className="bi bi-eye me-1"></i>
                    Detay
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Carousel Controls */}
      <button
        className="carousel-control-prev"
        type="button"
        data-bs-target="#topSellingCarousel"
        data-bs-slide="prev"
      >
        <span
          className="carousel-control-prev-icon bg-dark rounded-circle p-3"
          style={{ opacity: "0.8" }}
        />
        <span className="visually-hidden">Önceki</span>
      </button>
      <button
        className="carousel-control-next"
        type="button"
        data-bs-target="#topSellingCarousel"
        data-bs-slide="next"
      >
        <span
          className="carousel-control-next-icon bg-dark rounded-circle p-3"
          style={{ opacity: "0.8" }}
        />
        <span className="visually-hidden">Sonraki</span>
      </button>

      {/* Overlay için ekstra stil */}
      <style jsx>{`
        .carousel-item::before {
          content: "";
          position: absolute;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: linear-gradient(
            to bottom,
            rgba(0, 0, 0, 0.3) 0%,
            rgba(0, 0, 0, 0.1) 50%,
            rgba(0, 0, 0, 0.7) 100%
          );
          z-index: 1;
        }

        .carousel-caption {
          z-index: 2;
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

        .btn:hover:not(:disabled) {
          transform: scale(1.05) !important;
          transition: transform 0.2s ease !important;
        }
      `}</style>
    </div>
  );
};

export default TopSellingCarousel;
