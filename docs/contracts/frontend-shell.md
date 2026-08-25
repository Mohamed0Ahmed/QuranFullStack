# Frontend shell — navigation, shared styling, URL-state

Index only — defers to the linked code and Frontend architecture authorities. See [docs/contracts/README.md](./README.md).

Covers the app shell: navigation items, current shared styling, shared building
blocks, and cross-feature URL-state conventions. This page does **not** restate token
values, nav tables, or route keys. No permanent visual design authority is active
during the UI rebuild.

## Authoritative sources

- Core (navigation, data-access, app shell) → [`app/core/`](../../Frontend/quran-dashboard-ui/src/app/core/)
- Shared building blocks → [`app/shared/`](../../Frontend/quran-dashboard-ui/src/app/shared/)
- Current styles / tokens → [`styles/`](../../Frontend/quran-dashboard-ui/src/styles/)
- Response envelope (frontend model) → [response-envelope.md](./response-envelope.md)

**Precedence:** frontend code owns implemented behavior. The applicable Frontend structural or API
authority governs its own boundary, while visual work follows the owner's active rebuild direction.
