import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css'
import Navbar from './components/Navbar'
import Practice from './pages/Practice'
import Learn from './pages/Learn'
import Packs from './pages/Packs'
import ScrollToTop from './components/ScrollToTop'
import { ThemeProvider } from './context/ThemeContext'

function App() {
  return (
    <ThemeProvider>
      <Router basename={import.meta.env.BASE_URL}>
        <ScrollToTop />
        <div className="app-container">
          <Navbar />
          <main>
            <Routes>
              <Route path="/" element={<Practice />} />
              <Route path="/learn" element={<Learn />} />
              <Route path="/packs" element={<Packs />} />
            </Routes>
          </main>
          <footer className="container" style={{
            padding: '64px 0',
            borderTop: '1px solid var(--glass-border)',
            marginTop: '120px',
            textAlign: 'center',
            color: 'var(--text-secondary)',
            fontSize: '0.9rem'
          }}>
            <p>© {new Date().getFullYear()} Your Name Here.</p>
          </footer>
        </div>
      </Router>
    </ThemeProvider>
  )
}

export default App
