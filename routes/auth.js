const express = require('express');
const router = express.Router();
const { User } = require('../models');
const bcrypt = require('bcryptjs');
const { generateToken } = require('../utils/jwt');

router.post('/login', async (req, res) => {
  const { email, password } = req.body;

  const user = await User.findOne({ where: { email } });
  if (!user) return res.status(404).json({ error: 'Kullanıcı bulunamadı' });

  const isMatch = await bcrypt.compare(password, user.password);
  if (!isMatch) return res.status(401).json({ error: 'Şifre hatalı' });

  const token = generateToken(user);
  res.cookie('token', token, { httpOnly: true }).json({ message: 'Giriş başarılı' });
});

module.exports = router;
