import { Routes, Route } from 'react-router-dom';
import { useEffect } from 'react';
import { useAuthStore } from './store/authStore';
import { useGTM } from './hooks/useGTM';
import PublicLayout from './components/layout/PublicLayout';
import AppLayout from './components/layout/AppLayout';
import PublicDashboard from './pages/PublicDashboard';
import LoginPage from './pages/LoginPage';
import PrivateDashboard from './pages/PrivateDashboard';
import ServiceDetailPage from './pages/ServiceDetailPage';
import AdminPage from './pages/AdminPage';
import AnalyticsPage from './pages/AnalyticsPage';
import NotFoundPage from './pages/NotFoundPage';
import PrivateRoute from './components/routing/PrivateRoute';
import AdminRoute from './components/routing/AdminRoute';

function App() {
  const { initialize, checkAuth, isAuthenticated } = useAuthStore();
  
  // Автоматически отслеживаем переходы по страницам в GTM
  useGTM();

  useEffect(() => {
    // Восстанавливаем состояние авторизации из localStorage при загрузке приложения
    initialize();
    checkAuth();
  }, [initialize, checkAuth]);

  // Периодически проверяем состояние авторизации (каждые 30 секунд)
  useEffect(() => {
    if (!isAuthenticated) return;

    const interval = setInterval(() => {
      checkAuth();
    }, 30000); // Проверяем каждые 30 секунд

    return () => clearInterval(interval);
  }, [isAuthenticated, checkAuth]);

  return (
    <Routes>
      {/* Public routes */}
      <Route element={<PublicLayout />}>
        <Route path="/" element={<PublicDashboard />} />
        <Route path="/login" element={<LoginPage />} />
      </Route>

      {/* Private routes */}
      <Route element={<PrivateRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/dashboard" element={<PrivateDashboard />} />
          <Route path="/analytics" element={<AnalyticsPage />} />
          <Route path="/services/:id" element={<ServiceDetailPage />} />
          <Route
            path="/admin"
            element={
              <AdminRoute>
                <AdminPage />
              </AdminRoute>
            }
          />
        </Route>
      </Route>

      {/* 404 */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default App;

