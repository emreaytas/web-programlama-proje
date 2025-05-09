import sampleProducts from "../data/sampleProducts";
import { Link } from "react-router-dom";

const TopSellingCarousel = ({ products }) => {
  return (
    <div
      id="topSellingCarousel"
      className="carousel slide carousel-fade mb-5"
      data-bs-ride="carousel"
    >
      <div className="carousel-inner rounded shadow-lg">
        {products.map((product, index) => (
          <div
            key={product.id}
            className={`carousel-item ${index === 0 ? "active" : ""}`}
          >
            <img
              src={product.image}
              className="d-block w-100"
              alt={product.name}
              style={{
                height: "650px",
                objectFit: "cover",
                filter: "brightness(70%)",
              }}
            />
            <div className="carousel-caption d-none d-md-block">
              <h2 className="fw-bold display-5">{product.name}</h2>
              <p className="fs-5">{product.description}</p>
              <div
                className="d-flex align-items-center justify-content-center mt-3"
                style={{ gap: "12px" }}
              >
                <h3 className="fw-bold text-primary mb-0">{product.price} ₺</h3>
                <Link
                  to={`/product/${product.id}`}
                  className="btn btn-primary fw-semibold shadow"
                  style={{ whiteSpace: "nowrap", padding: "6px 16px" }}
                >
                  Ürünü İncele
                </Link>
              </div>
            </div>
          </div>
        ))}
      </div>

      <button
        className="carousel-control-prev"
        type="button"
        data-bs-target="#topSellingCarousel"
        data-bs-slide="prev"
      >
        <span className="carousel-control-prev-icon" />
        <span className="visually-hidden">Previous</span>
      </button>
      <button
        className="carousel-control-next"
        type="button"
        data-bs-target="#topSellingCarousel"
        data-bs-slide="next"
      >
        <span className="carousel-control-next-icon" />
        <span className="visually-hidden">Next</span>
      </button>
    </div>
  );
};

export default TopSellingCarousel;
