import { createContext, useContext } from "react";

// ─── Theme palettes ───────────────────────────────────────────────
const DARK = {
  bg: "#0a0a0a",
  surface: "#111111",
  surfaceHover: "#161616",
  border: "#1e1e1e",
  borderLight: "#2a2a2a",
  text: "#e8e8e8",
  textMuted: "#666",
  textDim: "#444",
  accent: "#e8ff47",
  accentDim: "rgba(232,255,71,0.08)",
  accentBorder: "rgba(232,255,71,0.2)",
  red: "#ff4444",
  redDim: "rgba(255,68,68,0.08)",
  green: "#44ff88",
  greenDim: "rgba(68,255,136,0.08)",
  blue: "#4488ff",
  blueDim: "rgba(68,136,255,0.08)",
};

const LIGHT = {
  bg: "#f4f5f7",
  surface: "#ffffff",
  surfaceHover: "#f0f1f3",
  border: "#e2e4e9",
  borderLight: "#d1d5db",
  text: "#1a1a1a",
  textMuted: "#6b7280",
  textDim: "#9ca3af",
  accent: "#7c3aed",          // vivid purple for light mode
  accentDim: "rgba(124,58,237,0.08)",
  accentBorder: "rgba(124,58,237,0.2)",
  red: "#dc2626",
  redDim: "rgba(220,38,38,0.06)",
  green: "#16a34a",
  greenDim: "rgba(22,163,74,0.06)",
  blue: "#2563eb",
  blueDim: "rgba(37,99,235,0.06)",
};

export const THEMES = { dark: DARK, light: LIGHT };

// ─── React context ────────────────────────────────────────────────
export const ThemeContext = createContext({
  theme: "dark",
  colors: DARK,
  toggleTheme: () => {},
});

export const useTheme = () => useContext(ThemeContext);

// ─── Styles factory (depends on current colors) ───────────────────
export function makeStyles(C) {
  return {
    app: { background: C.bg, minHeight: "100vh", fontFamily: "'DM Mono', 'Fira Code', monospace", color: C.text, display: "flex" },
    sidebar: { width: 220, minHeight: "100vh", background: C.surface, borderRight: `1px solid ${C.border}`, display: "flex", flexDirection: "column", padding: "28px 0", position: "fixed", top: 0, left: 0, bottom: 0, zIndex: 10 },
    logo: { padding: "0 24px 32px", borderBottom: `1px solid ${C.border}` },
    logoText: { fontSize: 13, fontWeight: 700, letterSpacing: "0.08em", color: C.accent, textTransform: "uppercase" },
    logoSub: { fontSize: 10, color: C.textDim, marginTop: 4, letterSpacing: "0.1em" },
    nav: { padding: "20px 12px", flex: 1 },
    navItem: (active) => ({
      display: "flex", alignItems: "center", gap: 10, padding: "9px 12px",
      borderRadius: 6, cursor: "pointer", fontSize: 12, letterSpacing: "0.05em",
      color: active ? C.accent : C.textMuted, background: active ? C.accentDim : "transparent",
      border: `1px solid ${active ? C.accentBorder : "transparent"}`,
      marginBottom: 2, transition: "all 0.15s", fontWeight: active ? 600 : 400,
    }),
    navDot: (active) => ({ width: 6, height: 6, borderRadius: "50%", background: active ? C.accent : C.textDim, flexShrink: 0 }),
    main: { marginLeft: 220, flex: 1, padding: "32px 36px", maxWidth: "calc(100vw - 220px)" },
    header: { display: "flex", alignItems: "baseline", justifyContent: "space-between", marginBottom: 32 },
    title: { fontSize: 22, fontWeight: 700, letterSpacing: "-0.02em", color: C.text },
    subtitle: { fontSize: 12, color: C.textMuted, marginTop: 4, letterSpacing: "0.03em" },
    grid2: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 },
    grid3: { display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 16 },
    grid4: { display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr", gap: 12 },
    card: { background: C.surface, border: `1px solid ${C.border}`, borderRadius: 10, padding: 20 },
    cardLabel: { fontSize: 10, color: C.textDim, letterSpacing: "0.12em", textTransform: "uppercase", marginBottom: 10 },
    statNum: { fontSize: 32, fontWeight: 700, letterSpacing: "-0.03em", lineHeight: 1 },
    statSub: { fontSize: 11, color: C.textMuted, marginTop: 6 },
    chip: (color) => ({ display: "inline-block", fontSize: 10, fontWeight: 600, letterSpacing: "0.08em", textTransform: "uppercase", color, padding: "2px 8px", borderRadius: 4, background: color + "15", border: `1px solid ${color}30` }),
    row: { display: "flex", alignItems: "center", gap: 10 },
    divider: { height: 1, background: C.border, margin: "16px 0" },
    btn: (variant = "default") => ({
      display: "inline-flex", alignItems: "center", gap: 6,
      padding: variant === "accent" ? "8px 16px" : "6px 12px",
      borderRadius: 6, fontSize: 11, fontWeight: 600, letterSpacing: "0.06em",
      cursor: "pointer", border: "none", transition: "all 0.15s",
      background: variant === "accent" ? C.accent : variant === "danger" ? C.redDim : C.surfaceHover,
      color: variant === "accent" ? (C === DARK ? "#000" : "#fff") : variant === "danger" ? C.red : C.textMuted,
      textTransform: "uppercase",
    }),
    table: { width: "100%", borderCollapse: "collapse" },
    th: { textAlign: "left", fontSize: 10, color: C.textDim, letterSpacing: "0.1em", textTransform: "uppercase", padding: "0 0 12px", borderBottom: `1px solid ${C.border}`, fontWeight: 500 },
    td: { padding: "12px 0", borderBottom: `1px solid ${C.border}`, fontSize: 12, color: C.text, verticalAlign: "middle" },
    pill: { display: "inline-block", width: 8, height: 8, borderRadius: "50%", marginRight: 6 },
    input: { background: C.surfaceHover, border: `1px solid ${C.borderLight}`, borderRadius: 6, padding: "8px 12px", color: C.text, fontSize: 12, fontFamily: "inherit", outline: "none", width: "100%", boxSizing: "border-box" },
    modal: { position: "fixed", inset: 0, background: C === DARK ? "rgba(0,0,0,0.8)" : "rgba(0,0,0,0.4)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 100 },
    modalBox: { background: C.surface, border: `1px solid ${C.border}`, borderRadius: 12, padding: 28, width: 500, maxHeight: "85vh", overflowY: "auto" },
    tag: { display: "inline-block", fontSize: 10, padding: "2px 6px", borderRadius: 3, background: C.surfaceHover, color: C.textMuted, marginRight: 4, marginBottom: 4 },
    barChart: { display: "flex", alignItems: "flex-end", gap: 6, height: 64 },
    bar: (pct, highlight) => ({ flex: 1, height: `${Math.max(pct * 100, 6)}%`, background: highlight ? C.accent : C.borderLight, borderRadius: 2, transition: "all 0.3s" }),
  };
}

// ─── Legacy exports (for backward compat during migration) ────────
// Components should now use: const { colors: C, styles: s } = useTheme();
// but we export defaults for non-component code.
export const C = DARK;
export const s = makeStyles(DARK);

// ─── Tiny utils (theme-independent) ───────────────────────────────
export const planColor = (p, C) => p === "Pro" ? C.accent : p === "Basic" ? C.blue : C.textMuted;
export const statusColor = (st, C) => st === "Active" ? C.green : st === "Suspended" ? C.red : C.textMuted;
export const threatColor = (t, C) => t === "High" ? C.red : t === "Medium" ? "#ffaa44" : C.textMuted;
export const typeColor = (t, C) => t === "SecurityBlock" ? C.red : t === "OutputFiltered" ? "#ffaa44" : t === "AgentResponse" ? C.green : C.textMuted;
export const businessIcon = (type) => ({ clinic: "⚕", salon: "✂", wellness: "🌿", nails: "💅" }[type] || "◆");
export const fmt = (d) => new Date(d).toLocaleDateString("es-CO", { day: "2-digit", month: "short", year: "numeric" });
export const fmtTime = (d) => new Date(d).toLocaleTimeString("es-CO", { hour: "2-digit", minute: "2-digit" });
