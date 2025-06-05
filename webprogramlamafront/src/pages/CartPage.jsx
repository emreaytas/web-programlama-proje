import { getCart } from "../utils/cartUtils";

const CartPage = () => {
  const cart = getCart();

  return (
    <div className="container py-5">
      <h2 className="fw-bold mb-4">Sepetiniz</h2>
      {cart.length === 0 ? (
        <p>Sepetiniz boş.</p>
      ) : (
        <ul className="list-group">
          {cart.map((item, index) => (
            <li key={index} className="list-group-item">
              <strong>{item.name}</strong> - {item.price} ₺
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default CartPage;
