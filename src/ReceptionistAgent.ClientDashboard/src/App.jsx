import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import Login from './pages/Login';
import Inbox from './pages/Inbox';
import Bookings from './pages/Bookings';
import Providers from './pages/Providers';
import Database from './pages/Database';
import Settings from './pages/Settings';

const ProtectedRoute = ({ children }) => {
  const { token } = useAuth();
  if (!token) return <Navigate to="/login" replace />;
  return children;
};

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route 
            path="/inbox" 
            element={
              <ProtectedRoute>
                <Inbox />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/bookings" 
            element={
              <ProtectedRoute>
                <Bookings />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/providers" 
            element={
              <ProtectedRoute>
                <Providers />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/database" 
            element={
              <ProtectedRoute>
                <Database />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/settings" 
            element={
              <ProtectedRoute>
                <Settings />
              </ProtectedRoute>
            } 
          />
          {/* Default redirect to Inbox */}
          <Route path="*" element={<Navigate to="/inbox" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
