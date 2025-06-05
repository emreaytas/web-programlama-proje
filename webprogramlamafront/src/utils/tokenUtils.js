// src/utils/tokenUtils.js - Geliştirilmiş token yönetimi
const TOKEN_KEY = "authToken";
const USER_KEY = "user";

export const generateFakeToken = (username) => {
  const payload = {
    user: username,
    timestamp: new Date().toISOString(),
    exp: new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString(), // 2 saat
  };
  return btoa(JSON.stringify(payload));
};

export const saveToken = (token) => {
  try {
    localStorage.setItem(TOKEN_KEY, token);
    console.log("✅ Token kaydedildi:", token ? "Mevcut" : "Boş");
  } catch (error) {
    console.error("❌ Token kaydedilemedi:", error);
  }
};

export const getToken = () => {
  try {
    const token = localStorage.getItem(TOKEN_KEY);
    console.log("🔍 Token alındı:", token ? "Mevcut" : "Yok");
    return token;
  } catch (error) {
    console.error("❌ Token alınamadı:", error);
    return null;
  }
};

export const saveUser = (user) => {
  try {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    console.log("✅ User kaydedildi:", user);
  } catch (error) {
    console.error("❌ User kaydedilemedi:", error);
  }
};

export const getUser = () => {
  try {
    const userStr = localStorage.getItem(USER_KEY);
    if (userStr) {
      const user = JSON.parse(userStr);
      console.log("🔍 User alındı:", user);
      return user;
    }
    console.log("🔍 User bulunamadı");
    return null;
  } catch (error) {
    console.error("❌ User parse hatası:", error);
    return null;
  }
};

export const isAuthenticated = () => {
  try {
    const token = getToken();
    const user = getUser();

    const isAuth = Boolean(token && user);

    console.log("🔐 Authentication kontrolü:", {
      token: token ? "✅ Mevcut" : "❌ Yok",
      user: user ? "✅ Mevcut" : "❌ Yok",
      result: isAuth ? "✅ Authenticated" : "❌ Not Authenticated",
    });

    return isAuth;
  } catch (error) {
    console.error("❌ Authentication kontrol hatası:", error);
    return false;
  }
};

export const clearToken = () => {
  try {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    console.log("🗑️ Token ve User temizlendi");
  } catch (error) {
    console.error("❌ Token temizlenemedi:", error);
  }
};

// JWT token'ın geçerliliğini kontrol et
export const isTokenValid = (token) => {
  if (!token) return false;

  try {
    // JWT token formatını kontrol et (3 part: header.payload.signature)
    const parts = token.split(".");
    if (parts.length !== 3) {
      console.warn("⚠️ Geçersiz JWT format");
      return false;
    }

    // Payload'ı decode et
    const payload = JSON.parse(atob(parts[1]));

    // Expiration time kontrolü
    if (payload.exp) {
      const now = Math.floor(Date.now() / 1000);
      const isExpired = payload.exp < now;

      if (isExpired) {
        console.warn("⚠️ Token süresi dolmuş");
        return false;
      }
    }

    console.log("✅ Token geçerli");
    return true;
  } catch (error) {
    console.error("❌ Token validation hatası:", error);
    return false;
  }
};

// Gelişmiş authentication kontrolü
export const isAuthenticatedAdvanced = () => {
  try {
    const token = getToken();
    const user = getUser();

    // Basic kontroller
    if (!token || !user) {
      console.log("❌ Token veya user eksik");
      return false;
    }

    // Token format kontrolü
    if (!isTokenValid(token)) {
      console.log("❌ Token geçersiz, temizleniyor...");
      clearToken();
      return false;
    }

    // User objesinin gerekli alanları var mı?
    if (!user.id || !user.email) {
      console.log("❌ User objesi eksik alanlar içeriyor");
      return false;
    }

    console.log("✅ Authentication başarılı");
    return true;
  } catch (error) {
    console.error("❌ Advanced authentication hatası:", error);
    clearToken(); // Hata durumunda temizle
    return false;
  }
};

// Token yenileme (isteğe bağlı)
export const refreshToken = async () => {
  try {
    const currentToken = getToken();
    if (!currentToken) return false;

    const response = await fetch(
      "https://localhost:7062/api/Auth/RefreshToken",
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${currentToken}`,
          "Content-Type": "application/json",
        },
      }
    );

    if (response.ok) {
      const data = await response.json();
      saveToken(data.token);
      console.log("✅ Token yenilendi");
      return true;
    } else {
      console.log("❌ Token yenilenemedi");
      clearToken();
      return false;
    }
  } catch (error) {
    console.error("❌ Token yenileme hatası:", error);
    return false;
  }
};

// Debug için authentication durumunu logla
export const debugAuth = () => {
  console.group("🔍 Authentication Debug");
  console.log("Token:", getToken());
  console.log("User:", getUser());
  console.log("Is Authenticated:", isAuthenticated());
  console.log("Is Authenticated Advanced:", isAuthenticatedAdvanced());
  console.groupEnd();
};

// Storage event listener (farklı tab'larda senkronizasyon için)
export const onAuthChange = (callback) => {
  const handleStorageChange = (e) => {
    if (e.key === TOKEN_KEY || e.key === USER_KEY) {
      callback(isAuthenticated());
    }
  };

  window.addEventListener("storage", handleStorageChange);

  return () => {
    window.removeEventListener("storage", handleStorageChange);
  };
};
