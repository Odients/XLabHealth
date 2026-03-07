import { AnalyticsDto } from '@/types';
import { LineChart, Line, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, ComposedChart } from 'recharts';
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
    if (mb == null || Number.isNaN(mb)) return '-';
    if (mb < 1024) {
      return `${mb.toFixed(2)} МБ`;
    } else if (mb < 1024 * 1024) {
      return `${(mb / 1024).toFixed(2)} ГБ`;
    } else {
      return `${(mb / (1024 * 1024)).toFixed(2)} ТБ`;
    }
  };

  const safeFormatSize = (value: number | null | undefined): string => {
    if (value == null || value === undefined || Number.isNaN(value)) return '-';
    return formatSize(value);
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
        
        // Исторические точки: факт в totalSizeMB/usedSpaceMB; для последней точки — заполняем прогнозные поля для плавного соединения линий
        const historicalPoints = points.map((p, idx) => {
          const isLast = idx === points.length - 1;
          const hasForecast = forecastPoints.length > 0;
          const ts = new Date(p.timestamp).getTime();
          return {
            ...p,
            timestampMs: ts,
            isForecast: false,
            forecastedTotalSizeMB: (isLast && hasForecast ? p.totalSizeMB : null) as number | null,
            forecastedUsedSpaceMB: (isLast && hasForecast ? p.usedSpaceMB : null) as number | null,
            forecastedUsagePercentage: (isLast && hasForecast ? p.usagePercentage : null) as number | null,
          };
        });

        // Точки прогноза: только прогнозные поля, фактические = null (линии факта не продлеваются в будущее)
        const forecastDataPoints = forecastPoints.map(p => {
          const ts = new Date(p.timestamp).getTime();
          return {
            timestamp: p.timestamp,
            timestampMs: ts,
            serviceId: p.serviceId,
            serviceName: p.serviceName,
            totalSizeMB: null as number | null,
            dataSizeMB: null as number | null,
            logSizeMB: null as number | null,
            usedSpaceMB: null as number | null,
            freeSpaceMB: null as number | null,
            usagePercentage: null as number | null,
            isForecast: true,
            forecastedTotalSizeMB: p.forecastedTotalSizeMB,
            forecastedUsedSpaceMB: p.forecastedUsedSpaceMB,
            forecastedUsagePercentage: p.forecastedUsagePercentage,
          };
        });

        const combinedData = [
          ...historicalPoints,
          ...forecastDataPoints,
        ].sort((a, b) => (a.timestampMs ?? 0) - (b.timestampMs ?? 0));
        
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
                      dataKey="timestampMs" 
                      type="number"
                      domain={['dataMin', 'dataMax']}
                      tickFormatter={(ms) => formatDate(new Date(ms).toISOString())}
                      angle={-45}
                      textAnchor="end"
                      height={80}
                    />
                    <YAxis 
                      tickFormatter={(value) => safeFormatSize(value)}
                    />
                    <Tooltip 
                      labelFormatter={(value) => {
                        if (typeof value === 'number') return formatDate(new Date(value).toISOString());
                        if (typeof value === 'string') return formatDate(value);
                        return formatDate(new Date(value).toISOString());
                      }}
                      formatter={(value: unknown, name: string) => {
                        const num = typeof value === 'number' ? value : undefined;
                        if (num == null || num === undefined || Number.isNaN(num)) return ['-', name];
                        return [formatSize(num), name];
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
                          connectNulls={true}
                        />
                        <Line 
                          type="monotone" 
                          dataKey="forecastedUsedSpaceMB" 
                          stroke="#ef4444" 
                          strokeWidth={2}
                          strokeDasharray="5 5"
                          name="Прогноз используемого пространства"
                          dot={false}
                          connectNulls={true}
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
                  <AreaChart data={points.map(p => ({ ...p, timestampMs: new Date(p.timestamp).getTime() }))}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestampMs" 
                      type="number"
                      domain={['dataMin', 'dataMax']}
                      tickFormatter={(ms) => formatDate(new Date(ms).toISOString())}
                      angle={-45}
                      textAnchor="end"
                      height={80}
                    />
                    <YAxis 
                      tickFormatter={(value) => safeFormatSize(value)}
                    />
                    <Tooltip 
                      labelFormatter={(value) => {
                        if (typeof value === 'number') return formatDate(new Date(value).toISOString());
                        if (typeof value === 'string') return formatDate(value);
                        return formatDate(new Date(value).toISOString());
                      }}
                      formatter={(value: unknown) => safeFormatSize(typeof value === 'number' ? value : undefined)}
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
                      dataKey="timestampMs" 
                      type="number"
                      domain={['dataMin', 'dataMax']}
                      tickFormatter={(ms) => formatDate(new Date(ms).toISOString())}
                      angle={-45}
                      textAnchor="end"
                      height={80}
                    />
                    <YAxis domain={[0, 100]} />
                    <Tooltip 
                      labelFormatter={(value) => {
                        if (typeof value === 'number') return formatDate(new Date(value).toISOString());
                        if (typeof value === 'string') return formatDate(value);
                        return formatDate(new Date(value).toISOString());
                      }}
                      formatter={(value: unknown, name: string) => {
                        const num = typeof value === 'number' ? value : undefined;
                        if (num == null || num === undefined || Number.isNaN(num)) return ['-', name];
                        return [`${num.toFixed(2)}%`, name];
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
                        connectNulls={true}
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
                        dataKey="timestampMs" 
                        type="number"
                        domain={['dataMin', 'dataMax']}
                        tickFormatter={(ms) => formatDate(new Date(ms).toISOString())}
                        angle={-45}
                        textAnchor="end"
                        height={80}
                      />
                      <YAxis 
                        tickFormatter={(value) => safeFormatSize(value)}
                      />
                      <Tooltip 
                        labelFormatter={(value) => {
                          if (typeof value === 'number') return formatDate(new Date(value).toISOString());
                          if (typeof value === 'string') return formatDate(value);
                          return formatDate(new Date(value).toISOString());
                        }}
                        formatter={(value: unknown, name: string) => {
                          const num = typeof value === 'number' ? value : undefined;
                          if (num == null || num === undefined || Number.isNaN(num)) return ['-', name];
                          return [formatSize(num), name];
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
                        connectNulls={true}
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
                        connectNulls={true}
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

