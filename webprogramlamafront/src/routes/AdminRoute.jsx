// src/routes/AdminRoute.jsx
import { Navigate } from "react-router-dom";
import { getToken } from "../utils/tokenUtils";

const AdminRoute = ({ children }) => {
  const token = getToken();

  // Token yoksa admin giriş sayfasına yönlendir
  if (!token) {
    return <Navigate to="/admin-login" replace />;
  }

  // Token varsa, admin sayfasını göster
  return children;
};

export default AdminRoute;
