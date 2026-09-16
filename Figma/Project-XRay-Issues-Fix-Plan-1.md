# Project X-Ray — Issue Fix Implementation Plan

Verified each reported issue against the actual code first. Findings below,
then a phased plan. Good news: several of these are small, precise bugs, not
deep redesigns — I've flagged which is which so you can prioritize the fast
wins.

## What's actually happening (verified in code)

| # | Reported issue | Root cause |
|---|---|---|
| 1 | Node click shows only file path, nothing about the node or neighbors | `ArchitecturePage.tsx`'s selected-node panel only renders `displayName`/`componentType`/`filePath` — it never reads `graph.edges` to show what connects to the node, and there's no "what's inside this node" content at all. |
| 2 | Analysis impact graph has no edges; clicking nodes does nothing; no recommendations | `AnalysisResultsPage.tsx`'s `buildGraph()` **hardcodes `edges: []`** — it's not a rendering bug, the edges are never built. There's also no `onNodeClick` on this graph, and no recommendation field exists anywhere in the analysis data model. |
| 3 | Security page has no way to run a scan | Not just missing UI — **the backend has no project-level scan endpoint at all**. Security findings only ever get created as a side effect of running a change analysis (`AttachSecurityEvidenceAsync`, scoped to files touched by that one change). There's no "scan this whole repo now" capability anywhere. |
| 4 | Settings page is messy, most items are redirects | Confirmed: of 6 sidebar items, only "General" renders inline. "Projects", "Repositories", "Security", "AI Settings", "Notifications" are all `<Link>` redirects out to entirely separate pages — it's not a Settings page, it's a nav menu pretending to be one. |
| 5 | AI Settings page doesn't let you change anything | Confirmed: `AiConfigurationPage.tsx` is 100% read-only display. "Model Settings" / "Prompt Templates" / "Usage & Rate Limits" cards have no inputs, no `onClick`, nothing. The backend also only has a `GET /ai/configuration` — **there is no update endpoint to build a form against yet.** |
| 6 | No legend/key on graphs | Confirmed — `TYPE_COLORS` and `RISK_COLOR` maps exist and are used to color nodes, but there's no rendered key explaining what each color means anywhere. |
| 7 | No edit/delete on projects | Confirmed both ways: `ProjectsController` has `GET`/`POST` only — no `PUT`/`DELETE`. Full-stack gap, not a UI oversight. |
| 8 | Export buttons don't work | Confirmed: there is exactly **one** export button in the whole app (`ReportDetailPage.tsx`, "Export PDF") and it has no `onClick` handler — it's a dead button. There is zero PDF/Excel generation code anywhere in the backend. |
| 9 | Graph gets hard to explore as it grows | Compounds two things already flagged in the earlier review: the layout is a naive fixed grid (no dagre/elkjs), and there's no "focus on this node's neighborhood" mode — every view shows the entire graph at once. |

---

## Phase 1 — Backend foundations (1–1.5 weeks)

Almost everything below is blocked on backend gaps, so do this first — building
frontend forms against endpoints that don't exist yet just means redoing them.

- [ ] **Expose impact-path edges.** In `AnalysisService`, `bestPathEdges[nodeId]`
      already holds the exact traversed edges for every impacted node — it's
      just never returned. Add an `edges` array to `AnalysisResponse` (or a
      new `GET /analyses/{id}/graph` endpoint) containing `{sourceNodeId,
      targetNodeId, edgeType, confidence}` for every edge across all
      `bestPathEdges` values. This directly unblocks Phase 3.
- [ ] **Add a node-detail endpoint.** `GET /projects/{id}/graph/nodes/{nodeId}`
      returning: the node's own metadata, its direct incoming/outgoing edges
      (with the neighbor's name + edge type), and — if parseable — a short
      list of what the file/class contains (method/member names your parser
      already extracts during ingestion but currently discards after
      building the node). This unblocks Phase 2.
- [ ] **Add a standalone security scan.** New `POST /projects/{id}/security-scans`
      that runs the existing scanning logic (already used inside
      `AttachSecurityEvidenceAsync`) against **every parsed file in the
      current snapshot**, not just files touched by one change. Backing
      table already exists (`SecurityScans`) — this reuses it with a
      project-wide scope instead of a change-wide one. This unblocks Phase 4.
- [ ] **Add AI configuration update.** `PUT /ai/configuration` accepting
      provider, active model, temperature/token limits, and a system prompt
      override — persist to a new `AiConfiguration` table (currently the GET
      endpoint likely returns values straight from `appsettings`, so this
      needs an actual persisted, editable record). This unblocks Phase 5.
- [ ] **Add project edit/delete.** `PUT /projects/{id}` (name, description)
      and `DELETE /projects/{id}` (cascade: snapshots, nodes, edges,
      analyses, security scans — check FK constraints in the DbContext
      before wiring the delete, several tables likely reference
      `ProjectId` transitively). This unblocks Phase 6.
- [ ] **Add export generation.** `GET /reports/{id}/export?format=pdf|xlsx`.
      Cheapest real implementation: build the export from data you already
      return in `ReportDetailResponse`/`AnalysisResponse` — a simple
      HTML→PDF render (e.g. QuestPDF, already in the .NET ecosystem) for PDF,
      and ClosedXML for Excel. This unblocks Phase 6.

**Exit criteria:** every frontend fix in Phases 2–6 has a real endpoint to
call — no phase after this one should require touching a controller for a
brand-new capability, only wiring up what's here.

---

## Phase 2 — Node detail panel (Architecture page) (3–4 days)

- [ ] Replace the flat `{displayName, componentType, filePath}` panel with a
      real detail view: call the new node-detail endpoint on selection.
- [ ] Show **"Contains"**: member/method list if available for the node's
      type (classes/services), or a short content summary for other types.
- [ ] Show **"Connects to"**: grouped by direction — "Depended on by" (incoming
      edges) and "Depends on" (outgoing edges) — each row showing the
      neighbor's name, edge type (`CALLS`/`IMPORTS`/etc.), and confidence.
      Clicking a neighbor row should re-center the graph on it (see Phase 6's
      focus mode — reuse the same "jump to node" logic).
- [ ] For edges connecting to a node already flagged CRITICAL/RISKY in the
      most recent analysis, surface a short "why this matters" line instead
      of just listing the edge type — this is the "why is it connected to
      critical nodes" part of the ask.

**Exit criteria:** clicking any node answers "what is this" and "what is it
connected to, and why does that matter" without leaving the page.

---

## Phase 3 — Fix the impact analysis graph (4–5 days)

- [ ] Rewrite `buildGraph()` in `AnalysisResultsPage.tsx` to consume the new
      `edges` array from Phase 1 instead of hardcoding `[]`. Map
      `sourceNodeId`/`targetNodeId` to React Flow edge objects, color by risk
      state of the connected nodes (reuse `RISK_COLOR`).
- [ ] Add `onNodeClick` to this graph (currently missing entirely). On click,
      open a side panel showing: the node's `ruleCode` (e.g. "R3"), its
      `distance` from the change, `minPathConfidence`, and a plain-language
      sentence built from the rule code (you already have rule descriptions
      in `AnalysisService`'s comments — surface them as user-facing text
      instead of leaving them as code comments).
- [ ] **Recommendations**: add a `recommendation` field to
      `AnalysisNodeResultResponse`, populated per rule code in
      `AnalysisService.Classify` (e.g. R1/CRITICAL-direct-break →
      "Add a contract test before merging"; R9/runtime-resolved →
      "Confidence is low because this path resolves at runtime — verify
      manually"). Static per-rule-code templates are enough for v1; this
      doesn't need AI.

**Exit criteria:** the blast-radius graph shows real connections, clicking a
node explains the "why," and every impacted node carries at least one
actionable recommendation.

---

## Phase 4 — Security Center: make scanning possible (3–4 days)

- [ ] Add a **"Run Security Scan"** button (top-right of `SecurityCenterPage`,
      next to the title) that calls the new `POST /security-scans` endpoint,
      shows a loading state, then refreshes `findings` on completion.
- [ ] Since this can take a while on a large repo, reuse the honest-progress
      pattern from the earlier roadmap's Phase 3 (indeterminate spinner is
      fine for v1; don't build another fake staged progress bar).
- [ ] Add a **scan history** strip (last N scans with timestamp + finding
      count) so results aren't just "whatever the last analysis happened to
      touch" — this makes the page make sense as a standalone destination
      rather than a leftover view of analysis side-effects.
- [ ] Confirm the existing remediation text in `SecurityFindingResponse` is
      populated for scan-triggered findings the same way it is for
      analysis-triggered ones — you already render it correctly when present.

**Exit criteria:** a user can land on Security Center with zero prior
analyses run and still get a real report.

---

## Phase 5 — Rebuild Settings properly (1–1.5 weeks)

- [ ] Turn Settings into an actual tabbed page: keep one route
      (`/settings`), render sub-panels **inline** based on the active tab
      instead of `<Link>`-ing away. Only genuinely separate concerns
      (Projects list, since it's a full CRUD page in its own right) should
      stay a real navigation target — link to it explicitly labeled as such
      rather than disguising it as a settings tab.
- [ ] Fold "Repositories" into "Projects" (they're currently identical
      redirects — there's no reason for both to exist as separate nav items).
- [ ] Move **AI Settings** and **Notifications** to be inline tabs within
      the same Settings shell, not standalone pages — this is what "fix the
      whole page" mostly comes down to: one settings surface, consistent
      layout, no jarring navigation.
- [ ] Make **AI Settings** editable using the Phase 1 endpoint: form fields
      for provider, active model (dropdown from a small known-good list),
      temperature/token limits (bounded numeric inputs), and prompt override
      (textarea). Keep the existing "Core Integration Philosophy" and
      pipeline-diagram cards — those are good, keep them as read-only
      context above the editable form.
- [ ] Reconcile the duplicate AI toggle: `SettingsPage`'s "Analysis
      Defaults" already has "Include AI explanation" — keep that one
      (it's the per-analysis default) and make it clear in the AI tab's
      copy that model/provider config is global while that toggle is
      per-run, so it's obvious why both exist instead of looking redundant.

**Exit criteria:** Settings is one coherent page with tabs, not a menu of
redirects; AI Settings is the one place that actually needs a save button
that it doesn't have today.

---

## Phase 6 — Cross-cutting: legend, edit/delete, export, graph UX (1–1.5 weeks)

- [ ] **Legend/key on both graphs.** Add a `Panel` (React Flow already
      supports this — you're already using it for the node/edge count badge)
      listing each `TYPE_COLORS`/`RISK_COLOR` entry with its swatch and
      label. One shared `<GraphLegend>` component used by both
      `ArchitecturePage` and `AnalysisResultsPage`.
- [ ] **Project edit/delete UI.** Add "Edit" (inline rename/description
      form) and "Delete" (confirmation modal — this is destructive and
      cascades, per Phase 1) buttons next to the existing "Open"/"Re-index"
      buttons on `ProjectsPage`.
- [ ] **Wire up exports.** Give `ReportDetailPage`'s "Export PDF" button its
      missing `onClick`, and add an equivalent export action anywhere else
      results are shown (Analysis Results, Security Center) — same pattern,
      calling the Phase 1 export endpoint with `format=pdf` or `format=xlsx`
      depending on a small format dropdown next to the button.
- [ ] **Graph exploration UX**, in order of effort-to-value:
  1. **Focus mode**: double-click a node to show only it + its direct
     neighbors (dim/hide the rest); click background to reset. This alone
     fixes most of "hard to explore as it grows" without touching layout.
  2. **Real layout**: swap the fixed grid for `dagre` (fastest to integrate
     with React Flow) so large graphs don't sprawl into an unreadable wall.
  3. **Collapse by type**: let a whole `componentType` group (e.g. all
     `DATABASE_TABLE` nodes) collapse into a single summary node, expandable
     on click — most useful once repos get into the hundreds-of-nodes range.

**Exit criteria:** every "Important remaining issue" from your list is
closed; large-graph exploration no longer means scrolling through an
unreadable mass of nodes.

---

## Suggested sequencing

| Week | Focus |
|---|---|
| 1–1.5 | Phase 1 (backend endpoints — everything else depends on this) |
| 2 | Phase 2 (node detail panel) |
| 2.5–3 | Phase 3 (fix impact graph edges + recommendations) |
| 3.5–4 | Phase 4 (security scan trigger) |
| 5–6 | Phase 5 (Settings rebuild) |
| 6.5–7.5 | Phase 6 (legend, edit/delete, export, graph focus mode) |

If you want to ship visible wins fastest before the backend work lands:
the `buildGraph()` edges: `[]` fix (Phase 3) and the dead "Export PDF"
button (Phase 6) are both one-file, near-zero-risk fixes you could do
today independent of this sequencing — everything else genuinely needs
Phase 1 first.
