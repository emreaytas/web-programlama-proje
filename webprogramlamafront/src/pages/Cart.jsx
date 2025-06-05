// src/pages/Cart.jsx - Düzeltilmiş e-posta sistemi ile
import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
  isAuthenticated,
  getToken,
  getUser,
  debugAuth,
} from "../utils/tokenUtils";
import { getCart, clearCart, removeFromCart } from "../utils/cartUtils";
import { urunAPI, urunUtils, useUrun } from "../api/urunAPI";

const Cart = () => {
  const navigate = useNavigate();
  const [localCartItems, setLocalCartItems] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [quantities, setQuantities] = useState({});
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [checkoutStep, setCheckoutStep] = useState("cart");
  const [shippingInfo, setShippingInfo] = useState({
    fullName: "",
    email: "",
    phone: "",
    address: "",
    city: "",
    postalCode: "",
    notes: "",
  });

  // Backend sepet hook'u
  const {
    sepet: backendSepet,
    loading: backendLoading,
    count: backendCount,
    loadSepet,
    removeFromSepet,
    clearSepet,
  } = useUrun();

  // Sayfa yüklendiğinde sepet verilerini getir
  useEffect(() => {
    const loadCartData = async () => {
      setIsLoading(true);
      setError(null);

      console.log("🔍 Cart.jsx - Authentication Debug:");
      debugAuth();

      try {
        const localItems = getCart();
        setLocalCartItems(localItems);

        const initialQuantities = {};
        localItems.forEach((item, index) => {
          initialQuantities[index] = 1;
        });
        setQuantities(initialQuantities);

        const token = getToken();
        const user = getUser();
        const authStatus = isAuthenticated();

        console.log("🔐 Authentication Status:", {
          token: !!token,
          user: !!user,
          authStatus,
        });

        if (authStatus && token && user) {
          console.log(
            "✅ Kullanıcı giriş yapmış, backend sepeti yükleniyor..."
          );
          await loadSepet();
          try {
            await urunUtils.syncWithLocalStorage.fullSync();
            const updatedLocalItems = getCart();
            setLocalCartItems(updatedLocalItems);
          } catch (syncError) {
            console.warn("Sepet senkronizasyonu başarısız:", syncError);
          }
        } else {
          console.log(
            "ℹ️ Kullanıcı giriş yapmamış, sadece local sepet kullanılıyor"
          );
        }
      } catch (err) {
        console.error("Sepet yükleme hatası:", err);
        setError("Sepet verilerini yüklerken bir hata oluştu");
      } finally {
        setIsLoading(false);
      }
    };

    loadCartData();
  }, []);

  // Miktar güncelleme
  const handleQuantityChange = (itemIndex, newQuantity) => {
    if (newQuantity < 1) return;
    setQuantities((prev) => ({
      ...prev,
      [itemIndex]: newQuantity,
    }));
  };

  // Ürün silme işlemi
  const handleRemoveItem = async (productId, itemIndex) => {
    try {
      removeFromCart(productId);
      const updatedLocalCart = getCart();
      setLocalCartItems(updatedLocalCart);

      const newQuantities = { ...quantities };
      delete newQuantities[itemIndex];
      const reorderedQuantities = {};
      Object.keys(newQuantities).forEach((key, index) => {
        if (parseInt(key) > itemIndex) {
          reorderedQuantities[parseInt(key) - 1] = newQuantities[key];
        } else if (parseInt(key) < itemIndex) {
          reorderedQuantities[key] = newQuantities[key];
        }
      });
      setQuantities(reorderedQuantities);

      if (isAuthenticated()) {
        try {
          await removeFromSepet(productId);
        } catch (backendError) {
          console.warn("Backend'den silme başarısız:", backendError);
        }
      }

      showToast("Ürün sepetten çıkarıldı", "success");
    } catch (error) {
      console.error("Ürün silme hatası:", error);
      showToast("Ürün silinirken hata oluştu", "error");
    }
  };

  // Sepeti temizleme işlemi
  const handleClearCart = async () => {
    if (
      !window.confirm("Sepeti tamamen temizlemek istediğinizden emin misiniz?")
    ) {
      return;
    }

    try {
      clearCart();
      setLocalCartItems([]);
      setQuantities({});

      if (isAuthenticated()) {
        try {
          await clearSepet();
        } catch (backendError) {
          console.warn("Backend sepet temizleme başarısız:", backendError);
        }
      }

      showToast("Sepet başarıyla temizlendi", "success");
    } catch (error) {
      console.error("Sepet temizleme hatası:", error);
      showToast("Sepet temizlenirken hata oluştu", "error");
    }
  };

  // Alışverişi tamamlama - Düzeltilmiş e-posta sistemi
  const handleCompleteOrder = async () => {
    const token = getToken();
    const user = getUser();
    const authStatus = isAuthenticated();

    console.log("🛒 Sipariş tamamlama - Authentication kontrolü:", {
      token: token ? "✅ Mevcut" : "❌ Yok",
      user: user ? "✅ Mevcut" : "❌ Yok",
      authStatus: authStatus ? "✅ Authenticated" : "❌ Not Authenticated",
      tokenLength: token ? token.length : 0,
      userDetails: user
        ? { id: user.id, email: user.email, username: user.username }
        : null,
    });

    if (!authStatus || !token || !user) {
      console.log(
        "❌ Authentication başarısız, giriş sayfasına yönlendiriliyor"
      );

      if (!token || !user) {
        console.log("🗑️ Eksik authentication verileri temizleniyor");
        localStorage.removeItem("authToken");
        localStorage.removeItem("user");
      }

      if (
        window.confirm(
          "Siparişi tamamlamak için giriş yapmanız gerekiyor. Giriş sayfasına yönlendirilmek istiyor musunuz?"
        )
      ) {
        navigate("/login");
      }
      return;
    }

    // Form validasyonu
    const requiredFields = ["fullName", "email", "phone", "address", "city"];
    const missingFields = requiredFields.filter(
      (field) => !shippingInfo[field]?.trim()
    );

    if (missingFields.length > 0) {
      showToast(
        `Lütfen şu alanları doldurun: ${missingFields.join(", ")}`,
        "error"
      );
      return;
    }

    console.log(
      "✅ Authentication ve validasyon başarılı, sipariş oluşturuluyor..."
    );
    setIsCheckingOut(true);

    try {
      // Sipariş verilerini hazırla
      const orderData = {
        items: localCartItems.map((item, index) => ({
          productId: item.id,
          productName: item.name,
          quantity: quantities[index] || 1,
          unitPrice: item.price,
          totalPrice: item.price * (quantities[index] || 1),
        })),
        shippingInfo: shippingInfo,
        totalAmount: calculateTotal(localCartItems),
        orderDate: new Date().toISOString(),
      };

      console.log("📦 Sipariş verisi hazırlandı:", orderData);

      // Backend'e sipariş gönder
      const orderResponse = await fetch("https://localhost:7062/api/Orders", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
          Accept: "application/json",
        },
        body: JSON.stringify({
          shippingAddress: `${shippingInfo.fullName}\n${shippingInfo.address}\n${shippingInfo.city} ${shippingInfo.postalCode}\nTel: ${shippingInfo.phone}`,
        }),
      });

      if (orderResponse.ok) {
        const orderResult = await orderResponse.json();
        console.log("✅ Sipariş başarıyla oluşturuldu:", orderResult);

        // E-posta gönderme işlemi - Tek endpoint kullan
        await sendOrderEmails(orderData, orderResult);

        // Sepeti temizle
        clearCart();
        setLocalCartItems([]);
        setQuantities({});

        if (isAuthenticated()) {
          try {
            await clearSepet();
          } catch (backendError) {
            console.warn("Backend sepet temizleme başarısız:", backendError);
          }
        }

        // Başarı sayfasına yönlendir
        setCheckoutStep("complete");
        showToast(
          "Siparişiniz başarıyla oluşturuldu! E-posta bildirimleri gönderildi.",
          "success"
        );

        setTimeout(() => {
          navigate("/");
        }, 4000);
      } else {
        const errorData = await orderResponse.json().catch(() => ({}));
        throw new Error(errorData.message || "Sipariş oluşturulamadı");
      }
    } catch (error) {
      console.error("Sipariş oluşturma hatası:", error);
      showToast(
        "Sipariş oluşturulurken hata oluştu: " + error.message,
        "error"
      );
    } finally {
      setIsCheckingOut(false);
    }
  };

  // Tek fonksiyonda hem müşteri hem admin e-postalarını gönder
  const sendOrderEmails = async (orderData, orderResult) => {
    try {
      console.log("📧 Sipariş e-postaları gönderiliyor...");

      const emailRequest = {
        orderNumber: orderResult.orderNumber || "ORD-" + Date.now(),
        customerName: orderData.shippingInfo.fullName,
        customerEmail: orderData.shippingInfo.email,
        customerPhone: orderData.shippingInfo.phone,
        shippingAddress: `${orderData.shippingInfo.address}, ${orderData.shippingInfo.city} ${orderData.shippingInfo.postalCode}`,
        totalAmount: orderData.totalAmount,
        itemCount: orderData.items.length,
        items: orderData.items.map((item) => ({
          productId: item.productId,
          productName: item.productName,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          totalPrice: item.totalPrice,
        })),
      };

      console.log("📧 Email request hazırlandı:", emailRequest);

      // Yeni endpoint'i kullan
      const emailResponse = await fetch(
        "https://localhost:7062/api/EmailTest/send-order-emails", // ✅ Artık bu endpoint var
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${getToken()}`,
            Accept: "application/json",
          },
          body: JSON.stringify(emailRequest),
        }
      );

      console.log("📧 Email response status:", emailResponse.status);

      if (emailResponse.ok) {
        const result = await emailResponse.json();
        console.log("✅ Sipariş e-postaları gönderildi:", result);

        // Sonuçları kullanıcıya göster
        if (result.success) {
          showToast("E-posta bildirimleri başarıyla gönderildi!", "success");

          // Detaylı başarı bilgisi
          if (result.details) {
            console.log("📧 E-posta detayları:", result.details);
            if (
              result.details.customerEmailSent &&
              result.details.adminEmailSent
            ) {
              showToast(
                "Hem müşteri hem admin e-postaları gönderildi",
                "success"
              );
            } else if (result.details.customerEmailSent) {
              showToast(
                "Müşteri e-postası gönderildi, admin e-postası başarısız",
                "warning"
              );
            } else if (result.details.adminEmailSent) {
              showToast(
                "Admin e-postası gönderildi, müşteri e-postası başarısız",
                "warning"
              );
            }
          }
        } else {
          console.warn("⚠️ E-posta gönderiminde bazı sorunlar:", result.errors);
          showToast(
            result.message || "E-posta gönderiminde bazı sorunlar yaşandı",
            "warning"
          );

          // Hataları göster
          if (result.errors && result.errors.length > 0) {
            result.errors.forEach((error) => {
              console.error("📧 E-posta hatası:", error);
            });
          }
        }
      } else {
        const errorText = await emailResponse.text();
        console.error("❌ E-posta API hatası:", {
          status: emailResponse.status,
          statusText: emailResponse.statusText,
          body: errorText,
        });

        try {
          const errorData = JSON.parse(errorText);
          console.warn("⚠️ E-posta gönderimi başarısız:", errorData);
          showToast(
            errorData.message || "E-posta bildirimleri gönderilemedi",
            "warning"
          );
        } catch (parseError) {
          console.error("❌ E-posta response parse hatası:", parseError);
          showToast(
            `E-posta servisi hatası (${emailResponse.status})`,
            "warning"
          );
        }
      }
    } catch (error) {
      console.error("❌ E-posta gönderme genel hatası:", error);

      // Network hatası mı kontrol et
      if (
        error.name === "TypeError" &&
        error.message.includes("Failed to fetch")
      ) {
        showToast("E-posta servisi bağlantısı kurulamadı", "warning");
        console.error("🌐 Network hatası - Backend çalışıyor mu?");
      } else {
        showToast("E-posta servisi geçici olarak kullanılamıyor", "warning");
      }
    }
  };
  // Adres bilgilerini güncelleme
  const handleShippingInfoChange = (field, value) => {
    setShippingInfo((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  // Toplam hesaplama
  const calculateTotal = (items) => {
    return items.reduce((sum, item, index) => {
      const quantity = quantities[index] || 1;
      return sum + item.price * quantity;
    }, 0);
  };

  // Toast mesajı göster
  const showToast = (message, type = "info") => {
    const notification = document.createElement("div");
    notification.className = `alert alert-${
      type === "error" ? "danger" : type
    } position-fixed`;
    notification.style.cssText =
      "top: 20px; right: 20px; z-index: 9999; min-width: 300px; max-width: 400px;";
    notification.innerHTML = `
      <div class="d-flex align-items-center">
        <i class="bi bi-${
          type === "success"
            ? "check-circle"
            : type === "error"
            ? "x-circle"
            : "info-circle"
        } me-2"></i>
        <div class="flex-grow-1">${message}</div>
        <button type="button" class="btn-close ms-2" onclick="this.parentElement.parentElement.remove()"></button>
      </div>
    `;
    document.body.appendChild(notification);

    setTimeout(
      () => {
        if (notification.parentElement) {
          notification.remove();
        }
      },
      type === "success" ? 6000 : 5000
    );
  };

  // Fiyat formatı
  const formatPrice = (price) => {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
    }).format(price);
  };

  // Loading durumu
  if (isLoading || backendLoading) {
    return (
      <div className="container py-5">
        <div className="text-center">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="mt-3 text-muted">Sepetiniz yükleniyor...</p>
        </div>
      </div>
    );
  }

  // Hata durumu
  if (error) {
    return (
      <div className="container py-5">
        <div className="alert alert-danger text-center">
          <h4>Hata!</h4>
          <p>{error}</p>
          <button
            className="btn btn-primary"
            onClick={() => window.location.reload()}
          >
            Sayfayı Yenile
          </button>
        </div>
      </div>
    );
  }

  // Sipariş tamamlandı ekranı
  if (checkoutStep === "complete") {
    return (
      <div className="container py-5">
        <div className="row justify-content-center">
          <div className="col-md-6 text-center">
            <div className="card shadow-lg border-0">
              <div className="card-body p-5">
                <i className="bi bi-check-circle-fill display-1 text-success mb-4"></i>
                <h2 className="fw-bold text-success mb-3">
                  Sipariş Tamamlandı!
                </h2>
                <p className="text-muted mb-4">
                  Siparişiniz başarıyla alınmıştır. Kısa süre içinde size
                  ulaşacağız. Sipariş onayı e-posta adresinize gönderilmiştir.
                </p>
                <div className="alert alert-info">
                  <i className="bi bi-envelope-check me-2"></i>
                  E-posta bildirimleri gönderildi!
                </div>
                <div className="d-grid gap-2">
                  <button
                    className="btn btn-success btn-lg"
                    onClick={() => navigate("/")}
                  >
                    <i className="bi bi-house me-2"></i>
                    Ana Sayfaya Dön
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Sepet boş durumu
  const hasItems = localCartItems.length > 0;

  if (!hasItems) {
    return (
      <div className="container py-5">
        <div className="row justify-content-center">
          <div className="col-md-6 text-center">
            <i className="bi bi-cart-x display-1 text-muted mb-4"></i>
            <h2 className="fw-bold mb-3">Sepetiniz Boş</h2>
            <p className="text-muted mb-4">
              Henüz sepetinize ürün eklememişsiniz. Alışverişe başlamak için
              ürünlerimizi inceleyin.
            </p>
            <button
              className="btn btn-primary btn-lg"
              onClick={() => navigate("/")}
            >
              <i className="bi bi-shop me-2"></i>
              Alışverişe Başla
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Ana sepet görünümü
  const displayItems = localCartItems;
  const totalAmount = calculateTotal(localCartItems);

  return (
    <div className="container py-5">
      {/* Başlık */}
      <div className="row mb-4">
        <div className="col-12">
          <h2 className="fw-bold mb-3">
            <i className="bi bi-cart3 me-2"></i>
            Sepetiniz
          </h2>
          <div className="d-flex gap-3 mb-3">
            <span className="badge bg-primary fs-6">
              {localCartItems.length} ürün
            </span>
            <span className="badge bg-success fs-6">
              Toplam: {formatPrice(totalAmount)}
            </span>
          </div>
        </div>
      </div>

      <div className="row">
        {/* Sepet Ürünleri */}
        <div className="col-lg-8">
          <div className="card shadow-sm">
            <div className="card-header bg-light d-flex justify-content-between align-items-center">
              <h5 className="mb-0">
                <i className="bi bi-list-ul me-2"></i>
                Sepetinizdeki Ürünler ({displayItems.length})
              </h5>
              <button
                className="btn btn-outline-danger btn-sm"
                onClick={handleClearCart}
                disabled={displayItems.length === 0}
              >
                <i className="bi bi-trash me-1"></i>
                Sepeti Temizle
              </button>
            </div>
            <div className="card-body p-0">
              {displayItems.map((item, index) => (
                <div
                  key={index}
                  className="d-flex align-items-center p-3 border-bottom"
                >
                  {/* Ürün Resmi */}
                  <img
                    src={
                      item.image ||
                      `https://via.placeholder.com/80x80/6c757d/ffffff?text=${encodeURIComponent(
                        item.name
                      )}`
                    }
                    alt={item.name}
                    className="rounded me-3"
                    style={{
                      width: "80px",
                      height: "80px",
                      objectFit: "cover",
                    }}
                    onError={(e) => {
                      e.target.src = `https://via.placeholder.com/80x80/6c757d/ffffff?text=${encodeURIComponent(
                        item.name
                      )}`;
                    }}
                  />

                  {/* Ürün Bilgileri */}
                  <div className="flex-grow-1">
                    <h6 className="fw-bold mb-1">{item.name}</h6>
                    <p className="text-muted small mb-1">
                      {item.description && item.description.length > 100
                        ? item.description.substring(0, 100) + "..."
                        : item.description || "Açıklama mevcut değil"}
                    </p>
                    <span className="badge bg-secondary">{item.category}</span>
                  </div>

                  {/* Miktar Kontrolü */}
                  <div className="d-flex align-items-center me-3">
                    <button
                      className="btn btn-outline-secondary btn-sm"
                      onClick={() =>
                        handleQuantityChange(
                          index,
                          (quantities[index] || 1) - 1
                        )
                      }
                      disabled={(quantities[index] || 1) <= 1}
                    >
                      <i className="bi bi-dash"></i>
                    </button>
                    <span
                      className="mx-3 fw-bold"
                      style={{ minWidth: "30px", textAlign: "center" }}
                    >
                      {quantities[index] || 1}
                    </span>
                    <button
                      className="btn btn-outline-secondary btn-sm"
                      onClick={() =>
                        handleQuantityChange(
                          index,
                          (quantities[index] || 1) + 1
                        )
                      }
                    >
                      <i className="bi bi-plus"></i>
                    </button>
                  </div>

                  {/* Fiyat */}
                  <div className="text-end me-3">
                    <div className="small text-muted">
                      Birim: {formatPrice(item.price)}
                    </div>
                    <div className="fw-bold text-success fs-5">
                      {formatPrice(item.price * (quantities[index] || 1))}
                    </div>
                  </div>

                  {/* Silme Butonu */}
                  <button
                    className="btn btn-outline-danger btn-sm"
                    onClick={() => handleRemoveItem(item.id, index)}
                    title="Sepetten Çıkar"
                  >
                    <i className="bi bi-trash"></i>
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Sepet Özeti ve Checkout */}
        <div className="col-lg-4">
          <div className="card shadow-sm">
            <div className="card-header bg-success text-white">
              <h5 className="mb-0">
                <i className="bi bi-receipt me-2"></i>
                Sepet Özeti
              </h5>
            </div>
            <div className="card-body">
              <div className="row mb-3">
                <div className="col">Ürün Sayısı:</div>
                <div className="col-auto fw-bold">
                  {Object.values(quantities).reduce((sum, qty) => sum + qty, 0)}{" "}
                  adet
                </div>
              </div>

              <div className="row mb-3">
                <div className="col">Ara Toplam:</div>
                <div className="col-auto fw-bold text-success">
                  {formatPrice(totalAmount)}
                </div>
              </div>

              <div className="row mb-3">
                <div className="col">Kargo:</div>
                <div className="col-auto fw-bold text-success">Ücretsiz</div>
              </div>

              <hr />

              <div className="row mb-4">
                <div className="col">
                  <h5 className="fw-bold">Toplam:</h5>
                </div>
                <div className="col-auto">
                  <h5 className="fw-bold text-success">
                    {formatPrice(totalAmount)}
                  </h5>
                </div>
              </div>

              {/* Adres Bilgileri */}
              {isAuthenticated() && (
                <div className="mb-4">
                  <h6 className="fw-bold mb-3">
                    <i className="bi bi-geo-alt me-2"></i>
                    Teslimat Bilgileri
                  </h6>

                  <div className="mb-2">
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      placeholder="Ad Soyad"
                      value={shippingInfo.fullName}
                      onChange={(e) =>
                        handleShippingInfoChange("fullName", e.target.value)
                      }
                    />
                  </div>

                  <div className="mb-2">
                    <input
                      type="email"
                      className="form-control form-control-sm"
                      placeholder="E-posta"
                      value={shippingInfo.email}
                      onChange={(e) =>
                        handleShippingInfoChange("email", e.target.value)
                      }
                    />
                  </div>

                  <div className="mb-2">
                    <input
                      type="tel"
                      className="form-control form-control-sm"
                      placeholder="Telefon"
                      value={shippingInfo.phone}
                      onChange={(e) =>
                        handleShippingInfoChange("phone", e.target.value)
                      }
                    />
                  </div>

                  <div className="mb-2">
                    <textarea
                      className="form-control form-control-sm"
                      rows="2"
                      placeholder="Adres"
                      value={shippingInfo.address}
                      onChange={(e) =>
                        handleShippingInfoChange("address", e.target.value)
                      }
                    />
                  </div>

                  <div className="row">
                    <div className="col-8">
                      <input
                        type="text"
                        className="form-control form-control-sm"
                        placeholder="Şehir"
                        value={shippingInfo.city}
                        onChange={(e) =>
                          handleShippingInfoChange("city", e.target.value)
                        }
                      />
                    </div>
                    <div className="col-4">
                      <input
                        type="text"
                        className="form-control form-control-sm"
                        placeholder="Posta Kodu"
                        value={shippingInfo.postalCode}
                        onChange={(e) =>
                          handleShippingInfoChange("postalCode", e.target.value)
                        }
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* Butonlar */}
              <div className="d-grid gap-2">
                <button
                  className="btn btn-success btn-lg"
                  onClick={handleCompleteOrder}
                  disabled={
                    displayItems.length === 0 ||
                    isCheckingOut ||
                    (isAuthenticated() &&
                      (!shippingInfo.fullName ||
                        !shippingInfo.email ||
                        !shippingInfo.phone ||
                        !shippingInfo.address ||
                        !shippingInfo.city))
                  }
                >
                  {isCheckingOut ? (
                    <>
                      <div
                        className="spinner-border spinner-border-sm me-2"
                        role="status"
                      >
                        <span className="visually-hidden">Yükleniyor...</span>
                      </div>
                      Sipariş Oluşturuluyor...
                    </>
                  ) : (
                    <>
                      <i className="bi bi-check-circle me-2"></i>
                      Alışverişi Tamamla
                    </>
                  )}
                </button>

                <button
                  className="btn btn-outline-primary"
                  onClick={() => navigate("/")}
                >
                  <i className="bi bi-arrow-left me-2"></i>
                  Alışverişe Devam Et
                </button>

                {/* E-posta Test Butonu - Development için */}
                {process.env.NODE_ENV === "development" && (
                  <button
                    className="btn btn-outline-info btn-sm"
                    onClick={async () => {
                      try {
                        const response = await fetch(
                          "https://localhost:7062/api/EmailTest/test-gmail",
                          {
                            method: "POST",
                            headers: {
                              "Content-Type": "application/json",
                              Authorization: `Bearer ${getToken()}`,
                              Accept: "application/json",
                            },
                          }
                        );

                        if (response.ok) {
                          showToast("Test e-postası gönderildi!", "success");
                        } else {
                          showToast("Test e-postası gönderilemedi", "error");
                        }
                      } catch (error) {
                        showToast("E-posta servisi hatası", "error");
                      }
                    }}
                  >
                    <i className="bi bi-envelope me-1"></i>
                    Test E-posta
                  </button>
                )}
              </div>
            </div>
          </div>

          {/* Giriş Önerisi */}
          {!isAuthenticated() && (
            <div className="card mt-3 border-warning">
              <div className="card-body text-center">
                <i className="bi bi-person-plus display-6 text-warning mb-2"></i>
                <h6 className="fw-bold">Hesap Oluşturun</h6>
                <p className="text-muted small mb-3">
                  Hesap oluşturarak sepetinizi güvende tutun ve siparişlerinizi
                  takip edin.
                </p>
                <button
                  className="btn btn-warning btn-sm"
                  onClick={() => navigate("/login")}
                >
                  Giriş Yap / Kayıt Ol
                </button>
              </div>
            </div>
          )}

          {/* E-posta Durumu */}
          <div className="card mt-3 border-0 bg-light">
            <div className="card-body text-center py-3">
              <div className="d-flex justify-content-center gap-3 mb-2">
                <i className="bi bi-shield-check text-success fs-4"></i>
                <i className="bi bi-truck text-primary fs-4"></i>
                <i className="bi bi-envelope-check text-info fs-4"></i>
              </div>
              <small className="text-muted">
                Güvenli Ödeme • Hızlı Kargo • E-posta Bildirimleri
              </small>
            </div>
          </div>
        </div>
      </div>

      {/* E-posta Bilgilendirme */}
      {isAuthenticated() && (
        <div className="row mt-4">
          <div className="col-12">
            <div className="alert alert-info">
              <div className="d-flex align-items-center">
                <i className="bi bi-envelope-heart display-6 me-3 text-info"></i>
                <div>
                  <h6 className="fw-bold mb-1">📧 E-posta Bildirimleri</h6>
                  <p className="mb-0 small">
                    Siparişiniz tamamlandığında hem size hem de mağaza
                    yönetimine e-posta bildirimi gönderilecektir. Sipariş
                    durumunuzu e-posta adresinizden takip edebilirsiniz.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* İstatistikler */}
      {displayItems.length > 0 && (
        <div className="row mt-4">
          <div className="col-12">
            <div className="card bg-gradient bg-primary text-white">
              <div className="card-body">
                <div className="row text-center">
                  <div className="col-md-3">
                    <i className="bi bi-box-seam display-6 mb-2"></i>
                    <h5>{displayItems.length}</h5>
                    <small>Farklı Ürün</small>
                  </div>
                  <div className="col-md-3">
                    <i className="bi bi-collection display-6 mb-2"></i>
                    <h5>
                      {Object.values(quantities).reduce(
                        (sum, qty) => sum + qty,
                        0
                      )}
                    </h5>
                    <small>Toplam Adet</small>
                  </div>
                  <div className="col-md-3">
                    <i className="bi bi-tags display-6 mb-2"></i>
                    <h5>
                      {new Set(displayItems.map((item) => item.category)).size}
                    </h5>
                    <small>Farklı Kategori</small>
                  </div>
                  <div className="col-md-3">
                    <i className="bi bi-currency-dollar display-6 mb-2"></i>
                    <h5>
                      {formatPrice(
                        totalAmount /
                          Object.values(quantities).reduce(
                            (sum, qty) => sum + qty,
                            0
                          )
                      )}
                    </h5>
                    <small>Ortalama Fiyat</small>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* CSS Animations */}
      <style jsx>{`
        .alert {
          animation: slideInDown 0.3s ease-out;
        }

        @keyframes slideInDown {
          from {
            transform: translateY(-100%);
            opacity: 0;
          }
          to {
            transform: translateY(0);
            opacity: 1;
          }
        }

        .btn:hover:not(:disabled) {
          transform: translateY(-1px);
          transition: transform 0.2s ease;
        }

        .card {
          transition: box-shadow 0.3s ease;
        }

        .card:hover {
          box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
        }

        .quantity-control {
          transition: all 0.2s ease;
        }

        .quantity-control:hover {
          background-color: #f8f9fa;
        }
      `}</style>
    </div>
  );
};

export default Cart;
