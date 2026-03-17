import React from 'react';
import DashboardLayout from '../components/DashboardLayout';
import Topbar from '../components/Topbar';
import { Database as DbIcon, Code, Play } from 'lucide-react';

const Database = () => {
  const tables = ['bookings', 'providers', 'clients', 'messages', 'services', 'invoices'];

  return (
    <DashboardLayout>
      <Topbar title="Base de Datos" actionLabel="Ejecutar" />
      <div className="flex-1 overflow-hidden flex p-7 gap-5">
        <div className="w-[200px] shrink-0 border border-[var(--border)] bg-[var(--surface)] rounded-[var(--radius)] overflow-hidden flex flex-col">
          <div className="px-3.5 py-3 text-[11px] font-semibold tracking-widest uppercase text-[var(--text-muted)] border-bottom border-[var(--border)]">Tablas</div>
          <div className="flex-1 overflow-y-auto">
            {tables.map((t, i) => (
              <div key={t} className={`px-3.5 py-2.5 text-[13px] border-b border-[var(--border)] last:border-none flex items-center gap-2 cursor-pointer transition-all duration-[var(--transition)] hover:bg-[var(--bg)] ${i === 0 ? 'bg-[var(--accent-light)] text-[var(--accent)] font-medium' : 'text-[var(--text-primary)]'}`}>
                <DbIcon size={13} className="opacity-50" />
                {t}
              </div>
            ))}
          </div>
        </div>

        <div className="flex-1 min-w-0 flex flex-col gap-4">
          <div className="border border-[var(--border)] bg-[var(--surface)] rounded-[var(--radius)] overflow-hidden">
            <div className="px-4 py-2.5 bg-[var(--bg)] border-b border-[var(--border)] flex items-center gap-2">
              <Code size={13} className="text-[var(--accent)]" />
              <div className="flex-1 text-[11px] font-semibold tracking-widest uppercase text-[var(--text-muted)]">Query Editor</div>
              <button className="px-3 py-1 bg-[var(--accent)] text-white border-none rounded-[var(--radius-sm)] text-[12px] flex items-center gap-1.5 cursor-pointer">
                <Play size={11} fill="currentColor" />
                Ejecutar
              </button>
            </div>
            <textarea 
              readOnly
              className="w-full px-4 py-3.5 h-[100px] font-mono text-[13px] text-[var(--accent)] bg-[#FAFAF8] border-none outline-none resize-none leading-relaxed"
              value="SELECT * FROM bookings ORDER BY created_at DESC LIMIT 10;"
            />
          </div>

          <div className="flex-1 border border-[var(--border)] bg-[var(--surface)] rounded-[var(--radius)] overflow-hidden flex flex-col">
            <div className="px-5 py-4 border-b border-[var(--border)] flex items-center justify-between">
              <div className="font-display text-[14px] font-bold">bookings <span className="font-body text-[12px] text-[var(--text-muted)] font-normal ml-1">— 2,841 registros</span></div>
              <div className="flex items-center gap-1 text-[var(--green)] text-[12px]">
                <span className="w-1.5 h-1.5 bg-[var(--green)] rounded-full"></span>
                Conectado
              </div>
            </div>
            <div className="flex-1 overflow-auto">
              <table className="w-full border-collapse">
                <thead>
                  <tr className="bg-[var(--bg)]">
                    {['id', 'cliente', 'servicio', 'fecha', 'estado'].map((h) => (
                      <th key={h} className="px-5 py-2.5 text-left text-[11px] font-semibold tracking-widest uppercase text-[var(--text-muted)] border-b border-[var(--border)]">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {[1, 2, 3, 4, 5].map((i) => (
                    <tr key={i} className="hover:bg-[var(--bg)] transition-colors">
                      <td className="px-5 py-3 text-[13px] border-b border-[var(--border)] font-mono">#284{i}</td>
                      <td className="px-5 py-3 text-[13px] border-b border-[var(--border)]">TechCorp S.A.</td>
                      <td className="px-5 py-3 text-[13px] border-b border-[var(--border)]">Limpieza</td>
                      <td className="px-5 py-3 text-[13px] border-b border-[var(--border)] text-[var(--text-secondary)]">2026-03-14</td>
                      <td className="px-5 py-3 text-[13px] border-b border-[var(--border)]">
                        <span className="px-2 py-0.5 rounded-md bg-green-light text-[#16A34A] text-[11px] font-medium">Confirmada</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
};

export default Database;
