# Frontend Structure Guide

## Purpose

This document defines lightweight structure rules for the Quran Dashboard Angular
app, plus file size and responsibility thresholds.

Read this file **before** adding or changing:

- Angular components, services, or routes
- state / facade / store files
- feature folder structure
- frontend file organization

For visual / style-system rules (colors, themes, tokens, `qd-` classes, RTL),
read:

- `.architecture/UI_STYLE_SYSTEM.md`

For product and visual context:

- `../../PRODUCT.md`
- `../../DESIGN.md`

> Scope note: this is documentation/rules only. It does not create components,
> services, routes, or features — it defines how that work must be organized when
> it is explicitly requested. This file is intentionally minimal for now and will
> grow as the frontend foundation is built.

## Minimum Structure Guidance

Keep the structure simple and feature-first until there is a real reason to add
more.

- **Organize by feature**, not by technical type. Prefer
  `src/app/features/<feature>/` over global `components/`, `services/`, `models/`
  dumping folders.
- Keep truly shared, cross-feature building blocks in a small `src/app/shared/`
  (or `core/` for app-wide singletons). Do not let these become dumping grounds.
- **Components use separate `.html` and `.scss` files by default.** Do not inline
  templates in TypeScript except for a tiny inline component with explicit
  approval.
- Separate responsibilities:
  - **API services** call HTTP endpoints and map basic API responses.
  - **Facade / store / state services** own page/feature state and orchestration.
  - **Components** coordinate UI state and user interactions.
  - **Pure helpers** hold focused, side-effect-free logic near their feature.
- Routes/feature wiring should stay thin and predictable; do not hide business
  logic in route configuration.
- Follow `UI_STYLE_SYSTEM.md` for all visual styling — compose shared `qd-`
  classes and tokens instead of recreating styles per component.
- Arabic-first / RTL is the default; respect it in structure and layout.
- Quranic data safety: never invent Quranic text or labels; show missing data as a
  controlled state, never silently fabricated.

## File Size and Responsibility Guidelines

File size limits here are **review thresholds, not blind automatic failures**. A
file that exceeds a threshold is a strong signal that a component or service is
doing too much and should be reviewed and justified — not a number to satisfy
mechanically.

Principles:

- File size limits are review thresholds, not blind automatic failures.
- Do not generate huge Angular components or services.
- Never put HTML templates inside TypeScript files unless it is a tiny inline
  component with explicit approval.
- Angular components should use separate `.html` and `.scss` files by default.
- A component / service with thousands of lines is not acceptable.
- A 1000+ line Angular file usually means the component/service is doing too much.
- A 3000+ line service/component is not acceptable and must be split before
  completion.
- Split by feature, UI responsibility, state responsibility, or API
  responsibility.

### Frontend thresholds

Thresholds are line counts per file. A **soft** threshold means "review and
justify"; a **hard** threshold means "stop and split, or split immediately".

#### 1. Angular component TypeScript files

- Ideal: 150–250 lines
- Soft review threshold: 300 lines
- Hard review threshold: 400 lines

Rules:

- Component TS should coordinate UI state and user interactions only.
- Do not put large business logic, formatting pipelines, API orchestration, or
  repeated helpers inside the component.
- If it grows, split into child components, facade/store services, pure helpers, or
  feature services.

#### 2. Angular component HTML templates

- Ideal: 150–250 lines
- Soft review threshold: 300 lines
- Hard review threshold: 400 lines

Rules:

- If a template grows too large, split it into child components.
- Do not build full pages as one giant template.
- Keep repeated sections as reusable components.

#### 3. Angular component SCSS

- Ideal: under 150 lines
- Soft review threshold: 200 lines
- Hard review threshold: 300 lines

Rules:

- Component SCSS should stay local and small.
- Repeated visual patterns must move to the global style system using `qd-`
  classes.
- Do not create a component-level design system.
- Do not redefine global cards / buttons / inputs / tables / modals inside
  component SCSS.

#### 4. Frontend API services

- Ideal: 100–200 lines
- Soft review threshold: 250 lines
- Hard review threshold: 350 lines

Rules:

- API services should mainly call HTTP endpoints and map basic API responses.
- They must not own page state, complex UI logic, business workflows, or formatting
  logic.
- Split by backend resource / feature when needed.

#### 5. Frontend facade / store / state services

- Ideal: 200–350 lines
- Soft review threshold: 400 lines
- Hard review threshold: 600 lines

Rules:

- State / facade services can be larger than API services, but must remain
  cohesive.
- Split by workflow, state slice, or feature area when they become too broad.
- Avoid oversized stores that own unrelated modals, filters, data loading, selection,
  drag/drop, and persistence all in one file.

#### 6. Frontend utility / helper files

- Ideal: under 150 lines
- Soft review threshold: 200 lines
- Hard review threshold: 300 lines

Rules:

- Helpers must be pure and focused.
- Do not create generic dumping files like `helpers.ts` or `utils.ts` with
  unrelated functions.
- Put helper functions near the feature unless truly shared.

### Frontend review behavior

If a frontend file is expected to exceed its **soft** threshold, the agent must:

- mention it in the plan or final response
- explain why the size is justified
- explain why splitting is not better

If a frontend file is expected to exceed its **hard** threshold, the agent must:

- stop and propose a split before implementing, or
- split the file immediately into cohesive smaller files

If a frontend file would exceed **1000 lines**:

- do not proceed without explicit human approval
- propose a concrete split plan
