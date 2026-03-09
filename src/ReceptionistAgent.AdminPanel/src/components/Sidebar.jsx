import { useTheme } from "../styles/tokens";

export default function Sidebar({ page, setPage }) {
  const { colors: C, styles: s, theme, toggleTheme } = useTheme();

  const PAGES = [
    { id: "dashboard", label: "Dashboard" },
    { id: "tenants",   label: "Tenants" },
    { id: "audit",     label: "Auditoría" },
    { id: "security",  label: "Seguridad" },
  ];

  return (
    <div style={s.sidebar}>
      <div style={s.logo}>
        <div style={s.logoText}>Receptionist<span style={{ color: C.text }}>AI</span></div>
        <div style={s.logoSub}>Admin Panel · v1.0</div>
      </div>
      <nav style={s.nav}>
        {PAGES.map(p => (
          <div key={p.id} style={s.navItem(page === p.id)} onClick={() => setPage(p.id)}>
            <div style={s.navDot(page === p.id)} />
            {p.label}
          </div>
        ))}
      </nav>

      {/* Theme toggle */}
      <div style={{ padding: "12px 16px" }}>
        <button
          onClick={toggleTheme}
          style={{
            ...s.btn(),
            width: "100%",
            justifyContent: "center",
            fontSize: 10,
            padding: "8px 12px",
            gap: 6,
          }}
        >
          {theme === "dark" ? "☀ Modo Claro" : "🌙 Modo Oscuro"}
        </button>
      </div>

      <div style={{ padding: "12px 24px 16px", borderTop: `1px solid ${C.border}` }}>
        <div style={{ fontSize: 10, color: C.textDim, letterSpacing: "0.06em" }}>BASE URL</div>
        <div style={{ fontSize: 10, color: C.textMuted, marginTop: 4, wordBreak: "break-all" }}>
          {import.meta.env.VITE_API_BASE_URL || "localhost:5083"}
        </div>
      </div>
    </div>
  );
}
