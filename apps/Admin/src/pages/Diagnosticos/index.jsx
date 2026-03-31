import { useState, useEffect, useMemo } from 'react';
import { diagnosticoApi } from '@/api/diagnostico';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { toast } from 'sonner';
import {
  RefreshCw, ChevronDown, MoreVertical, Pencil, Trash2,
  MessageCircle, CheckCircle2, Circle, Download, Search, ArrowUpDown,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// ─── Dados estáticos ─────────────────────────────────────────────────────────

const QUESTIONS = [
  '',
  'Quando você pensa em dinheiro, qual é o primeiro sentimento que vem?',
  'Você se vê como provedora ou como alguém que está tentando sobreviver?',
  'Você trabalha para pagar contas ou trabalha com propósito definido?',
  'Existe algo que você acredita que "pessoas como você" não conseguem ter?',
  'Quando alguém próximo prospera, qual é sua reação interna?',
  'Sua rotina diária é construída por você ou ditada pelas urgências?',
  'Você tem um tempo reservado para silêncio e direção — sem celular, sem tarefas?',
  'Você termina o que começa? Dê exemplos reais.',
  'Há decisões que você sabe que precisa tomar e está adiando? Quais?',
  'Você descansa com intencionalidade ou só para quando está esgotada?',
  'Você está se preparando ou esperando a oportunidade aparecer?',
  'Qual foi o último aprendizado que mudou sua forma de agir — não só de pensar?',
  'Você aceita correção com facilidade ou sente resistência?',
  'Existe algo na sua vida que está errado e você ainda não corrigiu?',
  'Você está na posição certa agora, ou está forçando uma porta que não é a sua?',
  'Você tem clareza do que foi chamada a fazer — ou ainda está testando possibilidades?',
  'Sua fé produz ação concreta ou produz espera passiva?',
  'Quem está na sua aliança mais próxima? Essas pessoas avançam ou estacionam?',
  'Você tem alguém que fala verdade para você — mesmo quando dói?',
  'Existe algo que Deus já falou pra você que você ainda não obedeceu?',
  'Você tem controle real do que entra e do que sai — ou só uma ideia aproximada?',
  'Você planta com constância ou só quando está motivada?',
  'Existe algo que consome seu tempo, dinheiro ou energia sem retorno real?',
  'Você já perdeu uma oportunidade por falta de preparo ou por medo?',
  'O que você faria diferente se soubesse que ia funcionar?',
];

const BLOCOS = [
  { label: 'Bloco 1 — Identidade e Posição',  qs: [1,2,3,4,5]   },
  { label: 'Bloco 2 — Governo Interior',       qs: [6,7,8,9,10]  },
  { label: 'Bloco 3 — Preparação e Processo',  qs: [11,12,13,14,15] },
  { label: 'Bloco 4 — Fé, Ação e Aliança',     qs: [16,17,18,19,20] },
  { label: 'Bloco 5 — Prosperidade e Leis',    qs: [21,22,23,24,25] },
];

const AREAS_B6 = [
  { key: 'b6Identidade',   label: 'Identidade' },
  { key: 'b6Governo',      label: 'Governo Interior' },
  { key: 'b6Preparacao',   label: 'Preparação' },
  { key: 'b6FeAcao',       label: 'Fé e Ação' },
  { key: 'b6Prosperidade', label: 'Prosperidade' },
];

// ─── Helpers ─────────────────────────────────────────────────────────────────

function initials(nome) {
  return (nome || '?').split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
}

function fmtDate(dt) {
  if (!dt) return '—';
  return new Date(dt).toLocaleDateString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function fmtDateShort(dt) {
  if (!dt) return '—';
  return new Date(dt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function isToday(dt) {
  if (!dt) return false;
  const d = new Date(dt);
  const today = new Date();
  return d.getDate() === today.getDate() && d.getMonth() === today.getMonth() && d.getFullYear() === today.getFullYear();
}

function exportarCSV(dados) {
  const headers = ['ID','Nome','WhatsApp','Status','Bloco','Cadastro','Conclusão','WA Enviado','Revisado'];
  const rows = dados.map(d => [
    d.id,
    d.nome,
    d.whatsapp || '',
    d.status === 'completo' ? 'Completo' : 'Em andamento',
    d.ultimoBloco,
    fmtDateShort(d.createdAt),
    fmtDateShort(d.concluidoEm),
    d.whatsappEnviado ? 'Sim' : 'Não',
    d.mentorRevisado ? 'Sim' : 'Não',
  ]);
  const csv = [headers, ...rows].map(r => r.map(v => `"${String(v).replace(/"/g,'""')}"`).join(',')).join('\n');
  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `diagnosticos-${new Date().toISOString().slice(0,10)}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}

// ─── StatCard ────────────────────────────────────────────────────────────────

function StatCard({ num, label, sub }) {
  return (
    <div className="bg-card rounded-2xl p-5 border border-border shadow-sm">
      <div className="text-3xl font-bold text-primary leading-none mb-1">{num}</div>
      <div className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{label}</div>
      {sub && <div className="text-xs text-muted-foreground mt-0.5">{sub}</div>}
    </div>
  );
}

// ─── EditDialog ───────────────────────────────────────────────────────────────

function EditDialog({ open, onOpenChange, d, onSaved }) {
  const [nome, setNome] = useState(d?.nome || '');
  const [whatsapp, setWhatsapp] = useState(d?.whatsapp || '');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) { setNome(d?.nome || ''); setWhatsapp(d?.whatsapp || ''); }
  }, [open, d]);

  const save = async () => {
    if (!nome.trim()) return;
    setSaving(true);
    try {
      await diagnosticoApi.editarCadastro(d.id, { nome, whatsapp });
      toast.success('Cadastro atualizado');
      onSaved();
      onOpenChange(false);
    } catch {
      toast.error('Erro ao salvar');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>Editar cadastro</DialogTitle></DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label>Nome</Label>
            <input
              className="w-full border border-border rounded-lg px-3 py-2 text-sm bg-background focus:outline-none focus:border-primary"
              value={nome}
              onChange={e => setNome(e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label>WhatsApp</Label>
            <input
              className="w-full border border-border rounded-lg px-3 py-2 text-sm bg-background focus:outline-none focus:border-primary"
              value={whatsapp}
              onChange={e => setWhatsapp(e.target.value)}
              placeholder="Ex: 11999999999"
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
          <Button onClick={save} disabled={saving || !nome.trim()}>
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── DeleteDialog ─────────────────────────────────────────────────────────────

function DeleteDialog({ open, onOpenChange, d, onDeleted }) {
  const [loading, setLoading] = useState(false);

  const confirm = async () => {
    setLoading(true);
    try {
      await diagnosticoApi.delete(d.id);
      toast.success('Diagnóstico excluído');
      onDeleted();
      onOpenChange(false);
    } catch {
      toast.error('Erro ao excluir');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>Excluir diagnóstico</DialogTitle></DialogHeader>
        <p className="text-sm text-muted-foreground">
          Tem certeza que deseja excluir o diagnóstico de <strong>{d?.nome}</strong>? Esta ação não pode ser desfeita.
        </p>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
          <Button variant="destructive" onClick={confirm} disabled={loading}>
            {loading ? 'Excluindo...' : 'Excluir'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── SubmissionCard ───────────────────────────────────────────────────────────

function SubmissionCard({ d, onRefresh }) {
  const [open, setOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [sendingWpp, setSendingWpp] = useState(false);
  const [togglingRevisado, setTogglingRevisado] = useState(false);

  // Bloco 6
  const [b6, setB6] = useState(() => ({
    b6IdentidadeStatus:   d.b6IdentidadeStatus   || '',
    b6IdentidadeQuebra:   d.b6IdentidadeQuebra   || '',
    b6GovernoStatus:      d.b6GovernoStatus      || '',
    b6GovernoQuebra:      d.b6GovernoQuebra      || '',
    b6PreparacaoStatus:   d.b6PreparacaoStatus   || '',
    b6PreparacaoQuebra:   d.b6PreparacaoQuebra   || '',
    b6FeAcaoStatus:       d.b6FeAcaoStatus       || '',
    b6FeAcaoQuebra:       d.b6FeAcaoQuebra       || '',
    b6ProsperidadeStatus: d.b6ProsperidadeStatus || '',
    b6ProsperidadeQuebra: d.b6ProsperidadeQuebra || '',
    b6Gargalo:            d.b6Gargalo            || '',
    b6ErroInvisivel:      d.b6ErroInvisivel      || '',
    b6ProximoMovimento:   d.b6ProximoMovimento   || '',
  }));
  const [savingB6, setSavingB6] = useState(false);
  const [savedB6, setSavedB6] = useState(false);

  // Mentor
  const [observacao, setObservacao] = useState(d.mentorObservacao || '');
  const [savingObs, setSavingObs] = useState(false);

  const setB6Field = (field, value) => setB6(prev => ({ ...prev, [field]: value }));

  const saveBloco6 = async () => {
    setSavingB6(true);
    try {
      await diagnosticoApi.updateBloco6(d.id, b6);
      setSavedB6(true);
      setTimeout(() => setSavedB6(false), 3000);
      onRefresh();
    } catch {
      toast.error('Erro ao salvar diagnóstico');
    } finally {
      setSavingB6(false);
    }
  };

  const saveObservacao = async () => {
    setSavingObs(true);
    try {
      await diagnosticoApi.updateMentor(d.id, { revisado: d.mentorRevisado, observacao });
      toast.success('Observação salva');
      onRefresh();
    } catch {
      toast.error('Erro ao salvar observação');
    } finally {
      setSavingObs(false);
    }
  };

  const toggleRevisado = async (e) => {
    e.stopPropagation();
    setTogglingRevisado(true);
    try {
      await diagnosticoApi.updateMentor(d.id, { revisado: !d.mentorRevisado, observacao: d.mentorObservacao || '' });
      onRefresh();
    } catch {
      toast.error('Erro ao atualizar');
    } finally {
      setTogglingRevisado(false);
    }
  };

  const reenviarWpp = async () => {
    setSendingWpp(true);
    try {
      await diagnosticoApi.reenviarWpp(d.id);
      toast.success('Mensagem enviada!');
      onRefresh();
    } catch {
      toast.error('Erro ao enviar mensagem');
    } finally {
      setSendingWpp(false);
    }
  };

  const statusEmoji = { ok: '✅', atencao: '⚠️', critico: '❌' };

  return (
    <>
      <div className={cn(
        'bg-card rounded-2xl border shadow-sm overflow-hidden transition-all',
        d.mentorRevisado ? 'border-l-4 border-l-green-500 border-border' : 'border-border',
        open && 'shadow-md'
      )}>
        {/* Header */}
        <div className="flex items-center gap-3 px-5 py-4">
          {/* Avatar — clicável para expandir */}
          <div
            className="w-10 h-10 rounded-full bg-primary/10 text-primary flex items-center justify-center font-bold text-sm shrink-0 cursor-pointer"
            onClick={() => setOpen(v => !v)}
          >
            {initials(d.nome)}
          </div>

          {/* Info — clicável para expandir */}
          <div className="flex-1 min-w-0 cursor-pointer" onClick={() => setOpen(v => !v)}>
            <div className="font-semibold text-foreground text-sm leading-tight">{d.nome}</div>
            <div className="text-xs text-muted-foreground mt-0.5 truncate">
              {d.whatsapp || 'Sem WhatsApp'} · {fmtDateShort(d.createdAt)}
            </div>
          </div>

          {/* Right controls */}
          <div className="flex items-center gap-2 shrink-0">
            {/* WA enviado */}
            {d.whatsappEnviado && (
              <span title={`Enviado em ${fmtDate(d.whatsappEnviadoEm)}`}
                className="text-xs font-semibold bg-green-100 text-green-700 dark:bg-green-950/50 dark:text-green-400 px-2 py-0.5 rounded-full">
                WA ✓
              </span>
            )}

            {/* Bloco dots */}
            <div className="hidden sm:flex gap-1">
              {[0,1,2,3,4].map(i => (
                <div key={i} className={cn('w-2 h-2 rounded-full', i < (d.ultimoBloco || 0) ? 'bg-primary' : 'bg-border')} />
              ))}
            </div>

            {/* Status */}
            <Badge variant={d.status === 'completo' ? 'default' : 'secondary'} className="text-xs hidden sm:inline-flex">
              {d.status === 'completo' ? 'Completo' : 'Em andamento'}
            </Badge>

            {/* Revisado toggle */}
            <button
              onClick={toggleRevisado}
              disabled={togglingRevisado}
              title={d.mentorRevisado ? 'Marcar como não revisado' : 'Marcar como revisado'}
              className="p-1 rounded-md transition-colors hover:bg-muted"
            >
              {d.mentorRevisado
                ? <CheckCircle2 className="h-5 w-5 text-green-500" />
                : <Circle className="h-5 w-5 text-muted-foreground/40" />
              }
            </button>

            {/* Menu */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button className="p-1 rounded-md hover:bg-muted transition-colors">
                  <MoreVertical className="h-4 w-4 text-muted-foreground" />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => setEditOpen(true)}>
                  <Pencil className="h-4 w-4 mr-2" /> Editar cadastro
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={reenviarWpp}
                  disabled={sendingWpp || !d.whatsapp}
                >
                  <MessageCircle className="h-4 w-4 mr-2" />
                  {sendingWpp ? 'Enviando...' : 'Enviar WhatsApp'}
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  className="text-destructive focus:text-destructive"
                  onClick={() => setDeleteOpen(true)}
                >
                  <Trash2 className="h-4 w-4 mr-2" /> Excluir
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Chevron */}
            <button onClick={() => setOpen(v => !v)} className="p-1 rounded-md hover:bg-muted transition-colors">
              <ChevronDown className={cn('h-4 w-4 text-muted-foreground transition-transform duration-300', open && 'rotate-180')} />
            </button>
          </div>
        </div>

        {/* Expanded body */}
        {open && (
          <div className="border-t border-border px-5 pt-5 pb-7 space-y-7">

            {/* Meta detalhes */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
              {[
                { label: 'Status', value: d.status === 'completo' ? 'Completo' : 'Em andamento' },
                { label: 'Bloco alcançado', value: `${d.ultimoBloco}/5` },
                { label: 'Cadastro', value: fmtDate(d.createdAt) },
                { label: 'Conclusão', value: fmtDate(d.concluidoEm) },
              ].map(({ label, value }) => (
                <div key={label} className="bg-muted/40 rounded-xl p-3">
                  <div className="text-muted-foreground mb-0.5 font-medium uppercase tracking-wide" style={{fontSize:'0.65rem'}}>{label}</div>
                  <div className="font-semibold text-foreground">{value}</div>
                </div>
              ))}
            </div>

            {/* Q&A por bloco */}
            {BLOCOS.map(bloco => (
              <div key={bloco.label}>
                <div className="text-xs font-bold uppercase tracking-widest text-primary mb-3 pb-2 border-b border-primary/15">
                  {bloco.label}
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  {bloco.qs.map(n => {
                    const ans = (d[`q${n}`] || '').trim();
                    return (
                      <div key={n} className="bg-muted/40 rounded-xl p-4">
                        <div className="text-xs font-medium text-muted-foreground mb-1.5">{n}. {QUESTIONS[n]}</div>
                        <div className={cn('text-sm leading-relaxed', !ans && 'italic text-muted-foreground')}>
                          {ans || 'Não respondida'}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}

            {/* Observação interna do mentor */}
            <div className="bg-muted/30 rounded-2xl p-5 border border-border">
              <div className="font-semibold text-foreground mb-1">Observação interna</div>
              <div className="text-xs text-muted-foreground mb-3">Nota privada — não visível para o aluno.</div>
              <textarea
                className="w-full bg-background border border-border rounded-lg px-3 py-2.5 text-sm resize-vertical min-h-[72px] focus:outline-none focus:border-primary transition-colors"
                rows={3}
                placeholder="Anotações pessoais sobre este aluno..."
                value={observacao}
                onChange={e => setObservacao(e.target.value)}
              />
              <div className="flex justify-end mt-2">
                <Button size="sm" variant="outline" onClick={saveObservacao} disabled={savingObs}>
                  {savingObs ? 'Salvando...' : 'Salvar observação'}
                </Button>
              </div>
            </div>

            {/* Bloco 6 */}
            <div className="bg-amber-50 dark:bg-amber-950/20 rounded-2xl p-6 border border-amber-200/50 dark:border-amber-800/30">
              <div className="font-bold text-lg text-foreground mb-1">Bloco 6 — Diagnóstico Final</div>
              <div className="text-sm text-muted-foreground mb-5">Mapeie as quebras identificadas por área.</div>

              <div className="overflow-x-auto mb-5">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border/60">
                      <th className="text-left text-xs font-bold uppercase tracking-widest text-muted-foreground pb-2 pr-4 w-36">Área</th>
                      <th className="text-left text-xs font-bold uppercase tracking-widest text-muted-foreground pb-2 pr-4 w-28">Status</th>
                      <th className="text-left text-xs font-bold uppercase tracking-widest text-muted-foreground pb-2">Quebra Identificada</th>
                    </tr>
                  </thead>
                  <tbody>
                    {AREAS_B6.map(area => (
                      <tr key={area.key} className="border-b border-border/30 last:border-0">
                        <td className="py-3 pr-4 font-semibold text-foreground whitespace-nowrap align-top pt-4">{area.label}</td>
                        <td className="py-3 pr-4 align-top pt-4">
                          <div className="flex gap-1.5">
                            {Object.entries(statusEmoji).map(([val, emoji]) => (
                              <label key={val} className={cn(
                                'w-9 h-9 rounded-lg border-2 flex items-center justify-center cursor-pointer text-base transition-all',
                                b6[`${area.key}Status`] === val
                                  ? val === 'ok' ? 'border-green-600 bg-green-50 dark:bg-green-950/40'
                                  : val === 'atencao' ? 'border-yellow-600 bg-yellow-50 dark:bg-yellow-950/40'
                                  : 'border-red-600 bg-red-50 dark:bg-red-950/40'
                                  : 'border-border bg-background hover:border-muted-foreground'
                              )}>
                                <input
                                  type="radio"
                                  className="sr-only"
                                  name={`${d.id}_${area.key}`}
                                  value={val}
                                  checked={b6[`${area.key}Status`] === val}
                                  onChange={() => setB6Field(`${area.key}Status`, val)}
                                />
                                {emoji}
                              </label>
                            ))}
                          </div>
                        </td>
                        <td className="py-3 align-top pt-4">
                          <textarea
                            className="w-full bg-background border border-border rounded-lg px-3 py-2 text-sm resize-none min-h-[56px] focus:outline-none focus:border-primary transition-colors"
                            rows={2}
                            placeholder="Quebra identificada..."
                            value={b6[`${area.key}Quebra`]}
                            onChange={e => setB6Field(`${area.key}Quebra`, e.target.value)}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {[
                { field: 'b6Gargalo',          label: 'Gargalo principal',    placeholder: 'Área com mais quebras encadeadas...' },
                { field: 'b6ErroInvisivel',    label: 'Erro invisível',       placeholder: 'O que ela não vê mas que está travando tudo...' },
                { field: 'b6ProximoMovimento', label: 'Próximo movimento',    placeholder: 'Uma ação concreta para os próximos 7 dias...' },
              ].map(({ field, label, placeholder }) => (
                <div key={field} className="mb-4 last:mb-0">
                  <label className="block text-xs font-bold uppercase tracking-widest text-muted-foreground mb-1.5">{label}</label>
                  <textarea
                    className="w-full bg-background border border-border rounded-lg px-3 py-2.5 text-sm resize-vertical min-h-[72px] focus:outline-none focus:border-primary transition-colors"
                    rows={3}
                    placeholder={placeholder}
                    value={b6[field]}
                    onChange={e => setB6Field(field, e.target.value)}
                  />
                </div>
              ))}

              <div className="flex items-center justify-end gap-3 mt-2">
                {savedB6 && <span className="text-xs font-semibold text-green-600">✓ Salvo com sucesso</span>}
                <Button onClick={saveBloco6} disabled={savingB6}>
                  {savingB6 ? 'Salvando...' : 'Salvar diagnóstico'}
                </Button>
              </div>
            </div>

          </div>
        )}
      </div>

      <EditDialog open={editOpen} onOpenChange={setEditOpen} d={d} onSaved={onRefresh} />
      <DeleteDialog open={deleteOpen} onOpenChange={setDeleteOpen} d={d} onDeleted={onRefresh} />
    </>
  );
}

// ─── DiagnosticosPage ─────────────────────────────────────────────────────────

export default function DiagnosticosPage() {
  const [diagnosticos, setDiagnosticos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('todos');
  const [busca, setBusca] = useState('');
  const [ordem, setOrdem] = useState('recente');

  const fetchAll = async () => {
    setLoading(true);
    try {
      const res = await diagnosticoApi.getAll();
      setDiagnosticos(res.data);
    } catch {
      toast.error('Erro ao carregar diagnósticos');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchAll(); }, []);

  // Stats
  const completos  = useMemo(() => diagnosticos.filter(d => d.status === 'completo').length, [diagnosticos]);
  const hoje       = useMemo(() => diagnosticos.filter(d => isToday(d.createdAt)).length, [diagnosticos]);
  const revisados  = useMemo(() => diagnosticos.filter(d => d.mentorRevisado).length, [diagnosticos]);
  const taxaConcl  = diagnosticos.length ? Math.round(completos / diagnosticos.length * 100) : 0;

  // Filtro + busca + ordem
  const listagem = useMemo(() => {
    let list = [...diagnosticos];

    if (filter === 'completo')  list = list.filter(d => d.status === 'completo');
    if (filter === 'parcial')   list = list.filter(d => d.status === 'parcial');
    if (filter === 'revisados') list = list.filter(d => d.mentorRevisado);

    if (busca.trim()) {
      const q = busca.toLowerCase();
      list = list.filter(d =>
        d.nome.toLowerCase().includes(q) ||
        (d.whatsapp || '').includes(q)
      );
    }

    if (ordem === 'recente') list.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    if (ordem === 'antigo')  list.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
    if (ordem === 'nome')    list.sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));

    return list;
  }, [diagnosticos, filter, busca, ordem]);

  return (
    <div className="space-y-6">

      {/* Título */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Diagnósticos</h1>
          <p className="text-muted-foreground text-sm mt-1">
            {diagnosticos.length} registrado{diagnosticos.length !== 1 ? 's' : ''} · {hoje} hoje
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => exportarCSV(diagnosticos)} disabled={!diagnosticos.length}>
            <Download className="h-4 w-4 mr-2" /> Exportar CSV
          </Button>
          <Button variant="outline" size="sm" onClick={fetchAll}>
            <RefreshCw className="h-4 w-4 mr-2" /> Atualizar
          </Button>
        </div>
      </div>

      {/* Stats */}
      {!loading && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <StatCard num={diagnosticos.length} label="Total" />
          <StatCard num={completos}  label="Completos" sub={`${taxaConcl}% de conclusão`} />
          <StatCard num={revisados}  label="Revisados" />
          <StatCard num={hoje}       label="Hoje" />
        </div>
      )}

      {/* Busca + ordem + filtros */}
      <div className="flex flex-col sm:flex-row gap-3">
        {/* Busca */}
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            className="w-full pl-9 pr-3 py-2 border border-border rounded-xl text-sm bg-background focus:outline-none focus:border-primary transition-colors"
            placeholder="Buscar por nome ou WhatsApp..."
            value={busca}
            onChange={e => setBusca(e.target.value)}
          />
        </div>

        {/* Ordem */}
        <select
          className="border border-border rounded-xl px-3 py-2 text-sm bg-background focus:outline-none focus:border-primary transition-colors"
          value={ordem}
          onChange={e => setOrdem(e.target.value)}
        >
          <option value="recente">Mais recente</option>
          <option value="antigo">Mais antigo</option>
          <option value="nome">Nome A→Z</option>
        </select>
      </div>

      {/* Filtros */}
      <div className="flex gap-2 flex-wrap">
        {[
          { key: 'todos',     label: 'Todos',        count: diagnosticos.length },
          { key: 'completo',  label: 'Completos',    count: completos },
          { key: 'parcial',   label: 'Em andamento', count: diagnosticos.length - completos },
          { key: 'revisados', label: 'Revisados',    count: revisados },
        ].map(f => (
          <button
            key={f.key}
            onClick={() => setFilter(f.key)}
            className={cn(
              'px-4 py-1.5 rounded-full text-sm font-semibold border-2 transition-all',
              filter === f.key
                ? 'bg-primary border-primary text-primary-foreground'
                : 'bg-transparent border-border text-muted-foreground hover:border-muted-foreground'
            )}
          >
            {f.label} <span className="ml-1 opacity-70">({f.count})</span>
          </button>
        ))}
      </div>

      {/* Lista */}
      {loading ? (
        <div className="space-y-3">
          {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-20 w-full rounded-2xl" />)}
        </div>
      ) : listagem.length === 0 ? (
        <div className="text-center py-16 text-muted-foreground text-sm">
          {busca ? 'Nenhum resultado para a busca.' : 'Nenhum diagnóstico encontrado.'}
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {listagem.map(d => (
            <SubmissionCard key={d.id} d={d} onRefresh={fetchAll} />
          ))}
        </div>
      )}

    </div>
  );
}
