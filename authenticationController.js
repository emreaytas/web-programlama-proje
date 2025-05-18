// JWT Token Üretimi (Login API)

// authController.js
const jwt = require('jsonwebtoken');

const SECRET_KEY = 'gizliAnahtar'; // .env dosyasına konmalı

const users = [
  { id: 1, username: 'user1', password: '1234', role: 'user' },
  { id: 2, username: 'admin1', password: 'admin123', role: 'admin' }
];

exports.login = (req, res) => {
  const { username, password } = req.body;
  const user = users.find(u => u.username === username && u.password === password);

  if (!user) return res.status(401).json({ message: 'Hatalı giriş' });

  const token = jwt.sign(
    { id: user.id, username: user.username, role: user.role },
    SECRET_KEY,
    { expiresIn: '1h' }
  );

  res.json({ token });
};




//JWT Middleware ile Kimlik Doğrulama

// middleware/authMiddleware.js
const jwt = require('jsonwebtoken');
const SECRET_KEY = 'gizliAnahtar';

exports.authenticateToken = (req, res, next) => {
  const authHeader = req.headers['authorization'];
  const token = authHeader?.split(' ')[1]; // "Bearer token"

  if (!token) return res.status(401).json({ message: 'Token gerekli' });

  jwt.verify(token, SECRET_KEY, (err, user) => {
    if (err) return res.status(403).json({ message: 'Geçersiz token' });

    req.user = user; // kullanıcının bilgileri artık req.user içinde
    next();
  });
};




//Token Gerekli Olan Korunan Endpoint

// routes/productRoutes.js
const express = require('express');
const router = express.Router();
const { authenticateToken } = require('../middleware/authMiddleware');

router.get('/products', authenticateToken, (req, res) => {
  res.json([
    { id: 1, name: 'Klavye', price: 200 },
    { id: 2, name: 'Mouse', price: 150 }
  ]);
});

module.exports = router;



// app.js / server.js dosyasına ekle

const express = require('express');
const bodyParser = require('body-parser');
const { login } = require('./controllers/authController');
const productRoutes = require('./routes/productRoutes');

const app = express();
app.use(bodyParser.json());

app.post('/login', login);
app.use('/api', productRoutes);

app.listen(3000, () => {
  console.log('Server çalışıyor: http://localhost:3000');
});
