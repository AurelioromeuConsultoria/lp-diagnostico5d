const express = require('express');
const { DatabaseSync } = require('node:sqlite');
const path = require('path');

const app = express();
const db = new DatabaseSync(path.join(__dirname, 'diagnostico.db'));

// Create table with all columns
db.exec(`
  CREATE TABLE IF NOT EXISTS submissions (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    nome         TEXT NOT NULL,
    whatsapp     TEXT,
    status       TEXT DEFAULT 'parcial',
    ultimo_bloco INTEGER DEFAULT 0,
    created_at   TEXT DEFAULT (datetime('now','localtime')),
    updated_at   TEXT DEFAULT (datetime('now','localtime')),
    q1  TEXT, q2  TEXT, q3  TEXT, q4  TEXT, q5  TEXT,
    q6  TEXT, q7  TEXT, q8  TEXT, q9  TEXT, q10 TEXT,
    q11 TEXT, q12 TEXT, q13 TEXT, q14 TEXT, q15 TEXT,
    q16 TEXT, q17 TEXT, q18 TEXT, q19 TEXT, q20 TEXT,
    q21 TEXT, q22 TEXT, q23 TEXT, q24 TEXT, q25 TEXT,
    b6_id_status TEXT, b6_id_quebra TEXT,
    b6_gi_status TEXT, b6_gi_quebra TEXT,
    b6_pr_status TEXT, b6_pr_quebra TEXT,
    b6_fa_status TEXT, b6_fa_quebra TEXT,
    b6_po_status TEXT, b6_po_quebra TEXT,
    b6_gargalo TEXT, b6_erro_invisivel TEXT, b6_proximo_movimento TEXT
  )
`);

// Migrate existing DB (adds missing columns safely)
[
  "ALTER TABLE submissions ADD COLUMN status TEXT DEFAULT 'parcial'",
  "ALTER TABLE submissions ADD COLUMN ultimo_bloco INTEGER DEFAULT 0",
  "ALTER TABLE submissions ADD COLUMN updated_at TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_id_status TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_id_quebra TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_gi_status TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_gi_quebra TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_pr_status TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_pr_quebra TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_fa_status TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_fa_quebra TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_po_status TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_po_quebra TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_gargalo TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_erro_invisivel TEXT",
  "ALTER TABLE submissions ADD COLUMN b6_proximo_movimento TEXT",
].forEach(sql => { try { db.exec(sql); } catch (_) {} });

app.use(express.json({ limit: '2mb' }));
app.use(express.static(path.join(__dirname)));

function qValues(qs) {
  return Array.from({ length: 25 }, (_, i) => qs['q' + (i + 1)] || '');
}

// POST — create new submission (partial or complete)
app.post('/api/diagnostico', (req, res) => {
  const { nome, whatsapp, status = 'parcial', ultimo_bloco = 0, ...qs } = req.body;
  if (!nome || !nome.trim()) return res.status(400).json({ error: 'Nome é obrigatório.' });

  const result = db.prepare(`
    INSERT INTO submissions
      (nome, whatsapp, status, ultimo_bloco,
       q1,q2,q3,q4,q5, q6,q7,q8,q9,q10,
       q11,q12,q13,q14,q15, q16,q17,q18,q19,q20, q21,q22,q23,q24,q25)
    VALUES (?,?,?,?, ?,?,?,?,?, ?,?,?,?,?, ?,?,?,?,?, ?,?,?,?,?, ?,?,?,?,?)
  `).run(nome.trim(), (whatsapp || '').trim(), status, ultimo_bloco, ...qValues(qs));

  res.json({ success: true, id: result.lastInsertRowid });
});

// PUT — update existing submission (partial or complete)
app.put('/api/diagnostico/:id', (req, res) => {
  const id = parseInt(req.params.id);
  const { nome, whatsapp, status = 'parcial', ultimo_bloco = 0, ...qs } = req.body;

  const result = db.prepare(`
    UPDATE submissions SET
      nome=?, whatsapp=?, status=?, ultimo_bloco=?,
      updated_at=datetime('now','localtime'),
      q1=?,q2=?,q3=?,q4=?,q5=?, q6=?,q7=?,q8=?,q9=?,q10=?,
      q11=?,q12=?,q13=?,q14=?,q15=?, q16=?,q17=?,q18=?,q19=?,q20=?,
      q21=?,q22=?,q23=?,q24=?,q25=?
    WHERE id=?
  `).run((nome || '').trim(), (whatsapp || '').trim(), status, ultimo_bloco, ...qValues(qs), id);

  if (result.changes === 0) return res.status(404).json({ error: 'Não encontrado.' });
  res.json({ success: true });
});

// PUT — save Bloco 6 mentor assessment
app.put('/api/diagnostico/:id/bloco6', (req, res) => {
  const id = parseInt(req.params.id);
  const {
    b6_id_status, b6_id_quebra,
    b6_gi_status, b6_gi_quebra,
    b6_pr_status, b6_pr_quebra,
    b6_fa_status, b6_fa_quebra,
    b6_po_status, b6_po_quebra,
    b6_gargalo, b6_erro_invisivel, b6_proximo_movimento,
  } = req.body;

  const result = db.prepare(`
    UPDATE submissions SET
      updated_at=datetime('now','localtime'),
      b6_id_status=?, b6_id_quebra=?,
      b6_gi_status=?, b6_gi_quebra=?,
      b6_pr_status=?, b6_pr_quebra=?,
      b6_fa_status=?, b6_fa_quebra=?,
      b6_po_status=?, b6_po_quebra=?,
      b6_gargalo=?, b6_erro_invisivel=?, b6_proximo_movimento=?
    WHERE id=?
  `).run(
    b6_id_status||'', b6_id_quebra||'',
    b6_gi_status||'', b6_gi_quebra||'',
    b6_pr_status||'', b6_pr_quebra||'',
    b6_fa_status||'', b6_fa_quebra||'',
    b6_po_status||'', b6_po_quebra||'',
    b6_gargalo||'', b6_erro_invisivel||'', b6_proximo_movimento||'',
    id
  );

  if (result.changes === 0) return res.status(404).json({ error: 'Não encontrado.' });
  res.json({ success: true });
});

// GET by WhatsApp — lookup for session restore across devices
app.get('/api/diagnostico/lookup', (req, res) => {
  const raw = (req.query.whatsapp || '').replace(/\D/g, '');
  if (!raw) return res.status(400).json({ error: 'WhatsApp obrigatório.' });

  // Match storing digits only OR formatted — compare digits
  const row = db.prepare(`
    SELECT * FROM submissions
    WHERE replace(replace(replace(replace(replace(whatsapp,' ',''),'-',''),'(',''),')',''),'+','') = ?
      AND status = 'parcial'
    ORDER BY updated_at DESC, created_at DESC
    LIMIT 1
  `).get(raw);

  if (!row) return res.json({ found: false });
  res.json({ found: true, record: row });
});

// GET all submissions
app.get('/api/diagnostico', (req, res) => {
  const rows = db.prepare('SELECT * FROM submissions ORDER BY created_at DESC').all();
  res.json(rows);
});

// GET single submission
app.get('/api/diagnostico/:id', (req, res) => {
  const row = db.prepare('SELECT * FROM submissions WHERE id=?').get(parseInt(req.params.id));
  if (!row) return res.status(404).json({ error: 'Não encontrado.' });
  res.json(row);
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Servidor rodando em http://localhost:${PORT}`));
