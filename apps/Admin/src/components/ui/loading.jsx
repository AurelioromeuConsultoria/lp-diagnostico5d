import { Loader2 } from 'lucide-react';

export function Loading({ className, size = 'default', text = 'Carregando...' }) {
  const sizeClasses = {
    sm: 'h-4 w-4',
    default: 'h-6 w-6',
    lg: 'h-8 w-8',
  };

  return (
    <div className={`flex items-center justify-center space-x-2 ${className || ''}`}>
      <Loader2 className={`animate-spin ${sizeClasses[size]}`} />
      {text && <span className="text-sm text-gray-500">{text}</span>}
    </div>
  );
}

export function LoadingPage({ text = 'Carregando...' }) {
  return (
    <div className="flex h-64 items-center justify-center">
      <Loading size="lg" text={text} />
    </div>
  );
}

export default Loading;
