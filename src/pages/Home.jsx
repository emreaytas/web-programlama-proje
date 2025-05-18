import sampleProducts from '../data/sampleProducts';
import ProductCard from '../components/ProductCard';
import TopSellingCarousel from "../components/TopSellingCarousel";

const Home = () => {
  // En çok satan ilk 3 ürünü ayır
  const topProducts = [...sampleProducts]
    .sort((a, b) => b.sold - a.sold)
    .slice(0, 3);

  // Diğer ürünleri filtrele
  const otherProducts = sampleProducts.filter(
    (product) => !topProducts.some(top => top.id === product.id)
  );

  return (
    <div className="container-fluid p-0">
      {/* En çok satanlar slider'a props olarak veriliyor */}
      <TopSellingCarousel products={topProducts} />

      <div className="container py-5">
        <h2 className="fw-bold text-center mb-4">Tüm Ürünler</h2>
        <div className="row">
          {otherProducts.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      </div>
    </div>
  );
};

export default Home;
