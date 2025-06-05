import { useState, useEffect } from "react";
import sampleProducts from "../data/sampleProducts";
import { useNavigate } from "react-router-dom";
import { getToken } from "../utils/tokenUtils";

const AdminPage = () => {
  const [products, setProducts] = useState(sampleProducts);
  const [newProduct, setNewProduct] = useState({
    name: "",
    description: "",
    price: "",
    sold: 0,
    image: "",
  });

  const navigate = useNavigate();

  // Giriş kontrolü: token yoksa admin login sayfasına yönlendir
  useEffect(() => {
    const token = getToken();
    if (!token) {
      navigate("/admin-login");
    }
  }, [navigate]);

  const handleAddProduct = () => {
    const newItem = {
      ...newProduct,
      id: products.length + 1,
      price: parseFloat(newProduct.price),
      sold: parseInt(newProduct.sold),
    };
    setProducts([newItem, ...products]);
    setNewProduct({ name: "", description: "", price: "", sold: 0, image: "" });
  };

  const handleDelete = (id) => {
    setProducts(products.filter((p) => p.id !== id));
  };

  return (
    <div className="container py-5">
      <h1 className="fw-bold text-center mb-4">Admin Paneli</h1>

      {/* Ürün Ekleme Formu */}
      <div className="card p-4 mb-5 shadow-sm">
        <h4 className="mb-3">Yeni Ürün Ekle</h4>
        <div className="row g-2">
          {["name", "description", "price", "sold", "image"].map((field) => (
            <div className="col-md-4" key={field}>
              <input
                type={field === "price" || field === "sold" ? "number" : "text"}
                className="form-control"
                placeholder={field.charAt(0).toUpperCase() + field.slice(1)}
                value={newProduct[field]}
                onChange={(e) =>
                  setNewProduct({ ...newProduct, [field]: e.target.value })
                }
              />
            </div>
          ))}
          <div className="col-12 text-end">
            <button className="btn btn-success mt-2" onClick={handleAddProduct}>
              Ürün Ekle
            </button>
          </div>
        </div>
      </div>

      {/* Ürün Listesi */}
      <h4 className="fw-bold mb-3">Tüm Ürünler</h4>
      <table className="table table-bordered table-striped">
        <thead>
          <tr>
            <th>ID</th>
            <th>İsim</th>
            <th>Açıklama</th>
            <th>Fiyat</th>
            <th>Satış</th>
            <th>İşlemler</th>
          </tr>
        </thead>
        <tbody>
          {products.map((p) => (
            <tr key={p.id}>
              <td>{p.id}</td>
              <td>{p.name}</td>
              <td>{p.description}</td>
              <td>{p.price} ₺</td>
              <td>{p.sold}</td>
              <td>
                <button
                  className="btn btn-sm btn-danger"
                  onClick={() => handleDelete(p.id)}
                >
                  Sil
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default AdminPage;
