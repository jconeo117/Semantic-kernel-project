import axios from "axios";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5083";
const API_KEY  = import.meta.env.VITE_API_KEY  || "";

const api = axios.create({
  baseURL: API_BASE,
  headers: {
    "Content-Type": "application/json",
    ...(API_KEY ? { "X-Api-Key": API_KEY } : {}),
  },
});

// ─── Tenants ──────────────────────────────────────────────────────

export async function fetchTenants() {
  const { data } = await api.get("/api/admin/tenants");
  return data; // { total, tenants: [...] }
}

export async function fetchTenantById(tenantId) {
  const { data } = await api.get(`/api/admin/tenants/${tenantId}`);
  return data; // { tenant, billing }
}

export async function createTenant(tenant) {
  const { data } = await api.post("/api/admin/tenants", tenant);
  return data;
}

export async function updateTenant(tenantId, tenant) {
  const { data } = await api.put(`/api/admin/tenants/${tenantId}`, tenant);
  return data;
}

export async function deleteTenant(tenantId) {
  const { data } = await api.delete(`/api/admin/tenants/${tenantId}`);
  return data;
}

// ─── Billing ──────────────────────────────────────────────────────

export async function fetchBilling(tenantId) {
  const { data } = await api.get(`/api/admin/tenants/${tenantId}/billing`);
  return data;
}

export async function suspendTenant(tenantId, reason = "Suspendido por administrador") {
  const { data } = await api.post(`/api/admin/tenants/${tenantId}/suspend`, { reason });
  return data;
}

export async function reactivateTenant(tenantId, activeUntil = null) {
  const { data } = await api.post(`/api/admin/tenants/${tenantId}/reactivate`, { activeUntil });
  return data;
}

// ─── Metrics ──────────────────────────────────────────────────────

export async function fetchMetrics(tenantId = null, from = null, to = null) {
  const params = {};
  if (tenantId) params.tenantId = tenantId;
  if (from) params.from = from;
  if (to) params.to = to;
  const { data } = await api.get("/api/admin/metrics", { params });
  return data; // MetricsSummary
}

// ─── Audit ────────────────────────────────────────────────────────

export async function fetchRecentAudit(tenantId = null, limit = 50) {
  const params = { limit };
  if (tenantId) params.tenantId = tenantId;
  const { data } = await api.get("/api/Audit/recent", { params });
  return data; // { tenantId, totalEntries, entries: [...] }
}

export async function fetchSecurityEvents(tenantId = null, from = null, to = null) {
  const params = {};
  if (tenantId) params.tenantId = tenantId;
  if (from) params.from = from;
  if (to) params.to = to;
  const { data } = await api.get("/api/Audit/security", { params });
  return data; // { tenantId, period, totalEvents, events: [...] }
}

// ─── Database Explorer ────────────────────────────────────────────

export async function fetchDatabaseSchema(tenantId) {
  const { data } = await api.get(`/api/admin/tenants/${tenantId}/database/schema`);
  return data; // { tenantId, dbType, tables, bookingsSchema }
}

export async function fetchDatabaseBookings(tenantId, limit = 50) {
  const { data } = await api.get(`/api/admin/tenants/${tenantId}/database/bookings`, { params: { limit } });
  return data; // { tenantId, totalBookings, showing, bookings }
}

export default api;
