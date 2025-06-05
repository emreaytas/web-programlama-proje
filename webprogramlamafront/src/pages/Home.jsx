// src/pages/Home.jsx
import { useState, useEffect } from "react";
import ProductCard from "../components/ProductCard";
import TopSellingCarousel from "../components/TopSellingCarousel";

const Home = () => {
  const [topProducts, setTopProducts] = useState([]);
  const [allProducts, setAllProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const API_BASE_URL = "https://localhost:7062/api";

  // Sayfa yüklendiğinde verileri getir
  useEffect(() => {
    fetchHomeData();
  }, []);

  const fetchHomeData = async () => {
    setLoading(true);
    setError(null);

    try {
      // Paralel olarak iki API çağrısı yap
      const [topStockResponse, allProductsResponse] = await Promise.all([
        fetch(`${API_BASE_URL}/Products/TopStock?count=3`, {
          headers: {
            Accept: "application/json",
          },
        }),
        fetch(`${API_BASE_URL}/Products/All?pageSize=50&inStockOnly=true`, {
          headers: {
            Accept: "application/json",
          },
        }),
      ]);

      // Hata kontrolü
      if (!topStockResponse.ok) {
        throw new Error(`Top products error: ${topStockResponse.status}`);
      }
      if (!allProductsResponse.ok) {
        throw new Error(`All products error: ${allProductsResponse.status}`);
      }

      // JSON verilerini parse et
      const topStockData = await topStockResponse.json();
      const allProductsData = await allProductsResponse.json();

      // Verileri dönüştür (API'den gelen format ile frontend'in beklediği format uyumlu hale getir)
      const formattedTopProducts = topStockData.map((product) => ({
        id: product.id,
        name: product.name,
        description: product.description,
        price: product.price,
        sold: product.stockQuantity, // Stok miktarını "sold" olarak kullan
        image:
          product.imageUrl ||
          "https://via.placeholder.com/300x300/6c757d/ffffff?text=Ürün+Resmi",
        category: product.category,
      }));

      const formattedAllProducts = allProductsData.map((product) => ({
        id: product.id,
        name: product.name,
        description: product.description,
        price: product.price,
        sold: product.stockQuantity,
        image:
          product.imageUrl ||
          "https://via.placeholder.com/300x300/6c757d/ffffff?text=Ürün+Resmi",
        category: product.category,
        stockStatus: product.stockStatus,
        isInStock: product.isInStock,
      }));

      setTopProducts(formattedTopProducts);
      setAllProducts(formattedAllProducts);
    } catch (error) {
      console.error("Veri getirme hatası:", error);
      setError(error.message);

      // Fallback: Hata durumunda sample data kullan
      const { default: sampleProducts } = await import(
        "../data/sampleProducts"
      );
      const topSampleProducts = [...sampleProducts]
        .sort((a, b) => b.sold - a.sold)
        .slice(0, 3);

      setTopProducts(topSampleProducts);
      setAllProducts(sampleProducts);
    } finally {
      setLoading(false);
    }
  };

  // Loading durumu
  if (loading) {
    return (
      <div className="container-fluid p-0">
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ height: "50vh" }}
        >
          <div className="text-center">
            <div
              className="spinner-border text-primary"
              role="status"
              style={{ width: "3rem", height: "3rem" }}
            >
              <span className="visually-hidden">Yükleniyor...</span>
            </div>
            <p className="mt-3 text-muted fs-5">Ürünler yükleniyor...</p>
          </div>
        </div>
      </div>
    );
  }

  // Hata durumu
  if (error && topProducts.length === 0) {
    return (
      <div className="container py-5">
        <div className="alert alert-warning text-center" role="alert">
          <h4 className="alert-heading">Bağlantı Sorunu</h4>
          <p>Ürünler yüklenirken bir sorun oluştu: {error}</p>
          <hr />
          <button className="btn btn-primary" onClick={fetchHomeData}>
            Tekrar Dene
          </button>
        </div>
      </div>
    );
  }

  // Top products'tan diğer ürünleri çıkar
  const otherProducts = allProducts.filter(
    (product) => !topProducts.some((top) => top.id === product.id)
  );

  return (
    <div className="container-fluid p-0">
      {/* Hata varsa uyarı göster ama sayfayı render et */}
      {error && (
        <div className="container mt-3">
          <div
            className="alert alert-info alert-dismissible fade show"
            role="alert"
          >
            <i className="bi bi-info-circle me-2"></i>
            Veriler önbellekten yüklendi. Güncel veriler için sayfayı yenileyin.
            <button
              type="button"
              className="btn-close"
              data-bs-dismiss="alert"
            ></button>
          </div>
        </div>
      )}

      {/* En Çok Stoğu Olan Ürünler Carousel */}
      {topProducts.length > 0 && <TopSellingCarousel products={topProducts} />}

      <div className="container py-5">
        {/* Başlık ve İstatistikler */}
        <div className="row mb-4">
          <div className="col-12">
            <h2 className="fw-bold text-center mb-3">Tüm Ürünlerimiz</h2>
            <div className="text-center mb-4">
              <span className="badge bg-primary fs-6 me-2">
                Toplam: {allProducts.length} ürün
              </span>
              <span className="badge bg-success fs-6 me-2">
                Stokta: {allProducts.filter((p) => p.isInStock).length}
              </span>
              <span className="badge bg-warning fs-6">
                Kategori: {new Set(allProducts.map((p) => p.category)).size}
              </span>
            </div>
          </div>
        </div>

        {/* Öne Çıkan Ürünler */}
        {topProducts.length > 0 && (
          <>
            <h3 className="fw-bold mb-4 text-primary">
              <i className="bi bi-star-fill me-2"></i>
              En Çok Stokta Olan Ürünler
            </h3>
            <div className="row mb-5">
              {topProducts.map((product) => (
                <ProductCard
                  key={`top-${product.id}`}
                  product={product}
                  showAddToCart={true}
                />
              ))}
            </div>
          </>
        )}

        {/* Diğer Ürünler */}
        {otherProducts.length > 0 && (
          <>
            <h3 className="fw-bold mb-4 text-secondary">
              <i className="bi bi-grid-3x3-gap-fill me-2"></i>
              Diğer Ürünler
            </h3>
            <div className="row">
              {otherProducts.map((product) => (
                <ProductCard
                  key={`other-${product.id}`}
                  product={product}
                  showAddToCart={true}
                />
              ))}
            </div>
          </>
        )}

        {/* Ürün bulunamadı durumu */}
        {allProducts.length === 0 && !loading && (
          <div className="text-center py-5">
            <i className="bi bi-inbox display-1 text-muted"></i>
            <h4 className="text-muted mt-3">Henüz Ürün Eklenmemiş</h4>
            <p className="text-muted">
              Mağazamıza yakında ürünler eklenecek. Lütfen daha sonra tekrar
              kontrol edin.
            </p>
            <button className="btn btn-primary" onClick={fetchHomeData}>
              <i className="bi bi-arrow-clockwise me-2"></i>
              Yenile
            </button>
          </div>
        )}

        {/* Kategoriler */}
        {allProducts.length > 0 && (
          <div className="row mt-5">
            <div className="col-12">
              <h4 className="fw-bold mb-3">
                <i className="bi bi-tags-fill me-2"></i>
                Kategorilerimiz
              </h4>
              <div className="d-flex flex-wrap gap-2">
                {[...new Set(allProducts.map((p) => p.category))].map(
                  (category) => {
                    const categoryCount = allProducts.filter(
                      (p) => p.category === category
                    ).length;
                    return (
                      <span
                        key={category}
                        className="badge bg-outline-primary border border-primary text-primary px-3 py-2 fs-6"
                        style={{ cursor: "pointer" }}
                        onClick={() => {
                          // Kategoriye göre filtreleme özelliği eklenebilir
                          console.log(`${category} kategorisi seçildi`);
                        }}
                      >
                        {category} ({categoryCount})
                      </span>
                    );
                  }
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Home;
