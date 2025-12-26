import { useState } from 'react';
import ServicesManagement from '@/components/admin/ServicesManagement';
import UsersManagement from '@/components/admin/UsersManagement';
import MaintenanceManagement from '@/components/admin/MaintenanceManagement';
import './AdminPage.css';

type TabType = 'services' | 'users' | 'maintenance';

const AdminPage = () => {
  const [activeTab, setActiveTab] = useState<TabType>('services');

  const tabs = [
    { id: 'services' as TabType, label: 'Сервисы' },
    { id: 'users' as TabType, label: 'Пользователи' },
    { id: 'maintenance' as TabType, label: 'Обслуживание' },
  ];

  return (
    <div className="admin-page">
      <div className="admin-header">
        <h1>Администрирование</h1>
      </div>

      <div className="admin-tabs">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            className={`admin-tab ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="admin-content">
        {activeTab === 'services' && <ServicesManagement />}
        {activeTab === 'users' && <UsersManagement />}
        {activeTab === 'maintenance' && <MaintenanceManagement />}
      </div>
    </div>
  );
};

export default AdminPage;

