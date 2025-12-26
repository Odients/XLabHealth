import { AnalyticsDto } from '@/types';
import { LineChart, Line, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, BarChart, Bar } from 'recharts';
import './AnalyticsCharts.css';

interface AnalyticsChartsProps {
  analytics: AnalyticsDto;
  period: string;
}

const AnalyticsCharts = ({ analytics, period }: AnalyticsChartsProps) => {
  const { timeSeries } = analytics;

  const formatDate = (timestamp: string): string => {
    const date = new Date(timestamp);
    if (period === '24h') {
      return date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    } else if (period === '7d') {
      return date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' });
    } else {
      return date.toLocaleDateString('ru-RU', { month: 'short', year: 'numeric' });
    }
  };

  return (
    <div className="analytics-charts">
      <div className="chart-section">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
          <h2>Доступность системы во времени</h2>
          <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>
            Система недоступна при недоступности любого сервиса
          </span>
        </div>
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={timeSeries.uptimeSeries}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={formatDate}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis domain={[0, 100]} />
            <Tooltip 
              labelFormatter={(value) => {
                if (typeof value === 'string') return formatDate(value);
                return formatDate(new Date(value).toISOString());
              }}
              formatter={(value: number) => [`${value.toFixed(2)}%`, 'Доступность']}
            />
            <Legend />
            <Line 
              type="monotone" 
              dataKey="value" 
              stroke="#10b981" 
              strokeWidth={2}
              name="Доступность (%)"
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      <div className="chart-section">
        <h2>Время отклика во времени</h2>
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={timeSeries.responseTimeSeries}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={formatDate}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis />
            <Tooltip 
              labelFormatter={(value) => {
                if (typeof value === 'string') return formatDate(value);
                return formatDate(new Date(value).toISOString());
              }}
              formatter={(value: number) => [`${Math.round(value)} мс`, 'Время отклика']}
            />
            <Legend />
            <Line 
              type="monotone" 
              dataKey="value" 
              stroke="#3b82f6" 
              strokeWidth={2}
              name="Время отклика (мс)"
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      <div className="chart-section">
        <h2>Распределение статусов</h2>
        <ResponsiveContainer width="100%" height={300}>
          <AreaChart data={timeSeries.statusDistributionSeries}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={formatDate}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis />
            <Tooltip 
              labelFormatter={(value) => {
                if (typeof value === 'string') return formatDate(value);
                return formatDate(new Date(value).toISOString());
              }}
            />
            <Legend />
            <Area 
              type="monotone" 
              dataKey="healthy" 
              stackId="1" 
              stroke="#10b981" 
              fill="#10b981" 
              name="Здоров"
            />
            <Area 
              type="monotone" 
              dataKey="degraded" 
              stackId="1" 
              stroke="#f59e0b" 
              fill="#f59e0b" 
              name="Деградирован"
            />
            <Area 
              type="monotone" 
              dataKey="unhealthy" 
              stackId="1" 
              stroke="#ef4444" 
              fill="#ef4444" 
              name="Не здоров"
            />
            <Area 
              type="monotone" 
              dataKey="unknown" 
              stackId="1" 
              stroke="#6b7280" 
              fill="#6b7280" 
              name="Неизвестно"
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      <div className="chart-section">
        <h2>Количество проверок</h2>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={timeSeries.checkCountSeries}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={formatDate}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis />
            <Tooltip 
              labelFormatter={(value) => {
                if (typeof value === 'string') return formatDate(value);
                return formatDate(new Date(value).toISOString());
              }}
              formatter={(value: number) => [`${Math.round(value)}`, 'Проверок']}
            />
            <Legend />
            <Bar dataKey="value" fill="#3b82f6" name="Количество проверок" />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default AnalyticsCharts;

