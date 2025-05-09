import { Link } from 'react-router-dom';

const Navbar = () => {
  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-dark shadow sticky-top">
      <div className="container">
        <Link className="navbar-brand fw-bold" to="/">🛍️ SmartShop</Link>
        <div className="collapse navbar-collapse justify-content-end">
          <ul className="navbar-nav">
            <li className="nav-item">
              <Link className="nav-link" to="/cart">🛒 Sepetim</Link>
            </li>
            <li className="nav-item">
              <Link className="nav-link" to="/admin">⚙️ Admin</Link>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
