import { useState, useEffect, useMemo } from "react";
import { ThemeContext, THEMES, makeStyles } from "./styles/tokens";
import Sidebar from "./components/Sidebar";
import { Toast } from "./components/Feedback";
import Dashboard from "./pages/Dashboard";
import TenantsPage from "./pages/TenantsPage";
import AuditPage from "./pages/AuditPage";
import SecurityPage from "./pages/SecurityPage";

export default function App() {
  const [page, setPage] = useState("dashboard");
  const [toast, setToast] = useState(null);
  const [theme, setTheme] = useState(() => localStorage.getItem("ra-theme") || "dark");

  const toggleTheme = () => {
    setTheme(t => {
      const next = t === "dark" ? "light" : "dark";
      localStorage.setItem("ra-theme", next);
      return next;
    });
  };

  const colors = THEMES[theme];
  const styles = useMemo(() => makeStyles(colors), [colors]);

  // Auto-dismiss toast
  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 4000);
    return () => clearTimeout(t);
  }, [toast]);

  const showToast = (message, type = "success") => setToast({ message, type });

  const themeCtx = useMemo(() => ({ theme, colors, styles, toggleTheme }), [theme, colors, styles]);

  return (
    <ThemeContext.Provider value={themeCtx}>
      <div style={styles.app}>
        <Sidebar page={page} setPage={setPage} />
        <main style={styles.main}>
          {page === "dashboard" && <Dashboard />}
          {page === "tenants"   && <TenantsPage onToast={showToast} />}
          {page === "audit"     && <AuditPage />}
          {page === "security"  && <SecurityPage />}
        </main>
        {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
      </div>
    </ThemeContext.Provider>
  );
}
