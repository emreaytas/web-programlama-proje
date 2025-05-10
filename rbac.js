const express = require('express');
const jwt = require('jsonwebtoken');
const bodyParser = require('body-parser');
const app = express();

app.use(bodyParser.json());

const users = [
  { id: 1, username: 'user1', password: '1234', role: 'user' },
  { id: 2, username: 'admin1', password: 'admin123', role: 'admin' }
];

const SECRET_KEY = 'gizliAnahtar'; // güvenlik için .env dosyasına alınmalı

app.post('/login', (req, res) => {
  const { username, password } = req.body;
  const user = users.find(u => u.username === username && u.password === password);

  if (!user) return res.status(401).json({ message: 'Invalid credentials' });

  const token = jwt.sign({ id: user.id, role: user.role }, SECRET_KEY, { expiresIn: '1h' });

  res.json({ token });
});




function authenticateToken(req, res, next) {
  const authHeader = req.headers['authorization'];
  const token = authHeader && authHeader.split(' ')[1];
  if (!token) return res.sendStatus(401);

  jwt.verify(token, SECRET_KEY, (err, user) => {
    if (err) return res.sendStatus(403);
    req.user = user;
    next();
  });
}

function authorizeRoles(...allowedRoles) {
  return (req, res, next) => {
    if (!allowedRoles.includes(req.user.role)) {
      return res.status(403).json({ message: 'Forbidden - You don\'t have permission' });
    }
    next();
  };
}





app.get('/admin/products', authenticateToken, authorizeRoles('admin'), (req, res) => {
  res.json({ message: 'Ürün yönetim paneline erişim başarılı.' });
});
