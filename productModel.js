src/
├── controllers/
│   └── productController.js
├── services/
│   └── productService.js
├── models/
│   └── productModel.js
├── routes/
│   └── productRoutes.js
├── app.js

// productModel.js
class Product {
    constructor() {
        this.products = [];
        this.id = 1;
    }

    getAll() {
        return this.products;
    }

    getById(id) {
        return this.products.find(p => p.id === id);
    }

    create(productData) {
        const newProduct = { id: this.id++, ...productData };
        this.products.push(newProduct);
        return newProduct;
    }

    delete(id) {
        const index = this.products.findIndex(p => p.id === id);
        if (index !== -1) {
            return this.products.splice(index, 1)[0];
        }
        return null;
    }
}

module.exports = new Product();


// productService.js
const Product = require("../models/productModel");

class ProductService {
    getAllProducts() {
        return Product.getAll();
    }

    getProductById(id) {
        return Product.getById(id);
    }

    createProduct(productData) {
        return Product.create(productData);
    }

    deleteProduct(id) {
        return Product.delete(id);
    }
}

module.exports = new ProductService();


// productController.js
const productService = require("../services/productService");

class ProductController {
    getAll(req, res) {
        const products = productService.getAllProducts();
        res.json(products);
    }

    getById(req, res) {
        const id = parseInt(req.params.id);
        const product = productService.getProductById(id);
        if (!product) {
            return res.status(404).json({ message: "Product not found" });
        }
        res.json(product);
    }

    create(req, res) {
        const productData = req.body;
        const newProduct = productService.createProduct(productData);
        res.status(201).json(newProduct);
    }

    delete(req, res) {
        const id = parseInt(req.params.id);
        const deleted = productService.deleteProduct(id);
        if (!deleted) {
            return res.status(404).json({ message: "Product not found" });
        }
        res.json(deleted);
    }
}

module.exports = new ProductController();


// productRoutes.js
const express = require("express");
const router = express.Router();
const productController = require("../controllers/productController");

router.get("/", productController.getAll.bind(productController));
router.get("/:id", productController.getById.bind(productController));
router.post("/", productController.create.bind(productController));
router.delete("/:id", productController.delete.bind(productController));

module.exports = router;


const express = require("express");
const app = express();
const productRoutes = require("./routes/productRoutes");

app.use(express.json());
app.use("/api/products", productRoutes);

const PORT = 3000;
app.listen(PORT, () => console.log(`Server running on port ${PORT}`));
