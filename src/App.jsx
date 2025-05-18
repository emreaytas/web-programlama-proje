import { BrowserRouter as Router, Routes, Route, useLocation } from 'react-router-dom';
import Navbar from './components/Navbar';
import Home from './pages/Home';
import AdminPage from "./pages/AdminPage";


  return (
    <>
      {!hideNavbar && <Navbar />}
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/admin" element={<AdminPage />} />
      </Routes>
    </>
  );
}

function App() {
  return (
    <Router>
      <AppContent />

    </Router>
  );
}

export default App;
