---
name: Project structure
description: lp-diagnostico5d — landing page + diagnostico form + .NET 10 API + React admin
type: project
---

**Root (static, untouchable):**
- `index.html` — landing page (sales)
- `diagnostico.html` — 5-step form, saves to API, localStorage restore + WhatsApp lookup

**apps/API/Diagnostico5D.API** — .NET 10 Web API with SQLite (EF Core)
- Controllers/DiagnosticoController.cs
- Services/SubmissionService.cs (+ interface)
- Models/Submission.cs
- Data/AppDbContext.cs
- DTOs/SubmissionDto.cs
- Runs on port 5000 (dev), 8080 (Docker)
- Serves static files from wwwroot (index.html, diagnostico.html, /admin)

**apps/Admin** — React 19 + Vite + Tailwind + Radix UI
- src/api/diagnostico.js — axios API calls
- src/lib/apiClient.js — axios instance (VITE_API_URL)
- src/pages/Diagnosticos/index.jsx — main admin page
- src/components/Bloco6Form.jsx — mentor assessment form
- Built to dist/, served by .NET API at /admin path
- Dev proxy: /api → localhost:5000

**Phase 2 planned:** WhatsApp dispatch via Evolution API (same pattern as AppIgreja EvolutionApiService). Config already in appsettings.json.

**Why:** Migrated from Node.js (server.js) to .NET 10 to match user's existing stack.
