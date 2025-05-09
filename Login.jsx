//TERMİNALE BUNLARI YAZ!!!
//npm create vite@latest e-ticaret-frontend --template react
//cd e-ticaret-frontend
//npm install
//npm install axios

import { useState } from 'react';
import axios from 'axios';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleLogin = async () => {
    try {
      const response = await axios.post('http://localhost:5000/api/login', {
        email, password
      }, { withCredentials: true });

      alert(response.data.message);
    } catch (err) {
      alert('Giriş başarısız');
    }
  };

  return (
    <div>
      <h2>Giriş Yap</h2>
      <input value={email} onChange={e => setEmail(e.target.value)} placeholder="E-posta" />
      <input type="password" value={password} onChange={e => setPassword(e.target.value)} placeholder="Şifre" />
      <button onClick={handleLogin}>Giriş</button>
    </div>
  );
}
