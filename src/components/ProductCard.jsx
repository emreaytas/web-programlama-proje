import { Link } from "react-router-dom";

const ProductCard = ({ product }) => {
  return (
    <div className="col-md-4 mb-4">
      <div className="card shadow-sm border-0 h-100 d-flex flex-column" style={{ minHeight: '460px' }}>
        <img
          src={product.image}
          alt={product.name}
          className="card-img-top"
          style={{ height: "280px", objectFit: "cover" }}
        />
        <div className="card-body d-flex flex-column justify-content-between">
          <div>
            <h5 className="card-title fw-semibold">{product.name}</h5>
            <p className="card-text text-muted" style={{ fontSize: "0.9rem" }}>
              {product.description}
            </p>
          </div>
          <div>
            <div className="d-flex justify-content-between align-items-center mb-2">
              <span className="fw-bold text-primary">{product.price} ₺</span>
            </div>
            <Link
              to={`/product/${product.id}`}
              className="btn btn-primary w-100 fw-bold shadow-sm"
            >
              Ürünü İncele
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProductCard;
