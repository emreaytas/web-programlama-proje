// api.js - API helper functions
const API_BASE_URL = "https://localhost:7130/api";

// Get auth token
const getAuthToken = () => {
  return localStorage.getItem("authToken");
};

// Get current user
const getCurrentUser = () => {
  const userStr = localStorage.getItem("user");
  return userStr ? JSON.parse(userStr) : null;
};

// Check if user has specific role
const hasRole = (role) => {
  const user = getCurrentUser();
  return user && user.role === role;
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
      throw new Error(errorData.message || `HTTP Error: ${response.status}`);
    }

    // Return empty object for 204 No Content
    if (response.status === 204) {
      return {};
    }

    return await response.json();
  } catch (error) {
    console.error("API call failed:", error);
    throw error;
  }
};

// Auth API functions
export const authAPI = {
  async login(email, password) {
    return await apiCall("/Auth/Login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    });
  },

  async register(userData) {
    return await apiCall("/Auth/Register", {
      method: "POST",
      body: JSON.stringify(userData),
    });
  },

  async getCurrentUser() {
    return await apiCall("/Auth/Me");
  },

  async changePassword(passwordData) {
    return await apiCall("/Auth/ChangePassword", {
      method: "POST",
      body: JSON.stringify(passwordData),
    });
  },

  async getMyRole() {
    return await apiCall("/Auth/MyRole");
  },

  logout() {
    localStorage.removeItem("authToken");
    localStorage.removeItem("user");
    window.location.href = "/login";
  },
};

// Cart API functions
export const cartAPI = {
  async get() {
    return await apiCall("/Cart");
  },

  async getSummary() {
    return await apiCall("/Cart/Summary");
  },

  async addItem(productId, quantity = 1) {
    return await apiCall("/Cart/AddItem", {
      method: "POST",
      body: JSON.stringify({ productId, quantity }),
    });
  },

  async addMultiple(items) {
    return await apiCall("/Cart/AddMultiple", {
      method: "POST",
      body: JSON.stringify(items),
    });
  },

  async updateItem(productId, quantity) {
    return await apiCall("/Cart/UpdateItem", {
      method: "PUT",
      body: JSON.stringify({ productId, quantity }),
    });
  },

  async removeItem(productId) {
    return await apiCall(`/Cart/RemoveItem/${productId}`, {
      method: "DELETE",
    });
  },

  async removeMultiple(productIds) {
    return await apiCall("/Cart/RemoveMultiple", {
      method: "DELETE",
      body: JSON.stringify(productIds),
    });
  },

  async clear() {
    return await apiCall("/Cart/Clear", {
      method: "DELETE",
    });
  },

  async getCheckoutInfo() {
    return await apiCall("/Cart/Checkout");
  },

  async validate() {
    return await apiCall("/Cart/Validate", {
      method: "PUT",
    });
  },
};

// Orders API functions
export const ordersAPI = {
  async getAll() {
    return await apiCall("/Orders");
  },

  async getById(id) {
    return await apiCall(`/Orders/${id}`);
  },

  async create(shippingAddress) {
    return await apiCall("/Orders", {
      method: "POST",
      body: JSON.stringify({ shippingAddress }),
    });
  },

  async updateStatus(id, status) {
    return await apiCall(`/Orders/${id}/Status`, {
      method: "PUT",
      body: JSON.stringify({ status }),
    });
  },
};

// Categories API functions
export const categoriesAPI = {
  async getAll() {
    return await apiCall("/Categories");
  },

  async getProducts(category, page = 1, pageSize = 10) {
    return await apiCall(
      `/Categories/${category}/products?page=${page}&pageSize=${pageSize}`
    );
  },

  async getStats(category) {
    return await apiCall(`/Categories/${category}/stats`);
  },
};

// Admin API functions
export const adminAPI = {
  async getSystemStats() {
    return await apiCall("/Admin/SystemStats");
  },

  async bulkUpdateRoles(userIds, newRole, reason = "") {
    return await apiCall("/Admin/BulkRoleUpdate", {
      method: "PUT",
      body: JSON.stringify({ userIds, newRole, reason }),
    });
  },

  async getSettings() {
    return await apiCall("/Admin/Settings");
  },
};

// Users API functions
export const usersAPI = {
  async getAll(filters = {}) {
    const params = new URLSearchParams();

    Object.keys(filters).forEach((key) => {
      if (
        filters[key] !== undefined &&
        filters[key] !== null &&
        filters[key] !== ""
      ) {
        params.append(key, filters[key]);
      }
    });

    const queryString = params.toString();
    const endpoint = queryString ? `/Users?${queryString}` : "/Users";

    return await apiCall(endpoint);
  },

  async getById(id) {
    return await apiCall(`/Users/${id}`);
  },

  async getProfile() {
    return await apiCall("/Users/Profile");
  },

  async updateProfile(profileData) {
    return await apiCall("/Users/Profile", {
      method: "PUT",
      body: JSON.stringify(profileData),
    });
  },

  async updateRole(id, role) {
    return await apiCall(`/Users/${id}/Role`, {
      method: "PUT",
      body: JSON.stringify({ role }),
    });
  },

  async delete(id) {
    return await apiCall(`/Users/${id}`, {
      method: "DELETE",
    });
  },

  async getStats() {
    return await apiCall("/Users/Stats");
  },
};

// Dashboard API functions
export const dashboardAPI = {
  async getUserDashboard() {
    return await apiCall("/Dashboard/User");
  },

  async getAdminDashboard() {
    return await apiCall("/Dashboard/Admin");
  },

  async getSalesDashboard(startDate, endDate) {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);

    const queryString = params.toString();
    const endpoint = queryString
      ? `/Dashboard/Sales?${queryString}`
      : "/Dashboard/Sales";

    return await apiCall(endpoint);
  },
};

// Utility functions
export const utils = {
  getAuthToken,
  getCurrentUser,
  hasRole,

  isAdmin() {
    return hasRole("Admin");
  },

  isModerator() {
    return hasRole("Moderator");
  },

  isUser() {
    return hasRole("User");
  },

  isAuthenticated() {
    return !!getAuthToken();
  },

  requireAuth() {
    if (!this.isAuthenticated()) {
      window.location.href = "/login";
      return false;
    }
    return true;
  },

  requireRole(role) {
    if (!this.requireAuth()) return false;

    if (!hasRole(role)) {
      alert("Bu işlem için yetkiniz bulunmuyor.");
      return false;
    }
    return true;
  },

  formatPrice(price) {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
    }).format(price);
  },

  formatDate(dateString) {
    return new Date(dateString).toLocaleDateString("tr-TR");
  },
};

// Export everything
export { API_BASE_URL, apiCall, getAuthToken, getCurrentUser, hasRole };

// Products API functions - Güncellenmiş versiyon
export const productsAPI = {
  // En çok stoğu olan ürünler (TopSellingCarousel için)
  async getTopStock(count = 3) {
    const endpoint = `/Products/TopStock?count=${count}`;
    return await apiCall(endpoint);
  },

  // Tüm ürünler (sayfalama ile)
  async getAll(filters = {}) {
    const params = new URLSearchParams();

    // Filtreleri ekle
    Object.keys(filters).forEach((key) => {
      if (
        filters[key] !== undefined &&
        filters[key] !== null &&
        filters[key] !== ""
      ) {
        params.append(key, filters[key]);
      }
    });

    const queryString = params.toString();
    const endpoint = queryString
      ? `/Products/All?${queryString}`
      : "/Products/All";

    return await apiCall(endpoint);
  },

  // Tekil ürün detayı
  async getById(id) {
    return await apiCall(`/Products/${id}`);
  },

  // Yeni ürün oluştur (Admin)
  async create(productData) {
    return await apiCall("/Products", {
      method: "POST",
      body: JSON.stringify(productData),
    });
  },

  // Ürün güncelle (Admin)
  async update(id, productData) {
    return await apiCall(`/Products/${id}`, {
      method: "PUT",
      body: JSON.stringify(productData),
    });
  },

  // Ürün sil (Admin)
  async delete(id) {
    return await apiCall(`/Products/${id}`, {
      method: "DELETE",
    });
  },

  // Öne çıkan ürünler
  async getFeatured(count = 6) {
    return await apiCall(`/Products/Featured?count=${count}`);
  },

  // Kategoriye göre ürünler
  async getByCategory(category, page = 1, pageSize = 20) {
    return await apiCall(
      `/Products/ByCategory/${encodeURIComponent(
        category
      )}?page=${page}&pageSize=${pageSize}`
    );
  },

  // Ürün arama
  async search(query, page = 1, pageSize = 20) {
    return await apiCall(
      `/Products/Search?q=${encodeURIComponent(
        query
      )}&page=${page}&pageSize=${pageSize}`
    );
  },

  // Ürün istatistikleri (Admin)
  async getStats() {
    return await apiCall("/Products/Stats");
  },

  // Stok güncelleme (Admin)
  async updateStock(id, quantity) {
    return await apiCall(`/Products/${id}/Stock`, {
      method: "PUT",
      body: JSON.stringify({ quantity }),
    });
  },

  // Home sayfası için tüm veriler
  async getHomeData() {
    try {
      const [topStock, allProducts] = await Promise.all([
        this.getTopStock(3),
        this.getAll({ pageSize: 50, inStockOnly: true }),
      ]);

      return {
        topProducts: topStock,
        allProducts: allProducts,
        success: true,
      };
    } catch (error) {
      console.error("Home data fetch error:", error);
      return {
        topProducts: [],
        allProducts: [],
        success: false,
        error: error.message,
      };
    }
  },

  // Kategorilere göre gruplu ürünler
  async getGroupedByCategory() {
    try {
      const allProducts = await this.getAll({ pageSize: 1000 });

      const grouped = allProducts.reduce((acc, product) => {
        const category = product.category;
        if (!acc[category]) {
          acc[category] = [];
        }
        acc[category].push(product);
        return acc;
      }, {});

      return grouped;
    } catch (error) {
      console.error("Grouped products fetch error:", error);
      throw error;
    }
  },

  // Fiyat aralığına göre ürünler
  async getByPriceRange(minPrice, maxPrice, page = 1, pageSize = 20) {
    const filters = {
      page,
      pageSize,
      minPrice,
      maxPrice,
    };
    return await this.getAll(filters);
  },

  // Popüler ürünler (stok miktarına göre)
  async getPopular(count = 10) {
    try {
      const response = await this.getAll({
        pageSize: count,
        inStockOnly: true,
        sortBy: "stock",
        sortOrder: "desc",
      });
      return response;
    } catch (error) {
      console.error("Popular products fetch error:", error);
      throw error;
    }
  },

  // Yeni eklenen ürünler
  async getLatest(count = 10) {
    try {
      const response = await this.getAll({
        pageSize: count,
        sortBy: "created",
        sortOrder: "desc",
      });
      return response;
    } catch (error) {
      console.error("Latest products fetch error:", error);
      throw error;
    }
  },
};

// Utility fonksiyonları
export const productUtils = {
  // Ürün için placeholder resim oluştur
  getPlaceholderImage(productName, width = 300, height = 300) {
    const encodedName = encodeURIComponent(productName || "Ürün");
    return `https://via.placeholder.com/${width}x${height}/6c757d/ffffff?text=${encodedName}`;
  },

  // Fiyat formatla
  formatPrice(price, currency = "TRY") {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: currency,
    }).format(price);
  },

  // Stok durumu kontrolü
  getStockStatus(stockQuantity) {
    if (stockQuantity === 0)
      return { status: "out", text: "Tükendi", class: "danger" };
    if (stockQuantity <= 10)
      return { status: "low", text: "Az Kaldı", class: "warning" };
    return { status: "in", text: "Stokta", class: "success" };
  },

  // Ürün URL'i oluştur
  getProductUrl(productId, productName) {
    const slug = productName
      .toLowerCase()
      .replace(/[^a-z0-9]/g, "-")
      .replace(/-+/g, "-")
      .replace(/^-|-$/g, "");
    return `/product/${productId}/${slug}`;
  },

  // Resim yükleme hatası için fallback
  handleImageError(event, productName) {
    event.target.src = this.getPlaceholderImage(productName);
    event.target.onerror = null; // Sonsuz döngüyü önle
  },

  // Ürün verilerini frontend formatına çevir
  transformProductData(apiProduct) {
    return {
      id: apiProduct.id,
      name: apiProduct.name,
      description: apiProduct.description,
      price: apiProduct.price,
      sold: apiProduct.stockQuantity, // API'deki stockQuantity'yi frontend'deki sold'a çevir
      image: apiProduct.imageUrl || this.getPlaceholderImage(apiProduct.name),
      category: apiProduct.category,
      stockStatus: this.getStockStatus(apiProduct.stockQuantity),
      isInStock: apiProduct.stockQuantity > 0,
      createdAt: apiProduct.createdAt,
      updatedAt: apiProduct.updatedAt,
    };
  },

  // Birden fazla ürünü dönüştür
  transformProductList(apiProducts) {
    return apiProducts.map((product) => this.transformProductData(product));
  },
};
