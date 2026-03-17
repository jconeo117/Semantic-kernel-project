import React from 'react';
import { Bell, Search, Plus } from 'lucide-react';

const Topbar = ({ title, showAction = true, actionLabel = "Nuevo", onAction }) => {
  return (
    <div className="h-[60px] bg-[var(--surface)] border-b border-[var(--border)] px-7 flex items-center justify-between shrink-0">
      <div className="font-display text-[17px] font-bold tracking-tight text-[var(--text-primary)]">
        {title}
      </div>
      
      <div className="flex items-center gap-2">
        <button className="w-[34px] h-[34px] border border-[var(--border)] rounded-lg bg-transparent flex items-center justify-center cursor-pointer transition-all duration-[var(--transition)] text-[var(--text-secondary)] hover:bg-[var(--bg)] hover:border-[var(--text-muted)] hover:text-[var(--text-primary)]">
          <Bell size={16} />
        </button>
        <button className="w-[34px] h-[34px] border border-[var(--border)] rounded-lg bg-transparent flex items-center justify-center cursor-pointer transition-all duration-[var(--transition)] text-[var(--text-secondary)] hover:bg-[var(--bg)] hover:border-[var(--text-muted)] hover:text-[var(--text-primary)]">
          <Search size={16} />
        </button>
        
        {showAction && (
          <button 
            onClick={onAction}
            className="px-3.5 py-1.5 bg-[var(--accent)] text-white border-none rounded-[var(--radius-sm)] font-body text-[13px] font-medium cursor-pointer flex items-center gap-1.5 transition-all duration-[var(--transition)] hover:bg-[var(--accent-hover)]"
          >
            <Plus size={13} />
            {actionLabel}
          </button>
        )}
      </div>
    </div>
  );
};

export default Topbar;
