import { useState } from "react";
import { useTheme, businessIcon, planColor, statusColor, fmt } from "../styles/tokens";
import DatabaseExplorer from "./DatabaseExplorer";

const TABS = [
  { id: "info", label: "Información" },
  { id: "database", label: "Base de Datos" },
];

export default function TenantModal({ data, onClose, onSuspend, onReactivate }) {
  const { colors: C, styles: s } = useTheme();
  const { tenant, billing } = data;
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState("info");

  const handleSuspend = async () => {
    setLoading(true);
    try { await onSuspend(tenant.tenantId); onClose(); }
    catch { setLoading(false); }
  };

  const handleReactivate = async () => {
    setLoading(true);
    try { await onReactivate(tenant.tenantId); onClose(); }
    catch { setLoading(false); }
  };

  return (
    <div style={s.modal} onClick={onClose}>
      <div style={{ ...s.modalBox, width: activeTab === "database" ? 700 : 500 }} onClick={e => e.stopPropagation()} className="fade-in">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 20 }}>
          <div>
            <div style={{ fontSize: 18, fontWeight: 700, letterSpacing: "-0.02em" }}>
              {businessIcon(tenant.businessType)} {tenant.businessName}
            </div>
            <div style={{ fontSize: 11, color: C.textMuted, marginTop: 4, fontFamily: "monospace" }}>{tenant.tenantId}</div>
          </div>
          <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
            <span style={s.chip(planColor(billing.planType, C))}>{billing.planType}</span>
            <span style={s.chip(statusColor(billing.billingStatus, C))}>{billing.billingStatus}</span>
          </div>
        </div>

        <div style={{ display: "flex", gap: 4, marginBottom: 16 }}>
          {TABS.map(tab => (
            <button key={tab.id} style={{
              ...s.btn(), fontSize: 10,
              background: activeTab === tab.id ? C.accentDim : C.surfaceHover,
              color: activeTab === tab.id ? C.accent : C.textMuted,
              border: `1px solid ${activeTab === tab.id ? C.accentBorder : "transparent"}`,
            }} onClick={() => setActiveTab(tab.id)}>
              {tab.id === "database" ? "🗄 " : "ℹ "}{tab.label}
            </button>
          ))}
        </div>

        <div style={s.divider} />

        {activeTab === "info" && (
          <div className="fade-in">
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14, marginBottom: 16 }}>
              {[
                { label: "Dirección", value: tenant.address || "—" },
                { label: "Teléfono", value: tenant.phone || "—" },
                { label: "Horarios", value: tenant.workingHours },
                { label: "Zona horaria", value: tenant.timeZoneId },
                { label: "Activo hasta", value: billing.activeUntil ? fmt(billing.activeUntil) : "Sin límite" },
                { label: "Creado", value: fmt(tenant.createdAt) },
                { label: "Tipo de DB", value: tenant.dbType || "InMemory" },
                { label: "Connection String", value: tenant.connectionString ? "••••••••" : "—" },
              ].map(f => (
                <div key={f.label}>
                  <div style={{ fontSize: 10, color: C.textDim, letterSpacing: "0.08em", textTransform: "uppercase", marginBottom: 3 }}>{f.label}</div>
                  <div style={{ fontSize: 12 }}>{f.value}</div>
                </div>
              ))}
            </div>
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontSize: 10, color: C.textDim, letterSpacing: "0.08em", textTransform: "uppercase", marginBottom: 8 }}>Servicios</div>
              <div>{(tenant.services || []).map(sv => <span key={sv} style={s.tag}>{sv}</span>)}</div>
            </div>
            <div style={{ marginBottom: 20 }}>
              <div style={{ fontSize: 10, color: C.textDim, letterSpacing: "0.08em", textTransform: "uppercase", marginBottom: 10 }}>
                Providers ({(tenant.providers || []).length})
              </div>
              {(tenant.providers || []).map(p => (
                <div key={p.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "8px 0", borderBottom: `1px solid ${C.border}` }}>
                  <div>
                    <div style={{ fontSize: 12, fontWeight: 500 }}>{p.name}</div>
                    <div style={{ fontSize: 10, color: C.textMuted }}>{p.role}</div>
                  </div>
                  <div style={{ textAlign: "right" }}>
                    <div style={{ fontSize: 10, color: C.textDim }}>{p.startTime} – {p.endTime}</div>
                    <div style={{ fontSize: 10, color: C.textDim }}>{p.slotDurationMinutes} min/slot</div>
                  </div>
                </div>
              ))}
            </div>
            <div style={{ background: C.surfaceHover, border: `1px solid ${C.border}`, borderRadius: 6, padding: "8px 12px", marginBottom: 20 }}>
              <div style={{ fontSize: 10, color: C.textDim, marginBottom: 3 }}>WEBHOOK</div>
              <div style={{ fontSize: 11, color: C.textMuted, fontFamily: "monospace" }}>/api/twilio/{tenant.tenantId}</div>
            </div>
          </div>
        )}

        {activeTab === "database" && (
          <div className="fade-in" style={{ marginBottom: 20 }}>
            <DatabaseExplorer tenantId={tenant.tenantId} dbType={tenant.dbType} />
          </div>
        )}

        <div style={s.divider} />
        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
          {billing.billingStatus === "Active" ? (
            <button style={s.btn("danger")} onClick={handleSuspend} disabled={loading}>
              {loading ? "..." : "Suspender"}
            </button>
          ) : (
            <button style={s.btn("accent")} onClick={handleReactivate} disabled={loading}>
              {loading ? "..." : "Reactivar"}
            </button>
          )}
          <button style={s.btn()} onClick={onClose}>Cerrar</button>
        </div>
      </div>
    </div>
  );
}
