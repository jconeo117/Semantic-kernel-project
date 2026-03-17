import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { AlertCircle } from 'lucide-react';

const Login = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const result = await login(username, password);
      if (result.success) {
        navigate('/inbox');
      } else {
        setError(result.error);
      }
    } catch (err) {
      setError('Ocurrió un error inesperado. Intente de nuevo.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div id="login-page" className="fixed inset-0 z-[100] bg-[var(--bg)] flex items-center justify-center animate-fadeIn font-body">
      <div className="login-card bg-[var(--surface)] border border-[var(--border)] rounded-[20px] p-12 w-[420px] shadow-[var(--shadow-lg)] animate-slideUp">
        <div className="font-display text-[22px] font-bold tracking-[-0.5px] text-[var(--text-primary)] mb-9 flex items-center gap-2.5">
          <span className="w-2 h-2 bg-[var(--accent)] rounded-full inline-block"></span>
          Dashboard
        </div>
        
        <div className="font-display text-[26px] font-bold tracking-[-0.5px] mb-1.5">Bienvenido de nuevo</div>
        <div className="text-[var(--text-secondary)] text-[14px] mb-8">Inicia sesión en tu espacio de trabajo</div>
        
        {error && (
          <div className="bg-red-50 text-red-600 p-3 rounded-lg text-[13px] mb-6 flex items-center gap-2 border border-red-100">
            <AlertCircle size={16} />
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label className="block text-[12px] font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-[0.3px]">Usuario o Tenant ID</label>
            <input
              type="text"
              required
              className="input-field"
              placeholder="nombre@empresa.com"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
          </div>
          
          <div className="mb-4">
            <label className="block text-[12px] font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-[0.3px]">Contraseña</label>
            <input
              type="password"
              required
              className="input-field"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>
          
          <button
            type="submit"
            disabled={loading}
            className="btn-primary"
          >
            {loading ? 'Iniciando sesión...' : 'Iniciar sesión'}
          </button>
        </form>
        
        <div className="text-center text-[var(--text-muted)] text-[12px] mt-5">
          ¿Olvidaste tu contraseña? <span className="text-[var(--accent)] cursor-pointer">Recuperar</span>
        </div>
      </div>
    </div>
  );
};

export default Login;
