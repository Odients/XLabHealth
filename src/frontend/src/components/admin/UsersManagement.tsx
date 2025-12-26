import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '@/services/api';
import { toast } from 'react-toastify';
import type { UserDto, UserCreateDto, UserUpdateDto } from '@/types';
import './UsersManagement.css';

const UsersManagement = () => {
  const queryClient = useQueryClient();
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [formData, setFormData] = useState<UserCreateDto>({
    username: '',
    email: '',
    password: '',
    role: 'Viewer',
    isActive: true,
  });

  const { data: users, isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: usersApi.getAll,
  });

  const createMutation = useMutation({
    mutationFn: usersApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Пользователь создан');
      setIsCreateModalOpen(false);
      resetForm();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при создании пользователя');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UserUpdateDto }) =>
      usersApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Пользователь обновлен');
      setEditingUser(null);
      resetForm();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при обновлении пользователя');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: usersApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Пользователь удален');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при удалении пользователя');
    },
  });

  const resetForm = () => {
    setFormData({
      username: '',
      email: '',
      password: '',
      role: 'Viewer',
      isActive: true,
    });
  };

  const handleCreate = () => {
    if (!formData.username || !formData.email || !formData.password) {
      toast.error('Заполните все обязательные поля');
      return;
    }
    createMutation.mutate(formData);
  };

  const handleEdit = (user: UserDto) => {
    setEditingUser(user);
    setFormData({
      username: user.username,
      email: user.email || '',
      password: '',
      role: user.role,
      isActive: user.isActive,
    });
  };

  const handleUpdate = () => {
    if (!editingUser) return;
    const updateData: UserUpdateDto = {
      username: formData.username,
      email: formData.email,
      role: formData.role,
      isActive: formData.isActive,
    };
    if (formData.password) {
      updateData.password = formData.password;
    }
    updateMutation.mutate({ id: editingUser.id, data: updateData });
  };

  const handleDelete = (id: string, username: string) => {
    if (window.confirm(`Вы уверены, что хотите удалить пользователя "${username}"?`)) {
      deleteMutation.mutate(id);
    }
  };

  const handleCancel = () => {
    setIsCreateModalOpen(false);
    setEditingUser(null);
    resetForm();
  };

  if (isLoading) {
    return (
      <div className="users-management">
        <div className="loading">Загрузка...</div>
      </div>
    );
  }

  return (
    <div className="users-management">
      <div className="users-header">
        <h2>Управление пользователями</h2>
        <button className="btn-primary" onClick={() => setIsCreateModalOpen(true)}>
          Добавить пользователя
        </button>
      </div>

      <div className="users-content">
        {users && users.length > 0 ? (
          <div className="users-table-container">
            <table className="users-table">
              <thead>
                <tr>
                  <th>Имя пользователя</th>
                  <th>Email</th>
                  <th>Роль</th>
                  <th>Активен</th>
                  <th>Создан</th>
                  <th>Последний вход</th>
                  <th>Действия</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <strong>{user.username}</strong>
                    </td>
                    <td>{user.email || '-'}</td>
                    <td>
                      <span className={`role-badge role-${user.role.toLowerCase()}`}>
                        {user.role}
                      </span>
                    </td>
                    <td>{user.isActive ? '✓' : '✕'}</td>
                    <td>{new Date(user.createdAt).toLocaleDateString('ru-RU')}</td>
                    <td>
                      {user.lastLoginAt
                        ? new Date(user.lastLoginAt).toLocaleDateString('ru-RU')
                        : '-'}
                    </td>
                    <td>
                      <div className="action-buttons">
                        <button className="btn-edit" onClick={() => handleEdit(user)}>
                          Редактировать
                        </button>
                        <button
                          className="btn-delete"
                          onClick={() => handleDelete(user.id, user.username)}
                        >
                          Удалить
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="no-users">Нет пользователей</div>
        )}
      </div>

      {(isCreateModalOpen || editingUser) && (
        <div className="modal-overlay" onClick={handleCancel}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2>{editingUser ? 'Редактировать пользователя' : 'Добавить пользователя'}</h2>
            <div className="form-group">
              <label htmlFor="username">Имя пользователя *</label>
              <input
                id="username"
                type="text"
                value={formData.username}
                onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                disabled={!!editingUser}
              />
            </div>
            <div className="form-group">
              <label htmlFor="email">Email *</label>
              <input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label htmlFor="password">
                Пароль {editingUser ? '(оставьте пустым, чтобы не менять)' : '*'}
              </label>
              <input
                id="password"
                type="password"
                value={formData.password}
                onChange={(e) => setFormData({ ...formData, password: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label htmlFor="role">Роль *</label>
              <select
                id="role"
                value={formData.role}
                onChange={(e) => setFormData({ ...formData, role: e.target.value })}
              >
                <option value="Viewer">Viewer</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                />
                Активен
              </label>
            </div>
            <div className="modal-actions">
              <button className="btn-secondary" onClick={handleCancel}>
                Отмена
              </button>
              <button
                className="btn-primary"
                onClick={editingUser ? handleUpdate : handleCreate}
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                {editingUser ? 'Сохранить' : 'Создать'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UsersManagement;

