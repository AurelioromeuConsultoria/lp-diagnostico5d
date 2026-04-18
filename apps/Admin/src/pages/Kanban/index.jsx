import { useState, useEffect, useRef } from 'react';
import { diagnosticoApi } from '@/api/diagnostico';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { toast } from 'sonner';
import { RefreshCw, Plus, MessageCircle, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Link } from 'react-router-dom';

// ─── Configuração das colunas ─────────────────────────────────────────────────

const COLUNAS = [
  { fase: 'novo',             label: 'Novo',               color: 'bg-slate-500',   light: 'bg-slate-50 dark:bg-slate-900/30',   border: 'border-slate-300 dark:border-slate-700' },
  { fase: 'convite_enviado',  label: 'Convite enviado',    color: 'bg-blue-500',    light: 'bg-blue-50 dark:bg-blue-900/20',     border: 'border-blue-200 dark:border-blue-800'  },
  { fase: 'em_preenchimento', label: 'Em preenchimento',   color: 'bg-amber-500',   light: 'bg-amber-50 dark:bg-amber-900/20',   border: 'border-amber-200 dark:border-amber-800'},
  { fase: 'aguardando_analise', label: 'Aguardando análise', color: 'bg-orange-500', light: 'bg-orange-50 dark:bg-orange-900/20', border: 'border-orange-200 dark:border-orange-800'},
  { fase: 'em_analise',       label: 'Em análise',         color: 'bg-purple-500',  light: 'bg-purple-50 dark:bg-purple-900/20', border: 'border-purple-200 dark:border-purple-800'},
  { fase: 'diagnosticado',    label: 'Diagnosticado',      color: 'bg-indigo-500',  light: 'bg-indigo-50 dark:bg-indigo-900/20', border: 'border-indigo-200 dark:border-indigo-800'},
  { fase: 'concluido',        label: 'Concluído',          color: 'bg-green-500',   light: 'bg-green-50 dark:bg-green-900/20',   border: 'border-green-200 dark:border-green-800'},
];

// ─── Helpers ──────────────────────────────────────────────────────────────────

function initials(nome) {
  return (nome || '?').split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
}

function fmtDateShort(dt) {
  if (!dt) return '';
  return new Date(dt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
}

function getColuna(fase) {
  return COLUNAS.find(c => c.fase === fase) ?? COLUNAS[0];
}

// ─── NovoConvidadoDialog ──────────────────────────────────────────────────────

function NovoConvidadoDialog({ open, onOpenChange, onCreated }) {
  const [nome, setNome] = useState('');
  const [whatsapp, setWhatsapp] = useState('');
  const [saving, setSaving] = useState(false);

  const reset = () => { setNome(''); setWhatsapp(''); };

  const save = async () => {
    if (!nome.trim()) return;
    setSaving(true);
    try {
      await diagnosticoApi.criarConvidado({ nome, whatsapp });
      toast.success(whatsapp ? 'Cadastrado e convite enviado!' : 'Cadastrado com sucesso');
      reset();
      onOpenChange(false);
      onCreated();
    } catch {
      toast.error('Erro ao cadastrar');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) reset(); onOpenChange(v); }}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Novo cadastro</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label>Nome *</Label>
            <input
              className="w-full border border-border rounded-lg px-3 py-2 text-sm bg-background focus:outline-none focus:border-primary"
              placeholder="Nome completo"
              value={nome}
              onChange={e => setNome(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && save()}
              autoFocus
            />
          </div>
          <div className="space-y-1.5">
            <Label>WhatsApp</Label>
            <input
              className="w-full border border-border rounded-lg px-3 py-2 text-sm bg-background focus:outline-none focus:border-primary"
              placeholder="5511999999999"
              value={whatsapp}
              onChange={e => setWhatsapp(e.target.value)}
            />
            <p className="text-xs text-muted-foreground">
              Se informado, o link do diagnóstico é enviado automaticamente via WhatsApp.
            </p>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
          <Button onClick={save} disabled={saving || !nome.trim()}>
            {saving ? 'Enviando...' : whatsapp ? 'Cadastrar e enviar link' : 'Cadastrar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── KanbanCard ───────────────────────────────────────────────────────────────

function KanbanCard({ d, onFaseChange, isDragging }) {
  const col = getColuna(d.fase);
  const [sendingWpp, setSendingWpp] = useState(false);

  const reenviarLink = async (e) => {
    e.stopPropagation();
    setSendingWpp(true);
    try {
      await diagnosticoApi.reenviarWpp(d.id);
      toast.success('Link reenviado!');
      onFaseChange();
    } catch {
      toast.error('Erro ao reenviar');
    } finally {
      setSendingWpp(false);
    }
  };

  const avancar = async (e) => {
    e.stopPropagation();
    const idx = COLUNAS.findIndex(c => c.fase === d.fase);
    if (idx < COLUNAS.length - 1) {
      try {
        await diagnosticoApi.updateFase(d.id, COLUNAS[idx + 1].fase);
        onFaseChange();
      } catch {
        toast.error('Erro ao mover');
      }
    }
  };

  const idxAtual = COLUNAS.findIndex(c => c.fase === d.fase);
  const podeAvancar = idxAtual < COLUNAS.length - 1;

  return (
    <div
      draggable
      onDragStart={e => {
        e.dataTransfer.setData('text/plain', String(d.id));
        e.dataTransfer.effectAllowed = 'move';
      }}
      className={cn(
        'bg-card rounded-xl border border-border p-3 cursor-grab active:cursor-grabbing',
        'shadow-sm hover:shadow-md transition-all select-none group',
        isDragging && 'opacity-40 scale-95'
      )}
    >
      {/* Header do card */}
      <div className="flex items-start gap-2 mb-2">
        <div className={cn('w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-bold shrink-0', col.color)}>
          {initials(d.nome)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-semibold text-sm text-foreground leading-tight truncate">{d.nome}</div>
          {d.whatsapp && (
            <div className="text-xs text-muted-foreground truncate">{d.whatsapp}</div>
          )}
        </div>
      </div>

      {/* Meta */}
      <div className="flex items-center gap-1.5 mb-2.5">
        {/* Bloco dots (só para quem começou a preencher) */}
        {d.ultimoBloco > 0 && (
          <div className="flex gap-0.5">
            {[0,1,2,3,4].map(i => (
              <div key={i} className={cn('w-1.5 h-1.5 rounded-full', i < d.ultimoBloco ? 'bg-primary' : 'bg-border')} />
            ))}
          </div>
        )}
        <span className="text-xs text-muted-foreground ml-auto">{fmtDateShort(d.createdAt)}</span>
      </div>

      {/* Ações (visíveis no hover) */}
      <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
        {/* Reenviar link — só para convite_enviado e novo (com whatsapp) */}
        {(d.fase === 'novo' || d.fase === 'convite_enviado') && d.whatsapp && (
          <button
            onClick={reenviarLink}
            disabled={sendingWpp}
            className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-700 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded-md"
          >
            <MessageCircle className="h-3 w-3" />
            {sendingWpp ? '...' : 'Link'}
          </button>
        )}

        {/* Avançar fase */}
        {podeAvancar && (
          <button
            onClick={avancar}
            className="flex items-center gap-0.5 text-xs text-muted-foreground hover:text-foreground bg-muted px-2 py-1 rounded-md ml-auto"
          >
            <ChevronRight className="h-3 w-3" />
            {COLUNAS[idxAtual + 1]?.label}
          </button>
        )}
      </div>
    </div>
  );
}

// ─── KanbanColuna ─────────────────────────────────────────────────────────────

function KanbanColuna({ coluna, cards, onFaseChange, draggingId, onDragOver, onDrop }) {
  const isOver = useRef(false);
  const [over, setOver] = useState(false);

  return (
    <div className="flex flex-col shrink-0 w-56">
      {/* Header */}
      <div className={cn('rounded-xl px-3 py-2 mb-2 flex items-center gap-2', coluna.light, `border ${coluna.border}`)}>
        <div className={cn('w-2.5 h-2.5 rounded-full shrink-0', coluna.color)} />
        <span className="text-xs font-bold text-foreground flex-1 leading-tight">{coluna.label}</span>
        <span className="text-xs font-bold text-muted-foreground bg-background/60 px-1.5 py-0.5 rounded-full">
          {cards.length}
        </span>
      </div>

      {/* Drop zone */}
      <div
        onDragOver={e => { e.preventDefault(); setOver(true); onDragOver(e); }}
        onDragLeave={() => setOver(false)}
        onDrop={e => { setOver(false); onDrop(e, coluna.fase); }}
        className={cn(
          'flex flex-col gap-2 min-h-24 rounded-xl p-1.5 transition-colors',
          over ? 'bg-primary/5 ring-2 ring-primary/30 ring-dashed' : 'bg-transparent'
        )}
      >
        {cards.map(d => (
          <KanbanCard
            key={d.id}
            d={d}
            onFaseChange={onFaseChange}
            isDragging={draggingId === d.id}
          />
        ))}

        {cards.length === 0 && !over && (
          <div className="text-xs text-muted-foreground/40 text-center py-4 italic">
            Vazio
          </div>
        )}
      </div>
    </div>
  );
}

// ─── KanbanPage ───────────────────────────────────────────────────────────────

export default function KanbanPage() {
  const [diagnosticos, setDiagnosticos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [novoOpen, setNovoOpen] = useState(false);
  const [draggingId, setDraggingId] = useState(null);

  const fetchAll = async () => {
    setLoading(true);
    try {
      const res = await diagnosticoApi.getAll();
      setDiagnosticos(res.data);
    } catch {
      toast.error('Erro ao carregar');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchAll(); }, []);

  const cardsByFase = (fase) =>
    diagnosticos.filter(d => (d.fase ?? 'novo') === fase);

  const handleDrop = async (e, targetFase) => {
    e.preventDefault();
    const id = parseInt(e.dataTransfer.getData('text/plain'));
    if (!id || isNaN(id)) return;

    const card = diagnosticos.find(d => d.id === id);
    if (!card || card.fase === targetFase) return;

    // Optimistic update
    setDiagnosticos(prev => prev.map(d => d.id === id ? { ...d, fase: targetFase } : d));

    try {
      await diagnosticoApi.updateFase(id, targetFase);
    } catch {
      toast.error('Erro ao mover card');
      fetchAll(); // reverte
    }
    setDraggingId(null);
  };

  const total = diagnosticos.length;

  return (
    <div className="flex flex-col h-full">
      {/* Cabeçalho */}
      <div className="flex items-center justify-between mb-6 shrink-0">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Kanban</h1>
          <p className="text-muted-foreground text-sm mt-1">
            {total} pessoa{total !== 1 ? 's' : ''} no fluxo
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={fetchAll}>
            <RefreshCw className="h-4 w-4 mr-2" /> Atualizar
          </Button>
          <Button size="sm" onClick={() => setNovoOpen(true)}>
            <Plus className="h-4 w-4 mr-2" /> Novo cadastro
          </Button>
        </div>
      </div>

      {/* Board */}
      {loading ? (
        <div className="text-sm text-muted-foreground">Carregando...</div>
      ) : (
        <div
          className="flex gap-3 overflow-x-auto pb-4 flex-1"
          onDragStart={e => {
            const id = parseInt(e.dataTransfer.getData('text/plain'));
            if (!isNaN(id)) setDraggingId(id);
          }}
          onDragEnd={() => setDraggingId(null)}
        >
          {COLUNAS.map(col => (
            <KanbanColuna
              key={col.fase}
              coluna={col}
              cards={cardsByFase(col.fase)}
              onFaseChange={fetchAll}
              draggingId={draggingId}
              onDragOver={e => e.preventDefault()}
              onDrop={handleDrop}
            />
          ))}
        </div>
      )}

      <NovoConvidadoDialog
        open={novoOpen}
        onOpenChange={setNovoOpen}
        onCreated={fetchAll}
      />
    </div>
  );
}
