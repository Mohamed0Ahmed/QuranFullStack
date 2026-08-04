# Contracts index

Index only — defers to the linked code + README, which are the authority. See docs/contracts/README.md.

## What this layer is

`docs/contracts/` is a **thin pointer index** to the current contract truth of this
monorepo. It restates **no** contract content — no routes, no DTO fields, no counts,
no identity rules, no schemas. Each page links to the authoritative source.

## Precedence (truth model)

Current **code + the nearest `README.md`** is the authority ("current truth"). This
index **defers** to them; where this index and a README/code disagree, **the
README/code wins**. Per-feature, planning-time contracts live in `specs/<feature>/contracts/`
during a feature's development; this index covers the steady-state truth (code + README)
after merge. Merged features 001–019 are historical and **not** scanned routinely — see
[`../../specs/README.md`](../../specs/README.md).

## Pages

- [HTTP API — route families](./http-api.md)
- [Response envelope](./response-envelope.md)
- [Words explorers — reads, identity, counts](./words-explorers.md)
- [Mushaf reader](./mushaf-reader.md)
- [Abwab — gates tree, relations, templates](./abwab.md)
- [Import pipelines & CLI verbs](./import-pipelines.md)
- [Frontend shell — navigation, tokens, URL-state](./frontend-shell.md)

## Related

- Workspace docs layer: [`../README.md`](../README.md)
- Per-feature planning workspace (001–019 frozen): [`../../specs/README.md`](../../specs/README.md)
