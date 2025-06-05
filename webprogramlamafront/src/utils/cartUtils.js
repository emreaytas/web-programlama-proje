// src/utils/cartUtils.js - Güncellenmiş localStorage yönetimi
const CART_KEY = "shopping_cart";

// Sepeti getir
export const getCart = () => {
  try {
    const cart = localStorage.getItem(CART_KEY);
    return cart ? JSON.parse(cart) : [];
  } catch (error) {
    console.error("Sepet verisi okunamadı:", error);
    return [];
  }
};

// Sepete ürün ekle
export const addToCart = (product) => {
  try {
    const currentCart = getCart();

    // Ürün zaten sepette var mı kontrol et
    const existingProductIndex = currentCart.findIndex(
      (item) => item.id === product.id
    );

    if (existingProductIndex !== -1) {
      // Ürün zaten var, güncelle (miktar artırımı yapılabilir)
      console.log("Ürün zaten sepette mevcut:", product.name);
      return false; // Eklenmedi
    } else {
      // Yeni ürün ekle
      const productToAdd = {
        id: product.id,
        name: product.name,
        description: product.description,
        price: product.price,
        category: product.category,
        image: product.image,
        addedAt: new Date().toISOString(),
        // Backend ID'si için yer tutucu (backend'den dönecek)
        backendId: null,
      };

      const updatedCart = [...currentCart, productToAdd];
      localStorage.setItem(CART_KEY, JSON.stringify(updatedCart));

      // Storage event'i tetikle (farklı tab'lar için)
      window.dispatchEvent(
        new StorageEvent("storage", {
          key: CART_KEY,
          newValue: JSON.stringify(updatedCart),
          oldValue: JSON.stringify(currentCart),
        })
      );

      console.log("Ürün sepete eklendi:", product.name);
      return true; // Başarıyla eklendi
    }
  } catch (error) {
    console.error("Sepete ekleme hatası:", error);
    return false;
  }
};

// Sepetten ürün çıkar (Product ID ile)
export const removeFromCart = (productId) => {
  try {
    const currentCart = getCart();
    const updatedCart = currentCart.filter((item) => item.id !== productId);

    localStorage.setItem(CART_KEY, JSON.stringify(updatedCart));

    // Storage event'i tetikle
    window.dispatchEvent(
      new StorageEvent("storage", {
        key: CART_KEY,
        newValue: JSON.stringify(updatedCart),
        oldValue: JSON.stringify(currentCart),
      })
    );

    console.log("Ürün sepetten çıkarıldı:", productId);
    return true;
  } catch (error) {
    console.error("Sepetten çıkarma hatası:", error);
    return false;
  }
};

// Sepeti temizle
export const clearCart = () => {
  try {
    const currentCart = getCart();
    localStorage.removeItem(CART_KEY);

    // Storage event'i tetikle
    window.dispatchEvent(
      new StorageEvent("storage", {
        key: CART_KEY,
        newValue: null,
        oldValue: JSON.stringify(currentCart),
      })
    );

    console.log("Sepet temizlendi");
    return true;
  } catch (error) {
    console.error("Sepet temizleme hatası:", error);
    return false;
  }
};

// Sepet sayısını getir
export const getCartCount = () => {
  const cart = getCart();
  return cart.length;
};

// Sepet toplamını hesapla
export const getCartTotal = () => {
  const cart = getCart();
  return cart.reduce((total, item) => total + (item.price || 0), 0);
};

// Ürünün sepette olup olmadığını kontrol et
export const isInCart = (productId) => {
  const cart = getCart();
  return cart.some((item) => item.id === productId);
};

// Sepet özetini getir
export const getCartSummary = () => {
  const cart = getCart();
  return {
    items: cart,
    count: cart.length,
    total: getCartTotal(),
    isEmpty: cart.length === 0,
    lastUpdated: new Date().toISOString(),
  };
};

// Backend ID'sini güncelle (backend'den başarılı yanıt geldiğinde)
export const updateBackendId = (productId, backendId) => {
  try {
    const currentCart = getCart();
    const updatedCart = currentCart.map((item) => {
      if (item.id === productId) {
        return { ...item, backendId: backendId };
      }
      return item;
    });

    localStorage.setItem(CART_KEY, JSON.stringify(updatedCart));
    console.log(
      `Backend ID güncellendi - Product: ${productId}, Backend: ${backendId}`
    );
    return true;
  } catch (error) {
    console.error("Backend ID güncelleme hatası:", error);
    return false;
  }
};

// Backend ile senkronizasyon için yardımcı fonksiyonlar
export const cartSync = {
  // Backend'deki sepet ile karşılaştır
  compareWithBackend(backendCart) {
    const localCart = getCart();
    const localIds = localCart.map((item) => item.id);
    const backendIds = backendCart.map((item) => item.UrunId || item.ProductId);

    return {
      localOnly: localIds.filter((id) => !backendIds.includes(id)),
      backendOnly: backendIds.filter((id) => !localIds.includes(id)),
      common: localIds.filter((id) => backendIds.includes(id)),
      needsSync:
        localIds.length !== backendIds.length ||
        !localIds.every((id) => backendIds.includes(id)),
    };
  },

  // Backend'den gelen veri ile localStorage'ı güncelle
  updateFromBackend(backendCart) {
    try {
      const localCart = getCart();
      const updatedCart = [...localCart];

      // Backend'deki her ürün için kontrol et
      backendCart.forEach((backendItem) => {
        const productId = backendItem.UrunId || backendItem.ProductId;
        const existingIndex = updatedCart.findIndex(
          (item) => item.id === productId
        );

        if (existingIndex !== -1) {
          // Backend ID'sini güncelle
          updatedCart[existingIndex].backendId = backendItem.Id;
        } else {
          // Backend'de var ama local'de yok, ekle
          if (backendItem.Product) {
            updatedCart.push({
              id: backendItem.Product.Id,
              name: backendItem.Product.Name,
              description: backendItem.Product.Description,
              price: backendItem.Product.Price,
              category: backendItem.Product.Category,
              image: backendItem.Product.ImageUrl,
              addedAt: backendItem.EklenmeTarihi,
              backendId: backendItem.Id,
            });
          }
        }
      });

      localStorage.setItem(CART_KEY, JSON.stringify(updatedCart));

      // Storage event'i tetikle
      window.dispatchEvent(
        new StorageEvent("storage", {
          key: CART_KEY,
          newValue: JSON.stringify(updatedCart),
          oldValue: JSON.stringify(localCart),
        })
      );

      console.log("Sepet backend ile senkronize edildi");
      return true;
    } catch (error) {
      console.error("Backend senkronizasyon hatası:", error);
      return false;
    }
  },

  // Sadece local'de olan ürünleri backend'e gönder
  async syncToBackend() {
    if (!window.localStorage.getItem("authToken")) {
      console.log(
        "Kullanıcı giriş yapmamış, backend senkronizasyonu atlanıyor"
      );
      return;
    }

    try {
      const localCart = getCart();
      const localOnlyItems = localCart.filter((item) => !item.backendId);

      for (const item of localOnlyItems) {
        try {
          const response = await fetch("https://localhost:7062/api/Urun", {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Authorization: `Bearer ${localStorage.getItem("authToken")}`,
              Accept: "application/json",
            },
            body: JSON.stringify({ urunId: item.id }),
          });

          if (response.ok) {
            const data = await response.json();
            updateBackendId(item.id, data.urunId);
            console.log(`✅ ${item.name} backend'e senkronize edildi`);
          } else {
            const errorData = await response.json().catch(() => ({}));
            if (!errorData.error?.includes("zaten sepetinizde")) {
              console.warn(
                `⚠️ ${item.name} backend'e gönderilemedi:`,
                errorData.error
              );
            }
          }
        } catch (error) {
          console.warn(`⚠️ ${item.name} backend senkronizasyon hatası:`, error);
        }
      }
    } catch (error) {
      console.error("Backend senkronizasyon genel hatası:", error);
    }
  },
};

// Debug için sepet durumunu logla
export const debugCart = () => {
  const cart = getCart();
  console.group("🛒 Sepet Debug Bilgileri");
  console.log("Toplam ürün:", cart.length);
  console.log("Toplam tutar:", getCartTotal());
  console.log("Ürünler:", cart);
  console.log(
    "Backend ID'li ürünler:",
    cart.filter((item) => item.backendId)
  );
  console.log(
    "Sadece local ürünler:",
    cart.filter((item) => !item.backendId)
  );
  console.groupEnd();
};

// Storage event listener'ı için yardımcı
export const onCartChange = (callback) => {
  const handleStorageChange = (e) => {
    if (e.key === CART_KEY) {
      const newCart = e.newValue ? JSON.parse(e.newValue) : [];
      callback(newCart);
    }
  };

  window.addEventListener("storage", handleStorageChange);

  // Cleanup function döndür
  return () => {
    window.removeEventListener("storage", handleStorageChange);
  };
};
