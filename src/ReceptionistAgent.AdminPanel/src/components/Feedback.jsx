import { useTheme } from "../styles/tokens";

export function LoadingState({ label = "Cargando..." }) {
  const { colors: C } = useTheme();
  return (
    <div className="fade-in" style={{ padding: "40px 0" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 20 }}>
        <div style={{
          width: 10, height: 10, borderRadius: "50%",
          background: C.accent, animation: "pulse 1.2s ease-in-out infinite",
        }} />
        <span style={{ fontSize: 12, color: C.textMuted, letterSpacing: "0.06em" }}>{label}</span>
      </div>
      {[...Array(4)].map((_, i) => (
        <div key={i} className="skeleton skeleton-block"
          style={{ width: `${70 + Math.random() * 30}%`, animationDelay: `${i * 0.15}s` }}
        />
      ))}
    </div>
  );
}

export function ErrorBanner({ message, onRetry }) {
  const { colors: C, styles: s } = useTheme();
  return (
    <div className="fade-in" style={{
      padding: "16px 20px", borderRadius: 8,
      background: C.redDim, border: "1px solid rgba(255,68,68,0.2)",
      display: "flex", alignItems: "center", justifyContent: "space-between",
    }}>
      <span style={{ fontSize: 12, color: C.red }}>⚠ {message}</span>
      {onRetry && (
        <button style={s.btn("danger")} onClick={onRetry}>Reintentar</button>
      )}
    </div>
  );
}

export function Toast({ message, type = "error", onClose }) {
  return (
    <div className={`toast toast--${type}`} onClick={onClose}>
      <span>{type === "error" ? "✕" : "✓"}</span>
      <span>{message}</span>
    </div>
  );
}
