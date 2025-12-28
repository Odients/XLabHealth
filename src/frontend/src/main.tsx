import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { ToastContainer } from 'react-toastify';
import App from './App';
import { getClientIp } from './utils/clientIp';
import { initGoogleTagManager } from './utils/gtm';
import { initRecaptcha } from './utils/recaptcha';
import './i18n/config';
import './styles/index.css';
import 'react-toastify/dist/ReactToastify.css';

// Предзагружаем IP клиента при инициализации приложения
// Это позволяет кэшировать IP до первого запроса к API
getClientIp().catch((error) => {
  console.warn('Failed to preload client IP:', error);
});

// Инициализируем Google Tag Manager
initGoogleTagManager();

// Инициализируем Google reCAPTCHA v3
initRecaptcha().catch((error) => {
  console.warn('Failed to initialize reCAPTCHA:', error);
});

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true,
        }}
      >
        <App />
        <ToastContainer
          position="top-right"
          autoClose={3000}
          hideProgressBar={false}
          newestOnTop={false}
          closeOnClick
          rtl={false}
          pauseOnFocusLoss
          draggable
          pauseOnHover
          theme="light"
        />
      </BrowserRouter>
    </QueryClientProvider>
  </React.StrictMode>
);

