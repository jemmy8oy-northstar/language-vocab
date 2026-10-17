import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css'
import Navbar from './components/Navbar'
import Practice from './pages/Practice'
import Learn from './pages/Learn'
import Packs from './pages/Packs'
import Stats from './pages/Stats'
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
              <Route path="/stats" element={<Stats />} />
            </Routes>
          </main>
        </div>
      </Router>
    </ThemeProvider>
  )
}

export default App
