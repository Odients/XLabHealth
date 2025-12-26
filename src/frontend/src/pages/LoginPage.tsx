import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { authApi } from '@/services/api';
import { useAuthStore } from '@/store/authStore';
import { isBackendUnavailable, getBackendUnavailableMessage } from '@/utils/backend';
import './LoginPage.css';

const loginSchema = z.object({
  username: z.string().min(1, 'Имя пользователя обязательно'),
  password: z.string().min(1, 'Пароль обязателен'),
});

type LoginFormData = z.infer<typeof loginSchema>;

const LoginPage = () => {
  const navigate = useNavigate();
  const { setUser } = useAuthStore();
  const [isLoading, setIsLoading] = useState(false);

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
      const response = await authApi.login(data);
      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);
      // setUser сохранит пользователя в localStorage автоматически
      setUser(response.user);
      toast.success('Успешный вход в систему');
      navigate('/dashboard');
    } catch (error: any) {
      // Если бэкенд недоступен, показываем нейтральное сообщение
      if (isBackendUnavailable(error)) {
        toast.error(getBackendUnavailableMessage());
      } else if (error.response?.status === 429) {
        // Обработка ошибки "Too Many Requests" (защита от брутфорса)
        const message = error.response?.data?.message || 'Слишком много неудачных попыток входа. Пожалуйста, подождите несколько минут и попробуйте снова.';
        toast.error(message, { autoClose: 5000 });
      } else {
        const errorMessage = error.response?.data?.error || error.response?.data?.message || 'Ошибка входа. Проверьте данные.';
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
            <h1>X-Lab Status</h1>
            <p className="login-subtitle">Вход в систему</p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="login-form">
            <div className="form-group">
              <label htmlFor="username">Имя пользователя</label>
              <input
                id="username"
                type="text"
                {...register('username')}
                className={errors.username ? 'error' : ''}
                placeholder="Введите имя пользователя"
              />
              {errors.username && (
                <span className="error-message">{errors.username.message}</span>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="password">Пароль</label>
              <input
                id="password"
                type="password"
                {...register('password')}
                className={errors.password ? 'error' : ''}
                placeholder="Введите пароль"
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
              {isLoading ? 'Вход...' : 'Войти'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;

