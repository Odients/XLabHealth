import { Link, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/store/authStore';
import { useSignalR } from '@/hooks/useSignalR';
import { useBackendHealth } from '@/hooks/useBackendHealth';
import './Header.css';

interface HeaderProps {
  isPublic: boolean;
}

const Header = ({ isPublic }: HeaderProps) => {
  const { t } = useTranslation();
  const { isAuthenticated, user, logout, checkAuth } = useAuthStore();
  const { connectionState } = useSignalR();
  const { backendAvailable } = useBackendHealth();
  const navigate = useNavigate();

  // Проверяем состояние авторизации при монтировании и при изменении
  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  // Слушаем события обновления авторизации для немедленного обновления UI
  useEffect(() => {
    const handleAuthRefresh = () => {
      checkAuth();
    };

    const handleAuthLogout = () => {
      checkAuth();
      // Если мы не на публичной странице, перенаправляем на логин
      if (!isPublic && window.location.pathname !== '/login') {
        navigate('/login', { replace: true });
      }
    };

    window.addEventListener('auth:refresh', handleAuthRefresh);
    window.addEventListener('auth:logout', handleAuthLogout);

    return () => {
      window.removeEventListener('auth:refresh', handleAuthRefresh);
      window.removeEventListener('auth:logout', handleAuthLogout);
    };
  }, [checkAuth, navigate, isPublic]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const getConnectionIndicator = () => {
    if (!isAuthenticated) return null;
    
    // Бэкенд считается подключенным, если либо SignalR подключен, либо бэкенд доступен через API
    const isConnected = connectionState === 'Connected' || backendAvailable;
    return (
      <div className="connection-indicator" title={isConnected ? 'Connected' : 'Disconnected'}>
        <span className={`connection-dot ${isConnected ? 'connected' : 'disconnected'}`} />
        <span className="connection-text">
          {isConnected ? 'Connected' : 'Disconnected'}
        </span>
      </div>
    );
  };

  return (
    <header className="header">
      <div className="header-container">
        <Link to="/" className="header-logo">
          <img src="/favicon.ico" alt="X-Lab" className="logo-icon" />
          <span className="logo-text">X-Lab Status</span>
        </Link>

        {!isPublic && isAuthenticated && (
          <nav className="header-nav">
            <Link to="/dashboard" className="nav-link">
              Dashboard
            </Link>
            <Link to="/analytics" className="nav-link">
              Аналитика
            </Link>
            <Link to="/admin" className="nav-link">
              Admin
            </Link>
          </nav>
        )}

        <div className="header-actions">
          {getConnectionIndicator()}
          {isAuthenticated ? (
            <>
              <div className="user-info">
                <span className="username">{user?.username}</span>
                {user?.role && (
                  <span className="user-role">{user.role}</span>
                )}
              </div>
              <button onClick={handleLogout} className="btn-logout">
                Выход
              </button>
            </>
          ) : (
            <Link to="/login" className="btn-login">
              {isPublic ? t('public.header.signIn') : 'Вход'}
            </Link>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header;

