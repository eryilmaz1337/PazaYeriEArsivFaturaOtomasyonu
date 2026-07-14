import React from 'react';
import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import Orders from './pages/Orders';

const navLinks = [
  { to: '/', label: 'Dashboard' },
  { to: '/orders', label: 'Tüm Siparişler' },
  { to: '/orders/trendyol', label: 'Trendyol', dot: 'bg-orange-500' },
  { to: '/orders/hepsiburada', label: 'Hepsiburada', dot: 'bg-purple-500' },
];

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-100 font-sans">
        <nav className="bg-white shadow">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between h-16">
              <div className="flex">
                <div className="flex-shrink-0 flex items-center text-xl font-bold text-indigo-600">
                  Otomasyon
                </div>
                <div className="hidden sm:ml-6 sm:flex sm:space-x-4">
                  {navLinks.map((link) => (
                    <NavLink
                      key={link.to}
                      to={link.to}
                      end={link.to === '/' || link.to === '/orders'}
                      className={({ isActive }) =>
                        `inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium transition-colors duration-200 ${
                          isActive
                            ? 'border-indigo-500 text-gray-900'
                            : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
                        }`
                      }
                    >
                      {link.dot && (
                        <span className={`w-2 h-2 rounded-full mr-1.5 ${link.dot}`}></span>
                      )}
                      {link.label}
                    </NavLink>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </nav>

        <main className="py-10">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route path="/orders" element={<Orders />} />
              <Route path="/orders/:platform" element={<Orders />} />
            </Routes>
          </div>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
