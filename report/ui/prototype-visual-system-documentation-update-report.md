# Prototype Visual System — Documentation Update Report

**Type:** Documentation-only update. No Angular source, tokens, components, styles, or
backend changed. No commit.
**Decision adopted:** The **Real Pages prototype** (`/projects/Real Pages`) is now the
official visual source of truth for the Quran Dashboard frontend, **with adaptation**:
a **navy + gold + parchment** identity, a soft surface + shadow elevation ladder, a
light navbar, a dark navy footer, subtle card hover motion, and the prototype UI
typography direction. Quran/Mushaf fonts and rendering are unchanged. Themes remain
**light + dark** (ivory → light, midnight → dark; sage not adopted).
**Date:** 2026-06-20

Basis: the two prior read-only reports —
`report/ui/color-usage-audit-report.md` (flatness audit) and
`report/ui/real-pages-visual-system-extraction-report.md` (extraction, verdict **USE
WITH ADAPTATION**).

---

## 1. Docs Found and Updated

| Document | Path | Role | Updated |
|----------|------|------|---------|
| Product (Impeccable) | `/projects/Dashboard/App/PRODUCT.md` | Product strategy / register / principles | ✅ Yes |
| Design (Impeccable) | `/projects/Dashboard/App/DESIGN.md` | Visual system / palette / elevation / rules | ✅ Yes |
| Frontend UI style system | `/projects/Dashboard/App/Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` | Implementation contract for tokens/classes | ✅ Yes |

**Search performed** to locate docs (so nothing was missed): repo-wide search for
`PRODUCT.md`, `DESIGN.md`, `UI_STYLE_SYSTEM*.md`, and `*style-system*`. Results: one of
each. PRODUCT.md and DESIGN.md live at the **workspace root** (the Impeccable context
dir resolved by the skill loader). UI_STYLE_SYSTEM.md lives in the **frontend repo**
under `.architecture/`. No other product/design/style docs exist (FRONTEND_STRUCTURE.md
and API_INTEGRATION_GUIDELINES.md are non-visual and were left untouched).

---

## 2. Exact Design Decisions Now Documented

**Identity (all three docs):**
- Official visual identity = Real Pages prototype, adopted **with adaptation**.
- **Navy + gold + parchment**: warm parchment canvas, near-white elevated cards, deep
  navy structural color (primary/brand/footer), restrained gold accent for state.
- **Light + dark only.** Prototype *ivory* → Angular **light**; prototype *midnight* →
  Angular **dark**; *sage* explicitly **not** adopted. The three prototype theme names
  are **not** introduced into the app.

**PRODUCT.md (new "Visual Identity" section):**
- Calm Quranic research workspace; warm parchment light mode; deep navy structural
  identity; restrained gold accent; premium dark footer / dark anchor sections; subtle
  card elevation + hover movement; clean light navbar; calm non-distracting motion;
  Quran text rendering stays sacred/stable and is never animated.
- Explicit statement that this **supersedes the earlier green / teal / petrol chrome
  exploration**.

**DESIGN.md:**
- §1 Overview + Key Characteristics rewritten to navy + gold + parchment with a real
  (soft) elevation ladder.
- §2 Colors retitled **"Navy + Gold + Parchment"** with light/dark/footer **reference
  value tables** and the note that implementation re-authors them as OKLCH `--qd-*`
  tokens for both themes.
- §3 Typography: UI fonts named (IBM Plex Sans Arabic + IBM Plex Sans, weights
  400/500/600/700); Quran/Mushaf fonts (Amiri etc.) marked **sacred and unchanged**.
- §4 retitled **"Elevation and Motion"**: surface ladder + hairline borders +
  **controlled soft-shadow ladder** (`shadow-sm` resting → `shadow` hover → `shadow-lg`
  floating); card hover = stronger border + stronger shadow + `translateY(-2px)`; no
  scale; two-token motion contract (≈140ms / ≈220ms `cubic-bezier(.2,.7,.3,1)`),
  reduced-motion respected, Quran text never animated.
- §5 Do's/Don'ts reconciled (see §3 below).

**UI_STYLE_SYSTEM.md (new Section 15 — "Prototype-Derived Implementation Contract"):**
- **A. Typography** — UI fonts + weights; Quran fonts unchanged.
- **B. Color roles** — full role table (page/section/card/recessed bg, border,
  border-strong, primary, primary-fg, accent + hover/soft/tint, footer-bg/bg-2/text/
  muted/accent/border, focus ring) with light reference hex + suggested `--qd-*` token
  names, plus dark reference values.
- **C. Navbar** — light/near-white, distinct from cards, optional perf-gated backdrop
  blur with opaque fallback, subtle border/shadow, active = gold text on accent-tint
  pill, hover = quiet surface + accent text, no heavy colored navbar.
- **D. Footer** — dark navy anchor, warm off-white text, muted blue-grey secondary,
  gold headings/links, gradient top hairline, dedicated footer tokens, and a directive
  to fix the undefined `--qd-text-meta` reference during implementation.
- **E. Cards/elevation** — resting border + soft shadow; hover stronger border +
  stronger shadow + ~`-2px` lift; mini ~`-1px`; feature shadow-only; no scale;
  quiet/bordered variants.
- **F. Motion** — two-token contract, subtle only, no bounce, reduced-motion, animate
  transform/opacity/shadow/color only, never Quran text.
- **G. Buttons/active** — primary = navy; gold accent for active/links/icons/eyebrows;
  soft = accent-tint; ghost = border + accent hover; selected = accent-tint + accent
  border/text; chips distinct from card.
- **H. Phasing** — Phase 1 navbar+footer → 2 global tokens/surface ladder → 3 card
  elevation → 4 buttons/active → 5 Mushaf/study polish; additive migration keeping
  existing tokens working; never paste prototype CSS.
- §4 token categories and rules updated to the new role set and elevation stance.

---

## 3. Conflicts Resolved

| Prior doc statement | Location | Resolution |
|---------------------|----------|------------|
| "Flat by default … depth … not drop shadows"; **The Flat-By-Default Rule** | DESIGN.md §4 | Replaced with **Elevation and Motion** + the **Soft-Elevation Rule**: controlled soft shadows are **required** for elevation (resting/hover/floating ladder). |
| "depth comes from tonal layering and hairline borders, not shadows (the Flat-By-Default Rule)" | UI_STYLE_SYSTEM.md §4 | Rewritten to "surface ladder + hairline borders + controlled soft shadows together"; controlled soft shadows required, not banned. |
| "No pure white, no pure black"; "Pure `#fff` and `#000` are forbidden" (Warm Neutral Rule) | DESIGN.md §2/§5 | Revised: parchment **canvas** stays tinted (no pure-white page, no pure `#000`), but **near-white/white elevated cards are allowed** when paired with parchment bg + border + soft shadow. |
| "Do not use pure black or pure white as default … depth … not shadows" | UI_STYLE_SYSTEM.md §4 | Updated to the revised Warm Neutral Rule + Soft-Elevation Rule. |
| "one muted earthy accent" / Muted Earthy Accent primary | DESIGN.md §1/§2 | Replaced with **gold accent + navy structural primary** (navy + gold + parchment). |
| "Don't use decorative gradients … glassmorphism …" (blanket) | DESIGN.md §5 | Kept the ban on **decorative** gradients/gradient-text and **glassmorphism-as-default** and side-stripes, but scoped **purposeful** exceptions: footer gradient top hairline + optional perf-gated navbar backdrop blur. |
| Anti-reference "Gold filigree …" could read as banning the gold accent | PRODUCT.md | Clarified: the ban is on decorative gold **ornament/filigree**; the restrained gold **accent color** is a different thing and is allowed. |
| "green / teal / petrol" chrome direction (existed only in conversation, never in committed docs) | — | Superseded explicitly in PRODUCT.md's Visual Identity section; no committed doc had to be rewritten for it. |
| `ivory/sage/midnight` as potential Angular theme names | all three docs | Documented that the app stays light/dark only; ivory→light, midnight→dark, sage not adopted; prototype names not introduced. |

---

## 4. Docs Intentionally Not Changed

- **`Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`** — structural/file-organization rules, not visual; out of scope.
- **`Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md`** — API integration, not visual; out of scope.
- **`Frontend/quran-dashboard-ui/CLAUDE.md` / `AGENTS.md` / `README.md`** — already point to PRODUCT.md / DESIGN.md / UI_STYLE_SYSTEM.md, which now carry the new direction; no edit needed.
- **`CODING_PRINCIPLES.md`, `CLAUDE.md` (root), Backend docs, `specs/`, `docs/`** — unrelated to the visual identity decision; untouched.
- **The two prior reports** (`color-usage-audit-report.md`, `real-pages-visual-system-extraction-report.md`) — historical inputs; left as-is.
- **The prototype source** (`/projects/Real Pages`) — read-only reference; untouched.

---

## 5. Source Files Not Touched — Confirmation

- **No Angular source code changed.** `git status -- src/` in the frontend repo is clean.
- **No tokens changed.** `_tokens.scss` / `_themes.scss` and all SCSS untouched (no `src/` changes).
- **No components changed.** No `.ts` / `.html` / component `.scss` modified.
- **No styles implemented.** This update is documentation prose only.
- **No backend code changed.** `git status -- Backend/` in the workspace repo is clean.
- **No build/test run, no formatting run, no Angular config changed.**

---

## 6. Git Status Summary

```
# Workspace repo (App) — branch main
 M DESIGN.md
 M PRODUCT.md
 m Frontend/quran-dashboard-ui      # submodule: UI_STYLE_SYSTEM.md edit + untracked report/

# Frontend repo (quran-dashboard-ui) — branch main
 M .architecture/UI_STYLE_SYSTEM.md
?? report/                          # untracked: the three ui reports (incl. this one)

# Verified clean:
#   Frontend/quran-dashboard-ui  src/    -> clean (no source/token/component changes)
#   Backend/                             -> clean
```

Changed files: **3 documentation files** (`PRODUCT.md`, `DESIGN.md`,
`.architecture/UI_STYLE_SYSTEM.md`) plus the untracked `report/` folder. Nothing else.

---

## 7. No Commit — Confirmation

No `git add`, `git commit`, or `git push` was run. All changes remain in the working
tree of their respective repos for review. Implementation (tokens, components, styles)
is **not** started; it begins only when a phase is explicitly requested, per Section 15H
of `UI_STYLE_SYSTEM.md` (Phase 1 = Navbar + Footer chrome).
