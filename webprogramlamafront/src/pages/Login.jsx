import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { saveToken, saveUser, debugAuth } from "../utils/tokenUtils";

const Login = () => {
  const [isRegister, setIsRegister] = useState(false);
  const [formData, setFormData] = useState({
    username: "",
    email: "",
    password: "",
    confirmPassword: "",
  });
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  // Backend API URL - Düzeltildi (fazla : kaldırıldı)
  const API_BASE_URL = "https://localhost:7062/api";

  const handleChange = (e) => {
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      if (isRegister) {
        // Kayıt işlemi
        if (formData.password !== formData.confirmPassword) {
          alert("Şifreler eşleşmiyor!");
          setIsLoading(false);
          return;
        }

        const registerData = {
          username: formData.username,
          email: formData.email,
          password: formData.password,
          confirmPassword: formData.confirmPassword,
        };

        console.log(
          "Sending register request to:",
          `${API_BASE_URL}/Auth/Register`
        );
        console.log("Register data:", registerData);

        const res = await fetch(`${API_BASE_URL}/Auth/Register`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Accept: "application/json",
          },
          body: JSON.stringify(registerData),
        });

        console.log("Register response status:", res.status);

        if (!res.ok) {
          const errorText = await res.text();
          console.error("Register error response:", errorText);

          try {
            const errorData = JSON.parse(errorText);
            if (errorData.errors) {
              const errorMessages = Object.values(errorData.errors)
                .flat()
                .join("\n");
              alert(errorMessages);
            } else {
              alert(errorData.message || "Kayıt sırasında bir hata oluştu.");
            }
          } catch (parseError) {
            alert(`Kayıt hatası: ${res.status} ${res.statusText}`);
          }
          setIsLoading(false);
          return;
        }

        const data = await res.json();
        console.log("Register response data:", data);

        if (data.success && data.token && data.user) {
          // Token ve user'ı güvenli şekilde kaydet
          saveToken(data.token);
          saveUser(data.user);

          console.log("✅ Kayıt başarılı, authentication verileri kaydedildi");
          debugAuth();

          alert("Kayıt başarılı! Hoş geldiniz!");
          navigate("/");
        } else {
          throw new Error("Kayıt yanıtında token veya user bilgisi eksik");
        }
      } else {
        // Giriş işlemi
        const loginData = {
          email: formData.email,
          password: formData.password,
        };

        console.log("Sending login request to:", `${API_BASE_URL}/Auth/Login`);
        console.log("Login data:", { email: loginData.email, password: "***" });

        const res = await fetch(`${API_BASE_URL}/Auth/Login`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Accept: "application/json",
          },
          body: JSON.stringify(loginData),
        });

        console.log("Login response status:", res.status);

        if (!res.ok) {
          const errorText = await res.text();
          console.error("Login error response:", errorText);

          try {
            const errorData = JSON.parse(errorText);
            alert(errorData.message || "Giriş başarısız.");
          } catch (parseError) {
            alert(`Giriş hatası: ${res.status} ${res.statusText}`);
          }
          setIsLoading(false);
          return;
        }

        const data = await res.json();
        console.log("Login response data:", data);

        if (data.success && data.token && data.user) {
          // Token ve user'ı güvenli şekilde kaydet
          saveToken(data.token);
          saveUser(data.user);

          console.log("✅ Giriş başarılı, authentication verileri kaydedildi");
          console.log("🔐 Kaydedilen user:", data.user);
          console.log("🎫 Kaydedilen token uzunluğu:", data.token.length);

          // Debug authentication durumu
          debugAuth();

          alert(`Hoş geldiniz, ${data.user.username}!`);
          navigate("/");
        } else {
          throw new Error("Giriş yanıtında token veya user bilgisi eksik");
        }
      }
    } catch (error) {
      console.error("API Error:", error);
      console.error("Error details:", {
        name: error.name,
        message: error.message,
        stack: error.stack,
      });

      // Hata türüne göre özel mesajlar
      if (
        error.name === "TypeError" &&
        error.message.includes("Failed to fetch")
      ) {
        alert(
          "Sunucuya bağlanılamıyor. Backend sunucusunun çalıştığından emin olun.\n\nKontrol edilecekler:\n1. Backend projesinin çalışıyor olması\n2. URL'nin doğru olması (https://localhost:7062)\n3. CORS ayarlarının yapılmış olması"
        );
      } else if (
        error.name === "TypeError" &&
        error.message.includes("NetworkError")
      ) {
        alert("Ağ bağlantı hatası. İnternet bağlantınızı kontrol edin.");
      } else {
        alert(
          "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.\n\nHata: " +
            error.message
        );
      }
    } finally {
      setIsLoading(false);
    }
  };

  // Test hesapları ile hızlı giriş
  const quickLogin = async (email, password) => {
    setIsLoading(true);
    setFormData({ ...formData, email, password });

    try {
      console.log("Quick login attempt:", email);

      const res = await fetch(`${API_BASE_URL}/Auth/Login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({ email, password }),
      });

      console.log("Quick login response status:", res.status);

      if (!res.ok) {
        const errorText = await res.text();
        console.error("Quick login error:", errorText);
        alert("Hızlı giriş başarısız. Test hesabı mevcut olmayabilir.");
        setIsLoading(false);
        return;
      }

      const data = await res.json();
      console.log("Quick login response:", data);

      if (data.success && data.token && data.user) {
        // Token ve user'ı güvenli şekilde kaydet
        saveToken(data.token);
        saveUser(data.user);

        console.log("✅ Hızlı giriş başarılı");
        debugAuth();

        alert(`Hoş geldiniz, ${data.user.username}!`);
        navigate("/");
      } else {
        throw new Error("Hızlı giriş yanıtında token veya user bilgisi eksik");
      }
    } catch (error) {
      console.error("Quick Login Error:", error);
      alert("Hızlı giriş sırasında bağlantı hatası oluştu.");
    } finally {
      setIsLoading(false);
    }
  };

  // Test bağlantısı için debug fonksiyonu
  const testConnection = async () => {
    try {
      console.log("Testing connection to:", API_BASE_URL);
      const res = await fetch(`${API_BASE_URL}/Auth/Me`, {
        method: "GET",
        headers: {
          Accept: "application/json",
        },
      });

      console.log("Test connection response:", res.status, res.statusText);

      if (res.status === 401) {
        alert(
          "✅ Backend bağlantısı başarılı! (401 - Unauthorized beklenen durum)"
        );
      } else {
        alert(`Backend yanıt verdi: ${res.status} ${res.statusText}`);
      }
    } catch (error) {
      console.error("Connection test failed:", error);
      alert(
        "❌ Backend bağlantısı başarısız!\n\nKontrol edin:\n1. Backend projesi çalışıyor mu?\n2. Port numarası doğru mu? (7062)\n3. HTTPS sertifika sorunu var mı?"
      );
    }
  };

  const containerStyle = {
    maxWidth: "400px",
    margin: "50px auto",
    padding: "20px",
    border: "1px solid #ccc",
    borderRadius: "8px",
    fontFamily: "Arial, sans-serif",
    backgroundColor: "#f9f9f9",
    boxShadow: "0 2px 10px rgba(0,0,0,0.1)",
  };

  const inputStyle = {
    width: "100%",
    padding: "10px",
    marginBottom: "10px",
    borderRadius: "4px",
    border: "1px solid #aaa",
    boxSizing: "border-box",
    fontSize: "14px",
  };

  const buttonStyle = {
    width: "100%",
    padding: "10px",
    backgroundColor: isLoading ? "#ccc" : "#007BFF",
    color: "white",
    border: "none",
    borderRadius: "4px",
    cursor: isLoading ? "not-allowed" : "pointer",
    marginBottom: "10px",
    fontSize: "16px",
    fontWeight: "500",
  };

  const toggleStyle = {
    marginTop: "10px",
    textAlign: "center",
    cursor: "pointer",
    color: "#007BFF",
    textDecoration: "underline",
    fontSize: "14px",
  };

  const testAccountsStyle = {
    marginTop: "20px",
    padding: "15px",
    backgroundColor: "#e3f2fd",
    borderRadius: "4px",
    fontSize: "12px",
    border: "1px solid #bbdefb",
  };

  const quickLoginButtonStyle = {
    padding: "8px 12px",
    margin: "5px",
    backgroundColor: "#28a745",
    color: "white",
    border: "none",
    borderRadius: "4px",
    cursor: "pointer",
    fontSize: "12px",
    fontWeight: "500",
    transition: "background-color 0.2s",
  };

  const debugButtonStyle = {
    padding: "5px 10px",
    backgroundColor: "#17a2b8",
    color: "white",
    border: "none",
    borderRadius: "3px",
    cursor: "pointer",
    fontSize: "11px",
    marginTop: "10px",
    width: "100%",
  };

  return (
    <div style={containerStyle}>
      <h2 style={{ textAlign: "center", marginBottom: "20px", color: "#333" }}>
        {isRegister ? "Kayıt Ol" : "Giriş Yap"}
      </h2>

      <form onSubmit={handleSubmit}>
        {isRegister && (
          <input
            style={inputStyle}
            type="text"
            name="username"
            placeholder="Kullanıcı Adı"
            value={formData.username}
            onChange={handleChange}
            required
            minLength="3"
          />
        )}

        <input
          style={inputStyle}
          type="email"
          name="email"
          placeholder="Email"
          value={formData.email}
          onChange={handleChange}
          required
        />

        <input
          style={inputStyle}
          type="password"
          name="password"
          placeholder="Şifre"
          value={formData.password}
          onChange={handleChange}
          required
          minLength="6"
        />

        {isRegister && (
          <input
            style={inputStyle}
            type="password"
            name="confirmPassword"
            placeholder="Şifre Tekrar"
            value={formData.confirmPassword}
            onChange={handleChange}
            required
            minLength="6"
          />
        )}

        <button style={buttonStyle} type="submit" disabled={isLoading}>
          {isLoading
            ? isRegister
              ? "Kayıt Olunuyor..."
              : "Giriş Yapılıyor..."
            : isRegister
            ? "Kayıt Ol"
            : "Giriş Yap"}
        </button>
      </form>

      {/* Debug butonu */}
      <button style={debugButtonStyle} onClick={testConnection} type="button">
        🔧 Backend Bağlantısını Test Et
      </button>

      <div
        style={toggleStyle}
        onClick={() => {
          setIsRegister(!isRegister);
          setFormData({
            username: "",
            email: "",
            password: "",
            confirmPassword: "",
          });
        }}
      >
        {isRegister
          ? "Zaten hesabınız var mı? Giriş Yap"
          : "Hesabınız yok mu? Kayıt Ol"}
      </div>

      {/* Test Hesapları - Sadece giriş sayfasında göster */}
      {!isRegister && (
        <div style={testAccountsStyle}>
          <h4
            style={{ margin: "0 0 15px 0", color: "#1976d2", fontSize: "14px" }}
          >
            🧪 Test Hesapları:
          </h4>

          <div style={{ marginBottom: "10px" }}>
            <div
              style={{
                marginBottom: "5px",
                fontSize: "13px",
                fontWeight: "bold",
              }}
            >
              👨‍💼 Admin Hesabı:
            </div>
            <div
              style={{ fontSize: "11px", color: "#666", marginBottom: "5px" }}
            >
              Email: admin@example.com | Şifre: Admin123!
            </div>
            <button
              type="button"
              style={quickLoginButtonStyle}
              onClick={() => quickLogin("admin@example.com", "Admin123!")}
              disabled={isLoading}
              onMouseOver={(e) => (e.target.style.backgroundColor = "#218838")}
              onMouseOut={(e) => (e.target.style.backgroundColor = "#28a745")}
            >
              👨‍💼 Admin Girişi
            </button>
          </div>

          <div>
            <div
              style={{
                marginBottom: "5px",
                fontSize: "13px",
                fontWeight: "bold",
              }}
            >
              👤 Kullanıcı Hesabı:
            </div>
            <div
              style={{ fontSize: "11px", color: "#666", marginBottom: "5px" }}
            >
              Email: user@test.com | Şifre: Test123!
            </div>
            <button
              type="button"
              style={quickLoginButtonStyle}
              onClick={() => quickLogin("user@test.com", "Test123!")}
              disabled={isLoading}
              onMouseOver={(e) => (e.target.style.backgroundColor = "#218838")}
              onMouseOut={(e) => (e.target.style.backgroundColor = "#28a745")}
            >
              👤 User Girişi
            </button>
          </div>

          <div
            style={{
              marginTop: "10px",
              fontSize: "10px",
              color: "#666",
              fontStyle: "italic",
              textAlign: "center",
            }}
          >
            ℹ️ Bu hesaplar test amaçlıdır. Gerçek projede kaldırılmalıdır.
          </div>
        </div>
      )}

      {/* Authentication Debug Butonu */}
      <button
        style={{
          ...debugButtonStyle,
          backgroundColor: "#6f42c1",
          marginTop: "5px",
        }}
        onClick={() => {
          debugAuth();
          alert("Authentication durumu console'da görüntülendi");
        }}
        type="button"
      >
        🔍 Authentication Durumunu Kontrol Et
      </button>
    </div>
  );
};

export default Login;
