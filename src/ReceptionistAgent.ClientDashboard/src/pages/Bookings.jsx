import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { format } from 'date-fns';
import { es } from 'date-fns/locale';
import DashboardLayout from '../components/DashboardLayout';
import Topbar from '../components/Topbar';

const Bookings = () => {
  const [bookings, setBookings] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [bookingsRes, statsRes] = await Promise.all([
          axios.get('/api/dashboard/bookings'),
          axios.get('/api/dashboard/stats')
        ]);
        setBookings(bookingsRes.data);
        setStats(statsRes.data);
      } catch (err) {
        console.error('Error fetching bookings data', err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  const getStatusStyle = (status) => {
    switch (status) {
      case 1: // Confirmed
      case 'Confirmed':
        return 'bg-green-light text-[#16A34A]';
      case 2: // Cancelled
      case 'Cancelled':
        return 'bg-red-100 text-[#EF4444]';
      case 0: // Scheduled
      case 'Scheduled':
      default:
        return 'bg-[#FEF3C7] text-[#D97706]';
    }
  };

  const getStatusLabel = (status) => {
    switch (status) {
      case 1: case 'Confirmed': return 'Confirmada';
      case 2: case 'Cancelled': return 'Cancelada';
      case 3: case 'Completed': return 'Completada';
      case 0: case 'Scheduled': default: return 'Pendiente';
    }
  };

  if (loading) {
    return (
      <DashboardLayout>
        <Topbar title="Reservas" />
        <div className="flex-1 flex items-center justify-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-[var(--accent)]"></div>
        </div>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout>
      <Topbar title="Reservas" />
      <div className="flex-1 overflow-y-auto p-7">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {[
            { label: 'Total Reservas', value: stats?.totalBookings || '0' },
            { label: 'Pendientes', value: stats?.pendingBookings || '0', highlight: true },
            { label: 'Sesiones Activas', value: stats?.activeSessions || '0' },
            { label: 'Proveedores', value: stats?.providerCount || '0' },
          ].map((stat) => (
            <div key={stat.label} className="bg-[var(--surface)] border border-[var(--border)] rounded-[var(--radius)] p-5 transition-shadow duration-[var(--transition)] hover:shadow-[var(--shadow)]">
              <div className="text-[12px] text-[var(--text-secondary)] font-medium tracking-[0.2px] mb-2">{stat.label}</div>
              <div className={`font-display text-[28px] font-bold tracking-[-1px] ${stat.highlight ? 'text-[var(--accent)]' : ''}`}>{stat.value}</div>
            </div>
          ))}
        </div>

        <div className="flex items-center gap-1.5 mb-4">
          {['Todas', 'Hoy', 'Esta semana', 'Pendientes', 'Canceladas'].map((chip, i) => (
            <span key={chip} className={`px-2.5 py-1 rounded-md text-[12px] font-medium border cursor-pointer transition-all duration-[var(--transition)] ${i === 0 ? 'bg-[var(--accent-light)] border-[var(--accent)] text-[var(--accent)]' : 'bg-[var(--bg)] border-[var(--border)] text-[var(--text-secondary)] hover:bg-[var(--accent-light)] hover:border-[var(--accent)] hover:text-[var(--accent)]'}`}>
              {chip}
            </span>
          ))}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {bookings.map((booking) => (
            <div key={booking.id} className="bg-[var(--surface)] border border-[var(--border)] rounded-[var(--radius)] p-4.5 transition-all duration-[var(--transition)] cursor-pointer hover:shadow-[var(--shadow)] hover:border-[var(--accent)] hover:-translate-y-[1px]">
              <div className="flex items-start justify-between mb-3">
                <span className="text-[11px] text-[var(--text-muted)] font-medium">#{booking.confirmationCode}</span>
                <span className={`px-2 py-0.5 rounded-md text-[11px] font-medium ${getStatusStyle(booking.status)}`}>
                  {getStatusLabel(booking.status)}
                </span>
              </div>
              <div className="text-[14px] font-bold mb-1 truncate">{booking.clientName || 'Cliente sin nombre'}</div>
              <div className="text-[12px] text-[var(--text-secondary)] mb-3">Con: {booking.providerName}</div>
              <div className="flex gap-3 text-[12px] text-[var(--text-secondary)]">
                <span className="flex items-center gap-1.5">
                  {format(new Date(booking.scheduledDate), "dd MMM", { locale: es })}
                </span>
                <span className="flex items-center gap-1.5">
                  {booking.scheduledTime.slice(0, 5)}
                </span>
              </div>
            </div>
          ))}
          {bookings.length === 0 && (
            <div className="col-span-full py-12 text-center text-[var(--text-muted)] border-2 border-dashed border-[var(--border)] rounded-[var(--radius)]">
              No hay reservas registradas.
            </div>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
};

export default Bookings;
