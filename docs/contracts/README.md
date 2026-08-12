# Contracts index

Index only — defers to the linked code and applicable architecture authorities.

## What this layer is

`docs/contracts/` is a **thin pointer index** to the current contract truth of this
monorepo. It restates **no** contract content — no routes, no DTO fields, no counts,
no identity rules, no schemas. Each page links to the authoritative source.

## Precedence (truth model)

Current **code** is implemented truth. Applicable `.architecture/` sources govern structural
rules, and this index **defers** to both. Per-feature, planning-time contracts live in
`specs/<feature>/contracts/` during a feature's development; this index covers steady-state code
after merge. Merged features 001–019 are historical and **not** scanned routinely — see
[`../../specs/README.md`](../../specs/README.md).

## Pages

- [HTTP API — route families](./http-api.md)
- [Response envelope](./response-envelope.md)
- [Words explorers — reads, identity, counts](./words-explorers.md)
- [Mushaf reader](./mushaf-reader.md)
- [Abwab — gates tree, relations, templates](./abwab.md)
- [Security access — identity and authorization](./security-access.md)
- [Import pipelines & CLI verbs](./import-pipelines.md)
- [Frontend shell — navigation, tokens, URL-state](./frontend-shell.md)

## Related

- Workspace docs layer: [`../README.md`](../README.md)
- Per-feature planning workspace (001–019 frozen): [`../../specs/README.md`](../../specs/README.md)
