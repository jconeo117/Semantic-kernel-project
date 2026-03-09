import { useState, useEffect, useCallback } from "react";
import { useTheme, typeColor, threatColor, fmt, fmtTime } from "../styles/tokens";
import { LoadingState, ErrorBanner } from "../components/Feedback";
import { fetchRecentAudit } from "../api/client";

export default function AuditPage() {
  const { colors: C, styles: s } = useTheme();
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState("all");

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchRecentAudit(null, 100);
      setEvents(data.entries || []);
    } catch (err) {
      setError(err.response?.data?.error || err.message || "Error al cargar auditoría");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const filtered = filter === "all" ? events : events.filter(e => e.eventType === filter);

  if (loading) return <LoadingState label="Cargando auditoría..." />;
  if (error) return <ErrorBanner message={error} onRetry={load} />;

  return (
    <div className="fade-in">
      <div style={s.header}>
        <div>
          <div style={s.title}>Auditoría</div>
          <div style={s.subtitle}>Historial de interacciones y eventos · {events.length} registros</div>
        </div>
        <button style={s.btn()} onClick={load}>↻ Refrescar</button>
      </div>

      <div style={{ display: "flex", gap: 6, marginBottom: 20 }}>
        {["all", "UserMessage", "AgentResponse", "SecurityBlock", "OutputFiltered"].map(f => (
          <button key={f} style={{
            ...s.btn(), fontSize: 10,
            background: filter === f ? C.accentDim : C.surfaceHover,
            color: filter === f ? C.accent : C.textMuted,
            border: `1px solid ${filter === f ? C.accentBorder : "transparent"}`,
          }} onClick={() => setFilter(f)}>
            {f === "all" ? "Todos" : f}
          </button>
        ))}
      </div>

      <div style={s.card}>
        <table style={s.table}>
          <thead>
            <tr>
              {["Tenant", "Tipo", "Amenaza", "Hora", "Contenido"].map(h => <th key={h} style={s.th}>{h}</th>)}
            </tr>
          </thead>
          <tbody>
            {filtered.map((e, idx) => (
              <tr key={e.id || idx}>
                <td style={{ ...s.td, fontSize: 11, color: C.textMuted, fontFamily: "monospace" }}>{e.tenantId}</td>
                <td style={s.td}><span style={s.chip(typeColor(e.eventType, C))}>{e.eventType}</span></td>
                <td style={s.td}>
                  {e.threatLevel
                    ? <span style={s.chip(threatColor(e.threatLevel, C))}>{e.threatLevel}</span>
                    : <span style={{ color: C.textDim, fontSize: 11 }}>—</span>}
                </td>
                <td style={{ ...s.td, fontSize: 11, color: C.textDim, whiteSpace: "nowrap" }}>
                  {fmt(e.timestamp)} {fmtTime(e.timestamp)}
                </td>
                <td style={{ ...s.td, color: C.textMuted, maxWidth: 320, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                  {e.content}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {filtered.length === 0 && (
          <div style={{ color: C.textDim, fontSize: 12, padding: "20px 0", textAlign: "center" }}>
            Sin eventos {filter !== "all" ? `de tipo "${filter}"` : ""}.
          </div>
        )}
      </div>
    </div>
  );
}
