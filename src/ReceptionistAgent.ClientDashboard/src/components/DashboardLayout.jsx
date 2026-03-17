import React from 'react';
import Sidebar from './Sidebar';

const DashboardLayout = ({ children }) => {
  return (
    <div className="flex h-screen overflow-hidden bg-[var(--bg)] font-body text-[var(--text-primary)]">
      <Sidebar />
      <main className="flex-1 flex flex-col overflow-hidden">
        {children}
      </main>
    </div>
  );
};

export default DashboardLayout;
