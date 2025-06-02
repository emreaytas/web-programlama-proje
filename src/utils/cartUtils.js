import Cookies from "js-cookie";

const CART_KEY = "shopping_cart";

// Sepeti getir
export const getCart = () => {
  const cart = Cookies.get(CART_KEY);
  return cart ? JSON.parse(cart) : [];
};

// Sepete ürün ekle
export const addToCart = (product) => {
  const currentCart = getCart();
  const updatedCart = [...currentCart, product];
  Cookies.set(CART_KEY, JSON.stringify(updatedCart), { expires: 7 });
};

// Sepeti temizle
export const clearCart = () => {
  Cookies.remove(CART_KEY);
};
