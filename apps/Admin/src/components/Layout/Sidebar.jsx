import { Link, useLocation } from 'react-router-dom';
import { ClipboardList, Kanban } from 'lucide-react';
import { cn } from '@/lib/utils';

const menuItems = [
  { title: 'Diagnósticos', href: '/diagnosticos', icon: ClipboardList },
  { title: 'Kanban',       href: '/kanban',        icon: Kanban },
];

export function Sidebar() {
  const location = useLocation();

  return (
    <div className="flex h-full w-64 flex-col bg-sidebar border-r border-sidebar-border">
      {/* Logo */}
      <div className="flex h-16 items-center px-6 border-b border-sidebar-border">
        <div className="flex items-center space-x-2">
          <svg
            viewBox="0 0 24 24"
            aria-hidden="true"
            className="h-8 w-8 text-sidebar-primary"
            fill="currentColor"
          >
            <path d="M3 20V4h4l5 5 5-5h4v16h-4V10l-5 5-5-5v10H3z" />
          </svg>
          <span className="text-lg font-semibold text-sidebar-foreground">
            Diagnóstico 5D
          </span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 space-y-1 p-4 overflow-y-auto">
        {menuItems.map((item) => {
          const Icon = item.icon;
          const isActive =
            location.pathname === item.href ||
            location.pathname.startsWith(item.href);

          return (
            <Link
              key={item.href}
              to={item.href}
              className={cn(
                'flex items-center space-x-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                  : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
              )}
            >
              <Icon className="h-5 w-5" />
              <span>{item.title}</span>
            </Link>
          );
        })}
      </nav>

      {/* Footer */}
      <div className="p-4 border-t border-sidebar-border">
        <a
          href="https://malachdigital.com.br/"
          target="_blank"
          rel="noreferrer"
          className="flex items-center gap-2 text-xs text-sidebar-foreground/70 hover:text-sidebar-foreground"
        >
          <svg
            viewBox="0 0 24 24"
            aria-hidden="true"
            className="h-4 w-4"
            fill="currentColor"
          >
            <path d="M3 20V4h4l5 5 5-5h4v16h-4V10l-5 5-5-5v10H3z" />
          </svg>
          <span>Malach Digital</span>
        </a>
      </div>
    </div>
  );
}
