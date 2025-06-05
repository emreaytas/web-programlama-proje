// src/api/urunAPI.js
import { useState } from "react";

const API_BASE_URL = "https://localhost:7062/api";

// Get auth token
const getAuthToken = () => {
  return localStorage.getItem("authToken");
};

// Generic API call function
const apiCall = async (endpoint, options = {}) => {
  const url = `${API_BASE_URL}${endpoint}`;
  const token = getAuthToken();

  const defaultHeaders = {
    "Content-Type": "application/json",
    Accept: "application/json",
  };

  if (token) {
    defaultHeaders["Authorization"] = `Bearer ${token}`;
  }

  const config = {
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
  };

  try {
    const response = await fetch(url, config);

    // Handle auth errors
    if (response.status === 401) {
      localStorage.removeItem("authToken");
      localStorage.removeItem("user");
      window.location.href = "/login";
      throw new Error("Oturum süresi doldu. Lütfen tekrar giriş yapın.");
    }

    if (response.status === 403) {
      throw new Error("Bu işlem için yetkiniz bulunmuyor.");
    }

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.error || `HTTP Error: ${response.status}`);
    }

    // Return empty object for 204 No Content
    if (response.status === 204) {
      return {};
    }

    return await response.json();
  } catch (error) {
    console.error("Urun API call failed:", error);
    throw error;
  }
};

// Urun API functions
export const urunAPI = {
  // Kullanıcının sepetindeki tüm ürünleri getir
  async getAll() {
    return await apiCall("/Urun");
  },

  // Sepete ürün ekle
  async add(urunId) {
    return await apiCall("/Urun", {
      method: "POST",
      body: JSON.stringify({ urunId }),
    });
  },

  // Sepetten ürün çıkar (ID ile)
  async remove(id) {
    return await apiCall(`/Urun/${id}`, {
      method: "DELETE",
    });
  },

  // Sepetten ürün çıkar (Product ID ile)
  async removeByProductId(productId) {
    return await apiCall(`/Urun/product/${productId}`, {
      method: "DELETE",
    });
  },

  // Sepeti tamamen temizle
  async clear() {
    return await apiCall("/Urun/clear", {
      method: "DELETE",
    });
  },

  // Sepetteki ürün sayısını getir
  async getCount() {
    return await apiCall("/Urun/count");
  },

  // Ürünün sepette olup olmadığını kontrol et
  async exists(productId) {
    return await apiCall(`/Urun/exists/${productId}`);
  },

  // Sepet özetini getir
  async getSummary() {
    return await apiCall("/Urun/summary");
  },
};

// Utility functions
export const urunUtils = {
  // Fiyat formatla
  formatPrice(price) {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
    }).format(price);
  },

  // Tarih formatla
  formatDate(dateString) {
    return new Date(dateString).toLocaleDateString("tr-TR", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  },

  // Toast mesajları için yardımcı
  showToast(message, type = "info") {
    // Bootstrap toast veya başka bir toast kütüphanesi kullanılabilir
    console.log(`[${type.toUpperCase()}] ${message}`);

    // Basit alert kullanımı (daha sonra toast library ile değiştirilebilir)
    if (type === "error") {
      alert(`Hata: ${message}`);
    } else if (type === "success") {
      alert(`Başarılı: ${message}`);
    }
  },

  // Local storage ile birlikte çalışma
  syncWithLocalStorage: {
    // Backend sepeti local storage'a kaydet
    async saveToLocal() {
      try {
        const sepet = await urunAPI.getAll();
        const localData = sepet.Urunler.map((urun) => ({
          id: urun.Product.Id,
          name: urun.Product.Name,
          price: urun.Product.Price,
          image: urun.Product.ImageUrl,
          category: urun.Product.Category,
          description: urun.Product.Description,
        }));
        localStorage.setItem("shopping_cart", JSON.stringify(localData));
        console.log("Backend sepeti local storage'a kaydedildi");
      } catch (error) {
        console.error("Backend sepet local storage'a kaydedilemedi:", error);
      }
    },

    // Local storage'dan backend'e senkronize et
    async syncToBackend() {
      try {
        const localCart = localStorage.getItem("shopping_cart");
        if (!localCart) return;

        const localItems = JSON.parse(localCart);

        for (const item of localItems) {
          try {
            // Ürünün backend sepetinde olup olmadığını kontrol et
            const exists = await urunAPI.exists(item.id);
            if (!exists.exists) {
              await urunAPI.add(item.id);
              console.log(`Ürün ${item.name} backend sepetine eklendi`);
            }
          } catch (error) {
            console.error(`Ürün ${item.name} backend'e eklenemedi:`, error);
          }
        }

        // Senkronizasyon sonrası local storage'ı güncelle
        await this.saveToLocal();
        console.log("Local storage backend ile senkronize edildi");
      } catch (error) {
        console.error("Local storage backend ile senkronize edilemedi:", error);
      }
    },

    // Çift yönlü senkronizasyon
    async fullSync() {
      try {
        // Önce backend'den güncel veriyi al
        await this.saveToLocal();

        // Sonra local'deki eksik verileri backend'e gönder
        await this.syncToBackend();

        console.log("Tam senkronizasyon tamamlandı");
      } catch (error) {
        console.error("Tam senkronizasyon başarısız:", error);
      }
    },
  },
};

// React Hook for Urun operations
export const useUrun = () => {
  const [sepet, setSepet] = useState([]);
  const [loading, setLoading] = useState(false);
  const [count, setCount] = useState(0);

  // Sepeti yükle
  const loadSepet = async () => {
    setLoading(true);
    try {
      const response = await urunAPI.getAll();
      setSepet(response.Urunler || []);
      setCount(response.UrunSayisi || 0);
    } catch (error) {
      console.error("Sepet yüklenemedi:", error);
      urunUtils.showToast("Sepet yüklenemedi", "error");
    } finally {
      setLoading(false);
    }
  };

  // Sepete ürün ekle
  const addToSepet = async (productId) => {
    try {
      const response = await urunAPI.add(productId);
      urunUtils.showToast(response.message, "success");
      await loadSepet(); // Sepeti yenile
      await urunUtils.syncWithLocalStorage.saveToLocal(); // Local storage'ı güncelle
      return response;
    } catch (error) {
      urunUtils.showToast(error.message, "error");
      throw error;
    }
  };

  // Sepetten ürün çıkar
  const removeFromSepet = async (productId) => {
    try {
      const response = await urunAPI.removeByProductId(productId);
      urunUtils.showToast(response.message, "success");
      await loadSepet(); // Sepeti yenile
      await urunUtils.syncWithLocalStorage.saveToLocal(); // Local storage'ı güncelle
      return response;
    } catch (error) {
      urunUtils.showToast(error.message, "error");
      throw error;
    }
  };

  // Sepeti temizle
  const clearSepet = async () => {
    try {
      const response = await urunAPI.clear();
      urunUtils.showToast(response.message, "success");
      setSepet([]);
      setCount(0);
      localStorage.removeItem("shopping_cart"); // Local storage'ı da temizle
      return response;
    } catch (error) {
      urunUtils.showToast(error.message, "error");
      throw error;
    }
  };

  // Sepet sayısını getir
  const loadCount = async () => {
    try {
      const response = await urunAPI.getCount();
      setCount(response.count);
    } catch (error) {
      console.error("Sepet sayısı alınamadı:", error);
    }
  };

  return {
    sepet,
    loading,
    count,
    loadSepet,
    addToSepet,
    removeFromSepet,
    clearSepet,
    loadCount,
  };
};

export default urunAPI;
