import { useState, useEffect } from 'react';
import { usersApi } from '@/api/users';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { toast } from 'sonner';
import { Plus, Pencil, Trash2, KeyRound, UserCheck, UserX, RefreshCw } from 'lucide-react';

function fmtDate(dt) {
  if (!dt) return '—';
  return new Date(dt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function Field({ label, children }) {
  return (
    <div className="space-y-1.5">
      <Label>{label}</Label>
      {children}
    </div>
  );
}

const INPUT = 'w-full border border-border rounded-lg px-3 py-2 text-sm bg-background focus:outline-none focus:border-primary transition-colors';

// ── CreateDialog ──────────────────────────────────────────────────────────────
function CreateDialog({ open, onOpenChange, onSaved }) {
  const [form, setForm] = useState({ nome: '', email: '', senha: '', confirmar: '' });
  const [saving, setSaving] = useState(false);

  useEffect(() => { if (open) setForm({ nome: '', email: '', senha: '', confirmar: '' }); }, [open]);

  const set = k => e => setForm(f => ({ ...f, [k]: e.target.value }));
  const valid = form.nome && form.email && form.senha && form.senha === form.confirmar;

  const save = async () => {
    if (!valid) return;
    setSaving(true);
    try {
      await usersApi.create({ nome: form.nome, email: form.email, senha: form.senha });
      toast.success('Usuário criado com sucesso');
      onSaved();
      onOpenChange(false);
    } catch (e) {
      toast.error(e?.response?.data?.message || 'Erro ao criar usuário');
    } finally { setSaving(false); }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>Novo usuário</DialogTitle></DialogHeader>
        <div className="space-y-4 py-2">
          <Field label="Nome"><input className={INPUT} value={form.nome} onChange={set('nome')} placeholder="Nome completo" /></Field>
          <Field label="Email"><input className={INPUT} type="email" value={form.email} onChange={set('email')} placeholder="email@exemplo.com" /></Field>
          <Field label="Senha"><input className={INPUT} type="password" value={form.senha} onChange={set('senha')} placeholder="Mínimo 6 caracteres" /></Field>
          <Field label="Confirmar senha">
            <input className={INPUT} type="password" value={form.confirmar} onChange={set('confirmar')} placeholder="Repita a senha" />
            {form.confirmar && form.senha !== form.confirmar && (
              <p className="text-xs text-destructive mt-1">As senhas não coincidem</p>
            )}
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
          <Button onClick={save} disabled={saving || !valid}>{saving ? 'Criando...' : 'Criar usuário'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── EditDialog ────────────────────────────────────────────────────────────────
function EditDialog({ open, onOpenChange, user, onSaved }) {
  const [form, setForm] = useState({ nome: '', email: '', ativo: true });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open && user) setForm({ nome: user.nome, email: user.email, ativo: user.ativo });
  }, [open, user]);

  const set = k => e => setForm(f => ({ ...f, [k]: e.target.value }));

  const save = async () => {
    setSaving(true);
    try {
      await usersApi.update(user.id, form);
      toast.success('Usuário atualizado');
      onSaved();
      onOpenChange(false);
    } catch (e) {
      toast.error(e?.response?.data?.message || 'Erro ao atualizar');
    } finally { setSaving(false); }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>Editar usuário</DialogTitle></DialogHeader>
        <div className="space-y-4 py-2">
          <Field label="Nome"><input className={INPUT} value={form.nome} onChange={set('nome')} /></Field>
          <Field label="Email"><input className={INPUT} type="email" value={form.email} onChange={set('email')} /></Field>
          <Field label="Status">
            <div className="flex gap-3">
              {[{ v: true, l: 'Ativo' }, { v: false, l: 'Inativo' }].map(({ v, l }) => (
                <button
                  key={l}
                  type="button"
                  onClick={() => setForm(f => ({ ...f, ativo: v }))}
                  className={`flex-1 py-2 rounded-lg border text-sm font-medium transition-colors ${
                    form.ativo === v
                      ? 'border-primary bg-primary/10 text-primary'
                      : 'border-border text-muted-foreground hover:border-muted-foreground'
                  }`}
                >{l}</button>
              ))}
            </div>
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
          <Button onClick={save} disabled={saving || !form.nome || !form.email}>{saving ? 'Salvando...' : 'Salvar'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── PasswordDialog ────────────────────────────────────────────────────────────
function PasswordDialog({ open, onOpenChange, user, onSaved }) {
  const [form, setForm] = useState({ nova: '', confirmar: '' });
  const [saving, setSaving] = useState(false);

  useEffect(() => { if (open) setForm({ nova: '', confirmar: '' }); }, [open]);

  const valid = form.nova.length >= 6 && form.nova === form.confirmar;

  const save = async () => {
    setSaving(true);
    try {
      await usersApi.changePassword(user.id, { novaSenha: form.nova });
      toast.success('Senha alterada com sucesso');
      onOpenChange(false);
    } catch (e) {
      toast.error(e?.response?.data?.message || 'Erro ao alterar senha');
    } finally { setSaving(false); }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>Alterar senha — {user?.nome}</DialogTitle></DialogHeader>
        <div className="space-y-4 py-2">
          <Field label="Nova senha"><input className={INPUT} type="password" value={form.nova} onChange={e => setForm(f => ({ ...f, nova: e.target.value }))} placeholder="Mínimo 6 caracteres" /></Field>
          <Field label="Confirmar nova senha">
            <input className={INPUT} type="password" value={form.confirmar} onChange={e => setForm(f => ({ ...f, confirmar: e.target.value }))} placeholder="Repita a senha" />
            {form.confirmar && form.nova !== form.confirmar && (
              <p className="text-xs text-destructive mt-1">As senhas não coincidem</p>
            )}
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
          <Button onClick={save} disabled={saving || !valid}>{saving ? 'Salvando...' : 'Alterar senha'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── DeleteDialog ──────────────────────────────────────────────────────────────
function DeleteDialog({ open, onOpenChange, user, onDeleted }) {
  const [loading, setLoading] = useState(false);

  const confirm = async () => {
    setLoading(true);
    try {
      await usersApi.delete(user.id);
      toast.success('Usuário excluído');
      onDeleted();
      onOpenChange(false);
    } catch (e) {
      toast.error(e?.response?.data?.message || 'Erro ao excluir');
    } finally { setLoading(false); }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>Excluir usuário</DialogTitle></DialogHeader>
        <p className="text-sm text-muted-foreground">
          Tem certeza que deseja excluir <strong>{user?.nome}</strong>? Esta ação não pode ser desfeita.
        </p>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
          <Button variant="destructive" onClick={confirm} disabled={loading}>{loading ? 'Excluindo...' : 'Excluir'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── UsuariosPage ──────────────────────────────────────────────────────────────
export default function UsuariosPage() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [createOpen, setCreateOpen] = useState(false);
  const [editTarget, setEditTarget] = useState(null);
  const [pwTarget, setPwTarget] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);

  const fetchAll = async () => {
    setLoading(true);
    try {
      const res = await usersApi.getAll();
      setUsers(res.data);
    } catch {
      toast.error('Erro ao carregar usuários');
    } finally { setLoading(false); }
  };

  useEffect(() => { fetchAll(); }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Usuários</h1>
          <p className="text-muted-foreground text-sm mt-1">{users.length} usuário{users.length !== 1 ? 's' : ''} cadastrado{users.length !== 1 ? 's' : ''}</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={fetchAll} disabled={loading}>
            <RefreshCw className={`h-4 w-4 mr-1.5 ${loading ? 'animate-spin' : ''}`} /> Atualizar
          </Button>
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4 mr-1.5" /> Novo usuário
          </Button>
        </div>
      </div>

      {loading ? (
        <div className="text-sm text-muted-foreground">Carregando...</div>
      ) : (
        <div className="rounded-2xl border border-border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/30">
                <th className="text-left text-xs font-semibold uppercase tracking-widest text-muted-foreground px-5 py-3">Nome</th>
                <th className="text-left text-xs font-semibold uppercase tracking-widest text-muted-foreground px-5 py-3">Email</th>
                <th className="text-left text-xs font-semibold uppercase tracking-widest text-muted-foreground px-5 py-3">Status</th>
                <th className="text-left text-xs font-semibold uppercase tracking-widest text-muted-foreground px-5 py-3">Criado em</th>
                <th className="px-5 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id} className="border-b border-border/50 last:border-0 hover:bg-muted/20 transition-colors">
                  <td className="px-5 py-4 font-medium text-foreground">{u.nome}</td>
                  <td className="px-5 py-4 text-muted-foreground">{u.email}</td>
                  <td className="px-5 py-4">
                    <Badge variant={u.ativo ? 'default' : 'secondary'} className="gap-1.5">
                      {u.ativo ? <UserCheck className="h-3 w-3" /> : <UserX className="h-3 w-3" />}
                      {u.ativo ? 'Ativo' : 'Inativo'}
                    </Badge>
                  </td>
                  <td className="px-5 py-4 text-muted-foreground">{fmtDate(u.criadoEm)}</td>
                  <td className="px-5 py-4">
                    <div className="flex items-center justify-end gap-1.5">
                      <Button size="sm" variant="ghost" onClick={() => setEditTarget(u)} title="Editar">
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => setPwTarget(u)} title="Alterar senha">
                        <KeyRound className="h-3.5 w-3.5" />
                      </Button>
                      <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setDeleteTarget(u)} title="Excluir">
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <CreateDialog  open={createOpen}       onOpenChange={setCreateOpen}       onSaved={fetchAll} />
      <EditDialog    open={!!editTarget}      onOpenChange={v => !v && setEditTarget(null)}   user={editTarget}   onSaved={fetchAll} />
      <PasswordDialog open={!!pwTarget}       onOpenChange={v => !v && setPwTarget(null)}     user={pwTarget}     onSaved={fetchAll} />
      <DeleteDialog  open={!!deleteTarget}    onOpenChange={v => !v && setDeleteTarget(null)} user={deleteTarget} onDeleted={fetchAll} />
    </div>
  );
}
