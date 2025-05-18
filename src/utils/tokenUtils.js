const TOKEN_KEY = "auth_token";

// Token oluştur (örnek: sahte token)
export const generateFakeToken = (username) => {
  return `FAKE-TOKEN-${username}-${Date.now()}`;
};

// Token'ı kaydet
export const saveToken = (token) => {
  localStorage.setItem(TOKEN_KEY, token);
};

// Token'ı getir
export const getToken = () => {
  return localStorage.getItem(TOKEN_KEY);
};

// Token'ı sil
export const clearToken = () => {
  localStorage.removeItem(TOKEN_KEY);
};
