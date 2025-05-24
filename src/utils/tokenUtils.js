const TOKEN_KEY = "auth_token";

/**
 * Sahte bir token oluşturur.
 * Gerçek uygulamalarda backend'den alınmalıdır.
 * @param {string} username
 * @returns {string}
 */
export const generateFakeToken = (username) => {
  const payload = {
    user: username,
    timestamp: new Date().toISOString()
  };
  return btoa(JSON.stringify(payload)); // base64 ile encode edilmiş sahte JWT
};

/**
 * Token'ı localStorage'a kaydeder.
 * @param {string} token
 */
export const saveToken = (token) => {
  if (typeof token === "string") {
    localStorage.setItem(TOKEN_KEY, token);
  }
};

/**
 * localStorage'dan token'ı alır.
 * @returns {string|null}
 */
export const getToken = () => {
  return localStorage.getItem(TOKEN_KEY);
};

/**
 * Token'ı siler.
 */
export const clearToken = () => {
  localStorage.removeItem(TOKEN_KEY);
};

/**
 * Token var mı kontrolü yapar.
 * @returns {boolean}
 */
export const isAuthenticated = () => {
  return Boolean(getToken());
};
