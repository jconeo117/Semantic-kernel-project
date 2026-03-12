import React, { createContext, useContext, useState, useEffect } from 'react';
import axios from 'axios';
import { jwtDecode } from 'jwt-decode';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }) => {
  const [token, setToken] = useState(localStorage.getItem('dashboard_token'));
  const [tenant, setTenant] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (token) {
      try {
        const decoded = jwtDecode(token);
        // Verify expiration
        if (decoded.exp * 1000 < Date.now()) {
          logout();
        } else {
          setTenant({
            id: decoded.tenant_id || decoded.nameid,
            name: decoded.BusinessName || decoded.unique_name || 'Admin',
          });
          axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
          axios.defaults.headers.common['X-Tenant-Id'] = decoded.tenant_id || decoded.nameid;
        }
      } catch (e) {
        logout();
      }
    }
    setIsLoading(false);
  }, [token]);

  const login = async (username, password) => {
    try {
      const response = await axios.post('/api/tenant/auth/login', { username, password });
      const newToken = response.data.token;
      setToken(newToken);
      localStorage.setItem('dashboard_token', newToken);
      axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
      axios.defaults.headers.common['X-Tenant-Id'] = response.data.tenantId;
      return { success: true };
    } catch (error) {
      console.error('Login error:', error);
      
      let errorMsg = 'Error al iniciar sesión. Verifique sus credenciales.';
      if (error.response?.data) {
          if (typeof error.response.data === 'string') {
              errorMsg = error.response.data;
          } else if (error.response.data.error) {
              errorMsg = error.response.data.error;
          } else if (error.response.data.title) {
              errorMsg = error.response.data.title;
          }
      }

      return { 
        success: false, 
        error: errorMsg
      };
    }
  };

  const logout = () => {
    setToken(null);
    setTenant(null);
    localStorage.removeItem('dashboard_token');
    delete axios.defaults.headers.common['Authorization'];
  };

  return (
    <AuthContext.Provider value={{ token, tenant, login, logout, isLoading }}>
      {!isLoading && children}
    </AuthContext.Provider>
  );
};
