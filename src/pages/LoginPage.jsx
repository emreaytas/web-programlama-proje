import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { generateFakeToken, saveToken } from "../utils/tokenUtils";

const LoginPage = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();

  const handleLogin = (e) => {
    e.preventDefault();

    // Basit kontrol (gerçek sistemde API ile yapılır)
    if (username === "admin" && password === "1234") {
      const token = generateFakeToken(username);
      saveToken(token);
      navigate("/admin");
    } else {
      alert("Kullanıcı adı veya şifre hatalı!");
    }
  };

  return (
    <div className="container d-flex justify-content-center align-items-center vh-100">
      <div className="card p-4 shadow" style={{ maxWidth: "400px", width: "100%" }}>
        <h3 className="mb-3 text-center">Admin Girişi</h3>
        <form onSubmit={handleLogin}>
          <input
            type="text"
            className="form-control mb-3"
            placeholder="Kullanıcı Adı"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
          />
          <input
            type="password"
            className="form-control mb-3"
            placeholder="Şifre"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <button className="btn btn-primary w-100 fw-semibold">Giriş Yap</button>
        </form>
      </div>
    </div>
  );
};

export default LoginPage;
