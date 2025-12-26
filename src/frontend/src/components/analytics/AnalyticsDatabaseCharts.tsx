import { AnalyticsDto } from '@/types';
import { LineChart, Line, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, ComposedChart, Bar } from 'recharts';
import './AnalyticsDatabaseCharts.css';

interface AnalyticsDatabaseChartsProps {
  analytics: AnalyticsDto;
  period: string;
}

const AnalyticsDatabaseCharts = ({ analytics, period }: AnalyticsDatabaseChartsProps) => {
  const { timeSeries } = analytics;

  if (!timeSeries.databaseSizeSeries || timeSeries.databaseSizeSeries.length === 0) {
    return (
      <div className="database-charts-empty">
        <p>Нет данных о размере баз данных за выбранный период</p>
      </div>
    );
  }

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

  const formatSize = (mb: number): string => {
    if (mb < 1024) {
      return `${mb.toFixed(2)} МБ`;
    } else if (mb < 1024 * 1024) {
      return `${(mb / 1024).toFixed(2)} ГБ`;
    } else {
      return `${(mb / (1024 * 1024)).toFixed(2)} ТБ`;
    }
  };

  // Группируем данные по сервисам
  const servicesMap = new Map<string, typeof timeSeries.databaseSizeSeries>();
  timeSeries.databaseSizeSeries.forEach(point => {
    if (!servicesMap.has(point.serviceId)) {
      servicesMap.set(point.serviceId, []);
    }
    servicesMap.get(point.serviceId)!.push(point);
  });

  // Группируем прогнозы по сервисам
  const forecastMap = new Map<string, typeof timeSeries.databaseSizeForecast>();
  if (timeSeries.databaseSizeForecast) {
    timeSeries.databaseSizeForecast.forEach(point => {
      if (!forecastMap.has(point.serviceId)) {
        forecastMap.set(point.serviceId, []);
      }
      forecastMap.get(point.serviceId)!.push(point);
    });
  }

  const services = Array.from(servicesMap.entries());

  return (
    <div className="analytics-database-charts">
      <h2 className="database-charts-title">Метрики размера баз данных</h2>

      {services.map(([serviceId, points]) => {
        const serviceName = points[0]?.serviceName || serviceId;
        const forecastPoints = forecastMap.get(serviceId) || [];
        const lastPoint = points[points.length - 1];
        
        // Объединяем исторические данные и прогноз для графика
        const combinedData = [
          ...points.map(p => ({
            ...p,
            isForecast: false,
            forecastedTotalSizeMB: null as number | null,
            forecastedUsedSpaceMB: null as number | null,
          })),
          ...forecastPoints.map(p => ({
            timestamp: p.timestamp,
            serviceId: p.serviceId,
            serviceName: p.serviceName,
            totalSizeMB: p.forecastedTotalSizeMB,
            usedSpaceMB: p.forecastedUsedSpaceMB,
            usagePercentage: p.forecastedUsagePercentage,
            isForecast: true,
            forecastedTotalSizeMB: p.forecastedTotalSizeMB,
            forecastedUsedSpaceMB: p.forecastedUsedSpaceMB,
          })),
        ].sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());
        
        return (
          <div key={serviceId} className="database-chart-section">
            <h3 className="database-chart-service-name">{serviceName}</h3>
            
            {/* Информация о прогнозе */}
            {forecastPoints.length > 0 && (
              <div className="forecast-info">
                {forecastPoints[0]?.growthRateMBPerDay && (
                  <div className="forecast-stat">
                    <span className="forecast-label">Скорость роста:</span>
                    <span className="forecast-value">
                      {forecastPoints[0].growthRateMBPerDay > 0 ? '+' : ''}
                      {formatSize(Math.abs(forecastPoints[0].growthRateMBPerDay))}/день
                    </span>
                  </div>
                )}
                {forecastPoints[0]?.estimatedFullDate && (
                  <div className="forecast-stat">
                    <span className="forecast-label">Прогноз заполнения:</span>
                    <span className="forecast-value forecast-warning">
                      {new Date(forecastPoints[0].estimatedFullDate).toLocaleDateString('ru-RU', {
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric'
                      })}
                    </span>
                  </div>
                )}
              </div>
            )}

            <div className="database-charts-grid">
              {/* График общего размера БД с прогнозом */}
              <div className="chart-container">
                <h4>Общий размер БД и прогноз роста</h4>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={combinedData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestamp" 
                      tickFormatter={formatDate}
                      angle={-45}
                      textAnchor="end"
                      height={80}
                    />
                    <YAxis 
                      tickFormatter={(value) => formatSize(value)}
                    />
                    <Tooltip 
                      labelFormatter={(value) => {
                        if (typeof value === 'string') return formatDate(value);
                        return formatDate(new Date(value).toISOString());
                      }}
                      formatter={(value: number, name: string, props: any) => {
                        if (!value || value === 0) return ['-', name];
                        return [formatSize(value), name];
                      }}
                    />
                    <Legend />
                    {/* Исторические данные */}
                    <Line 
                      type="monotone" 
                      dataKey="totalSizeMB" 
                      stroke="#3b82f6" 
                      strokeWidth={2}
                      name="Общий размер (факт)"
                      dot={false}
                      connectNulls={false}
                    />
                    <Line 
                      type="monotone" 
                      dataKey="dataSizeMB" 
                      stroke="#10b981" 
                      strokeWidth={2}
                      name="Размер данных (факт)"
                      dot={false}
                      connectNulls={false}
                    />
                    <Line 
                      type="monotone" 
                      dataKey="logSizeMB" 
                      stroke="#f59e0b" 
                      strokeWidth={2}
                      name="Размер логов (факт)"
                      dot={false}
                      connectNulls={false}
                    />
                    {/* Прогноз */}
                    {forecastPoints.length > 0 && (
                      <>
                        <Line 
                          type="monotone" 
                          dataKey="forecastedTotalSizeMB" 
                          stroke="#8b5cf6" 
                          strokeWidth={2}
                          strokeDasharray="5 5"
                          name="Прогноз общего размера"
                          dot={false}
                          connectNulls={false}
                        />
                        <Line 
                          type="monotone" 
                          dataKey="forecastedUsedSpaceMB" 
                          stroke="#ef4444" 
                          strokeWidth={2}
                          strokeDasharray="5 5"
                          name="Прогноз используемого пространства"
                          dot={false}
                          connectNulls={false}
                        />
                      </>
                    )}
                  </LineChart>
                </ResponsiveContainer>
              </div>

              {/* График использования пространства */}
              <div className="chart-container">
                <h4>Использование пространства</h4>
                <ResponsiveContainer width="100%" height={250}>
                  <AreaChart data={points}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestamp" 
                      tickFormatter={formatDate}
                      angle={-45}
                      textAnchor="end"
                      height={80}
                    />
                    <YAxis 
                      tickFormatter={(value) => formatSize(value)}
                    />
                    <Tooltip 
                      labelFormatter={(value) => {
                        if (typeof value === 'string') return formatDate(value);
                        return formatDate(new Date(value).toISOString());
                      }}
                      formatter={(value: number) => formatSize(value)}
                    />
                    <Legend />
                    <Area 
                      type="monotone" 
                      dataKey="usedSpaceMB" 
                      stackId="1"
                      stroke="#ef4444" 
                      fill="#ef4444" 
                      name="Используется"
                    />
                    <Area 
                      type="monotone" 
                      dataKey="freeSpaceMB" 
                      stackId="1"
                      stroke="#10b981" 
                      fill="#10b981" 
                      name="Свободно"
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>

              {/* График процента использования */}
              <div className="chart-container">
                <h4>Процент использования</h4>
                <ResponsiveContainer width="100%" height={250}>
                  <ComposedChart data={combinedData}>
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
                      formatter={(value: number, name: string, props: any) => {
                        if (!value || value === 0) return ['-', name];
                        return [`${value.toFixed(2)}%`, name];
                      }}
                    />
                    <Legend />
                    <Area 
                      type="monotone" 
                      dataKey="usagePercentage" 
                      fill="#fef3c7"
                      stroke="#f59e0b"
                      strokeWidth={2}
                      name="Использование (%) - факт"
                      connectNulls={false}
                    />
                    {forecastPoints.length > 0 && (
                      <Line 
                        type="monotone" 
                        dataKey="forecastedUsagePercentage" 
                        stroke="#8b5cf6"
                        strokeWidth={2}
                        strokeDasharray="5 5"
                        name="Прогноз использования (%)"
                        dot={false}
                        connectNulls={false}
                      />
                    )}
                  </ComposedChart>
                </ResponsiveContainer>
              </div>

              {/* График прогноза роста */}
              {forecastPoints.length > 0 && (
                <div className="chart-container chart-container-full">
                  <h4>Прогноз роста БД</h4>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={combinedData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="timestamp" 
                        tickFormatter={formatDate}
                        angle={-45}
                        textAnchor="end"
                        height={80}
                      />
                      <YAxis 
                        tickFormatter={(value) => formatSize(value)}
                      />
                      <Tooltip 
                        labelFormatter={(value) => {
                          if (typeof value === 'string') return formatDate(value);
                          return formatDate(new Date(value).toISOString());
                        }}
                        formatter={(value: number, name: string, props: any) => {
                          if (!value || value === 0) return ['-', name];
                          return [formatSize(value), name];
                        }}
                      />
                      <Legend />
                      {/* Разделительная линия между фактом и прогнозом */}
                      <Line 
                        type="monotone" 
                        dataKey="totalSizeMB" 
                        stroke="#3b82f6" 
                        strokeWidth={2}
                        name="Общий размер (факт)"
                        dot={false}
                        connectNulls={false}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="forecastedTotalSizeMB" 
                        stroke="#8b5cf6" 
                        strokeWidth={2}
                        strokeDasharray="5 5"
                        name="Прогноз общего размера"
                        dot={{ fill: '#8b5cf6', r: 4 }}
                        connectNulls={false}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="usedSpaceMB" 
                        stroke="#ef4444" 
                        strokeWidth={2}
                        name="Используемое пространство (факт)"
                        dot={false}
                        connectNulls={false}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="forecastedUsedSpaceMB" 
                        stroke="#f87171" 
                        strokeWidth={2}
                        strokeDasharray="5 5"
                        name="Прогноз используемого пространства"
                        dot={{ fill: '#f87171', r: 4 }}
                        connectNulls={false}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
};

export default AnalyticsDatabaseCharts;

