import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import DashboardLayout from '../components/DashboardLayout';
import Topbar from '../components/Topbar';

const Settings = () => {
  const { tenant, businessName } = useAuth();
  
  const getInitials = (name) => {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) || 'TN';
  };

  return (
    <DashboardLayout>
      <Topbar title="Ajustes" showAction={false} />
      <div className="flex-1 overflow-y-auto p-7">
        <div className="max-w-[560px]">
          <div className="font-display text-[15px] font-bold tracking-tight mb-3.5 px-1">Perfil de Empresa</div>
          
          <div className="bg-[var(--surface)] border border-[var(--border)] rounded-[var(--radius)] p-5 mb-5">
            <div className="flex items-center gap-5 mb-5 px-1">
              <div className="w-[52px] h-[52px] rounded-xl bg-gradient-to-br from-[var(--accent)] to-[#7C3AED] flex items-center justify-center text-white text-[18px] font-bold">
                {getInitials(businessName)}
              </div>
              <div>
                <div className="font-bold">{businessName || 'Cargando...'}</div>
                <div className="text-[13px] text-[var(--text-secondary)]">Tenant ID: {tenant}</div>
              </div>
            </div>

            <div className="mb-4">
              <label className="block text-[12px] font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-widest px-1">Nombre del Negocio</label>
              <input type="text" className="input-field" defaultValue={businessName} readOnly />
            </div>
            <div className="mb-4">
              <label className="block text-[12px] font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-widest px-1">ID del Sistema</label>
              <input type="text" className="input-field" defaultValue={tenant} readOnly />
            </div>
            
            <p className="text-[11px] text-[var(--text-muted)] mt-4 px-1 italic">
              * Para modificar estos datos, por favor contacte con soporte técnico.
            </p>
          </div>

          <div className="font-display text-[15px] font-bold tracking-tight mb-3.5 px-1">Preferencias del Dashboard</div>
          <div className="bg-[var(--surface)] border border-[var(--border)] rounded-[var(--radius)] p-5">
            <div className="mb-4">
              <label className="block text-[12px] font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-widest px-1">Zona horaria</label>
              <input type="text" className="input-field" defaultValue="America/Bogota (UTC-5)" readOnly />
            </div>
            <div>
              <label className="block text-[12px] font-semibold text-[var(--text-secondary)] mb-1.5 uppercase tracking-widest px-1">Idioma</label>
              <input type="text" className="input-field" defaultValue="Español (Colombia)" readOnly />
            </div>
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
};

export default Settings;
