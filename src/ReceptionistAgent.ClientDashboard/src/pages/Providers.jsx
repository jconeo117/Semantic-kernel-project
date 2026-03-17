import React, { useState, useEffect } from 'react';
import axios from 'axios';
import DashboardLayout from '../components/DashboardLayout';
import Topbar from '../components/Topbar';

const Providers = () => {
  const [providers, setProviders] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchProviders = async () => {
      try {
        const res = await axios.get('/api/dashboard/providers');
        setProviders(res.data);
      } catch (err) {
        console.error('Error fetching providers', err);
      } finally {
        setLoading(false);
      }
    };
    fetchProviders();
  }, []);

  const getInitials = (name) => {
    return name?.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) || '??';
  };

  const getRandomGradient = (id) => {
    const gradients = [
      'from-[#2D5BE3] to-[#7C3AED]',
      'from-[#059669] to-[#10B981]',
      'from-[#F59E0B] to-[#EF4444]',
      'from-[#DC2626] to-[#F87171]',
      'from-[#7C3AED] to-[#C026D3]'
    ];
    // Use id length or sum of chars to pick a stable gradient
    const index = id ? id.length % gradients.length : 0;
    return gradients[index];
  };

  if (loading) {
    return (
      <DashboardLayout>
        <Topbar title="Proveedores" />
        <div className="flex-1 flex items-center justify-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-[var(--accent)]"></div>
        </div>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout>
      <Topbar title="Proveedores" />
      <div className="flex-1 overflow-y-auto p-7">
        <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-3 gap-4 mb-6">
          {[
            { label: 'Total proveedores', value: providers.length },
            { label: 'Activos', value: providers.filter(p => p.isAvailable).length },
            { label: 'En línea', value: providers.filter(p => p.isAvailable).length },
          ].map((stat) => (
            <div key={stat.label} className="bg-[var(--surface)] border border-[var(--border)] rounded-[var(--radius)] p-5 transition-shadow duration-[var(--transition)] hover:shadow-[var(--shadow)]">
              <div className="text-[12px] text-[var(--text-secondary)] font-medium tracking-[0.2px] mb-2">{stat.label}</div>
              <div className="font-display text-[28px] font-bold tracking-[-1px] mb-1.5">{stat.value}</div>
            </div>
          ))}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {providers.map((provider) => (
            <div key={provider.id} className="bg-[var(--surface)] border border-[var(--border)] rounded-[var(--radius)] p-5 text-center transition-all duration-[var(--transition)] cursor-pointer hover:shadow-[var(--shadow)] hover:-translate-y-[1px]">
              <div className={`w-[52px] h-[52px] rounded-xl mx-auto mb-3 flex items-center justify-center text-white text-[20px] font-bold bg-gradient-to-br ${getRandomGradient(provider.id)}`}>
                {getInitials(provider.name)}
              </div>
              <div className="text-[14px] font-bold mb-0.5">{provider.name}</div>
              <div className="text-[12px] text-[var(--text-secondary)] mb-3">{provider.role || 'Proveedor'}</div>
              <div className="text-[var(--amber)] text-[12px] tracking-[1px] mb-3">★★★★★</div>
              <div className="flex justify-center gap-5 pt-3 border-t border-[var(--border)]">
                <div className="text-center">
                  <div className="font-display text-[16px] font-bold">{Math.floor(Math.random() * 50) + 10}</div>
                  <div className="text-[10px] text-[var(--text-muted)]">Trabajos</div>
                </div>
                <div className="text-center">
                  <div className="font-display text-[16px] font-bold">4.9</div>
                  <div className="text-[10px] text-[var(--text-muted)]">Rating</div>
                </div>
                <div className="text-center">
                  <div className="font-display text-[16px] font-bold">98%</div>
                  <div className="text-[10px] text-[var(--text-muted)]">Puntual</div>
                </div>
              </div>
            </div>
          ))}
          {providers.length === 0 && (
            <div className="col-span-full py-12 text-center text-[var(--text-muted)] border-2 border-dashed border-[var(--border)] rounded-[var(--radius)]">
              No hay proveedores configurados.
            </div>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
};

export default Providers;
