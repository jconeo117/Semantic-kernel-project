import { useState, useEffect, useCallback } from "react";
import { useTheme, planColor, typeColor, fmtTime } from "../styles/tokens";
import { LoadingState, ErrorBanner } from "../components/Feedback";
import { fetchMetrics, fetchRecentAudit, fetchTenants } from "../api/client";

export default function Dashboard() {
  const { colors: C, styles: s } = useTheme();
  const [metrics, setMetrics] = useState(null);
  const [recentEvents, setRecentEvents] = useState([]);
  const [tenants, setTenants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [metricsData, auditData, tenantsData] = await Promise.all([
        fetchMetrics(),
        fetchRecentAudit(null, 5),
        fetchTenants(),
      ]);
      setMetrics(metricsData);
      setRecentEvents(auditData.entries || []);
      setTenants(tenantsData.tenants || []);
    } catch (err) {
      setError(err.response?.data?.error || err.message || "Error al cargar datos del dashboard");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) return <LoadingState label="Cargando dashboard..." />;
  if (error) return <ErrorBanner message={error} onRetry={load} />;
  if (!metrics) return null;

  const maxCount = metrics.messagesPerDay.length > 0
    ? Math.max(...metrics.messagesPerDay.map(d => d.count))
    : 1;
  const activeTenants = tenants.length;
  const today = new Date().toLocaleDateString("es-CO", { day: "numeric", month: "short", year: "numeric" });

  return (
    <div className="fade-in">
      <div style={s.header}>
        <div>
          <div style={s.title}>Dashboard</div>
          <div style={s.subtitle}>Resumen del sistema · {today}</div>
        </div>
        <button style={s.btn()} onClick={load}>↻ Refrescar</button>
      </div>

      <div style={{ ...s.grid4, marginBottom: 16 }}>
        {[
          { label: "Mensajes totales", value: metrics.totalMessages.toLocaleString(), sub: `Desde ${new Date(metrics.from).toLocaleDateString("es-CO")}`, color: C.text },
          { label: "Sesiones únicas", value: metrics.uniqueSessions.toLocaleString(), sub: "Periodo seleccionado", color: C.text },
          { label: "Tenants registrados", value: activeTenants, sub: "En el sistema", color: C.accent },
          { label: "Bloques de seguridad", value: metrics.securityBlocks, sub: "Periodo seleccionado", color: C.red },
        ].map(stat => (
          <div key={stat.label} style={s.card}>
            <div style={s.cardLabel}>{stat.label}</div>
            <div style={{ ...s.statNum, color: stat.color }}>{stat.value}</div>
            <div style={s.statSub}>{stat.sub}</div>
          </div>
        ))}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1.5fr 1fr", gap: 16, marginBottom: 16 }}>
        <div style={s.card}>
          <div style={s.cardLabel}>Actividad — mensajes por día</div>
          <div style={s.barChart}>
            {metrics.messagesPerDay.map((d, i) => (
              <div key={d.date} style={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", gap: 4, height: "100%" }}>
                <div style={{ flex: 1, width: "100%", display: "flex", alignItems: "flex-end" }}>
                  <div style={s.bar(d.count / maxCount, i === metrics.messagesPerDay.length - 1)} />
                </div>
                <div style={{ fontSize: 9, color: C.textDim }}>{new Date(d.date).getDate()}</div>
              </div>
            ))}
          </div>
          {metrics.messagesPerDay.length > 0 && (
            <div style={{ ...s.row, marginTop: 12, gap: 16 }}>
              <div style={{ fontSize: 11, color: C.textMuted }}>
                Pico: <span style={{ color: C.text }}>{maxCount} msgs</span>
              </div>
              <div style={{ fontSize: 11, color: C.textMuted }}>
                Prom: <span style={{ color: C.text }}>{Math.round(metrics.messagesPerDay.reduce((a, b) => a + b.count, 0) / metrics.messagesPerDay.length)} msgs/día</span>
              </div>
            </div>
          )}
        </div>

        <div style={s.card}>
          <div style={s.cardLabel}>Resumen de Tenants</div>
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
            <span style={{ fontSize: 12 }}>Total</span>
            <span style={{ fontSize: 24, fontWeight: 700, color: C.accent }}>{activeTenants}</span>
          </div>
          <div style={s.divider} />
          <div style={{ fontSize: 11, color: C.textMuted }}>Tenants registrados en el sistema</div>
        </div>
      </div>

      <div style={s.card}>
        <div style={s.cardLabel}>Eventos recientes</div>
        {recentEvents.length === 0 ? (
          <div style={{ color: C.textDim, fontSize: 12, padding: "20px 0" }}>Sin eventos recientes.</div>
        ) : (
          <table style={s.table}>
            <thead>
              <tr>
                {["Tenant", "Tipo", "Hora", "Contenido"].map(h => <th key={h} style={s.th}>{h}</th>)}
              </tr>
            </thead>
            <tbody>
              {recentEvents.map((e, idx) => (
                <tr key={e.id || idx}>
                  <td style={{ ...s.td, fontSize: 11, color: C.textMuted }}>{e.tenantId}</td>
                  <td style={s.td}><span style={{ ...s.chip(typeColor(e.eventType, C)), fontSize: 9 }}>{e.eventType}</span></td>
                  <td style={{ ...s.td, fontSize: 11, color: C.textDim }}>{fmtTime(e.timestamp)}</td>
                  <td style={{ ...s.td, color: C.textMuted, maxWidth: 280, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{e.content}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
