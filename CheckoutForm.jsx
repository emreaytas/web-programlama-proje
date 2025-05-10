//components/CheckoutForm.jsx
import React, { useState } from "react";
import axios from "axios";
import { toast } from "react-toastify";

const CheckoutForm = () => {
  const [amount, setAmount] = useState(100); // örnek miktar

  const handlePayment = async () => {
    try {
      // Mock ödeme endpoint'i (örneğin: http://localhost:3000/api/checkout)
      const response = await axios.post("/api/checkout", { amount });
      if (response.status === 200) {
        toast.success("Ödeme başarılı!");
      } else {
        toast.error("Ödeme başarısız.");
      }
    } catch (error) {
      toast.error("Bir hata oluştu: " + error.message);
    }
  };

  return (
    <div>
      <h2>Mock Ödeme</h2>
      <p>Tutar: {amount} TL</p>
      <button onClick={handlePayment}>Öde</button>
    </div>
  );
};

export default CheckoutForm;

//App.jsx
import React from "react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import CheckoutForm from "./components/CheckoutForm";

function App() {
  return (
    <div className="App">
      <CheckoutForm />
      <ToastContainer position="top-right" autoClose={3000} />
    </div>
  );
}

export default App;


//routes/checkout.js
const express = require("express");
const router = express.Router();

router.post("/", (req, res) => {
  const { amount } = req.body;

  if (amount > 0) {
    console.log(`Mock ödeme işlemi başarıyla alındı: ${amount} TL`);
    res.status(200).json({ message: "Ödeme başarılı (mock)" });
  } else {
    res.status(400).json({ message: "Geçersiz tutar" });
  }
});

module.exports = router;

//app.js
const express = require("express");
const app = express();
const checkoutRoutes = require("./routes/checkout");

app.use(express.json());
app.use("/api/checkout", checkoutRoutes);

const PORT = 3000;
app.listen(PORT, () => console.log(`Server running on port ${PORT}`));

// fronted:npm install axios react-toastify
//backend:npm install express
