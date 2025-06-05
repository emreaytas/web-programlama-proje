import { Link, useLocation } from "react-router-dom";

const Navbar = () => {
  const location = useLocation();

  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-light px-3">
      <Link className="navbar-brand" to="/">
        Mağaza
      </Link>
      <div className="collapse navbar-collapse">
        <ul className="navbar-nav ms-auto">
          {/* Yardım linki her zaman gösterilir */}
          <li className="nav-item">
            <Link className="nav-link" to="/help">
              Yardım
            </Link>
          </li>

          {/* Sepet linki sadece /cart sayfasında değilsek gösterilir */}
          {location.pathname !== "/cart" && (
            <li className="nav-item">
              <Link className="nav-link" to="/cart">
                Sepet
              </Link>
            </li>
          )}
        </ul>
      </div>
    </nav>
  );
};

export default Navbar;
