import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { useTranslation } from 'react-i18next';
import { authApi } from '@/services/api';
import { useAuthStore } from '@/store/authStore';
import { isBackendUnavailable } from '@/utils/backend';
import { useRecaptcha } from '@/hooks/useRecaptcha';
import { LoginDto } from '@/types';
import './LoginPage.css';

type LoginFormData = {
  username: string;
  password: string;
};

const LoginPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { setUser } = useAuthStore();
  const [isLoading, setIsLoading] = useState(false);
  const { getToken } = useRecaptcha();

  const loginSchema = useMemo(
    () =>
      z.object({
        username: z.string().min(1, t('public.login.usernameRequired')),
        password: z.string().min(1, t('public.login.passwordRequired')),
      }),
    [t]
  );

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    try {
      // Получаем токен reCAPTCHA перед отправкой формы
      const recaptchaToken = await getToken('login');
      
      // Отправляем данные логина с токеном reCAPTCHA (если доступен)
      const loginData: LoginDto = {
        ...data,
        ...(recaptchaToken && { recaptchaToken }),
      };
      
      const response = await authApi.login(loginData);
      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);
      // setUser сохранит пользователя в localStorage автоматически
      setUser(response.user);
      toast.success(t('public.login.success'));
      navigate('/dashboard');
    } catch (error: any) {
      // Если бэкенд недоступен, показываем нейтральное сообщение
      if (isBackendUnavailable(error)) {
        toast.error(t('public.backendUnavailable.message'));
      } else if (error.response?.status === 429) {
        // Обработка ошибки "Too Many Requests" (защита от брутфорса)
        const message = error.response?.data?.message || t('public.login.tooManyAttempts');
        toast.error(message, { autoClose: 5000 });
      } else {
        const errorMessage = error.response?.data?.error || error.response?.data?.message || t('public.login.error');
        toast.error(errorMessage);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-card">
          <div className="login-header">
            <img src="/favicon.ico" alt="X-Lab" className="login-logo" />
            <h1>{t('public.login.title')}</h1>
            <p className="login-subtitle">{t('public.login.subtitle')}</p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="login-form">
            <div className="form-group">
              <label htmlFor="username">{t('public.login.username')}</label>
              <input
                id="username"
                type="text"
                {...register('username')}
                className={errors.username ? 'error' : ''}
                placeholder={t('public.login.usernamePlaceholder')}
              />
              {errors.username && (
                <span className="error-message">{errors.username.message}</span>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="password">{t('public.login.password')}</label>
              <input
                id="password"
                type="password"
                {...register('password')}
                className={errors.password ? 'error' : ''}
                placeholder={t('public.login.passwordPlaceholder')}
              />
              {errors.password && (
                <span className="error-message">{errors.password.message}</span>
              )}
            </div>

            <button
              type="submit"
              className="btn-primary"
              disabled={isLoading}
            >
              {isLoading ? t('public.login.signingIn') : t('public.login.signIn')}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;

