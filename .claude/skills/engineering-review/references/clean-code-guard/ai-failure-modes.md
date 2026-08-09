# AI Review Failure Modes

Fourteen systematic ways LLM-generated code goes wrong, for the reviewer walking a diff.
Where a generic principle already has a canonical owner, the pointer is given instead of a
rule body: naming/comments/functions are `CODING_PRINCIPLES.md` §2 (comments: its
`Comment Policy`), SOLID is §3, DRY/KISS/YAGNI is §4, focused scope is §7.

1. **Catch-all error handling that swallows failures.** Broad catches returning
   null/empty "success" make an outage indistinguishable from an empty result. Catch only
   what the code can specifically recover from; a swallowed exception with no documented
   recovery path is a defect.
2. **Defensive guards for impossible cases.** Null/type/truthiness checks for states the
   type system or caller contract already excludes. Trust the contract.
3. **Premature abstraction.** Interfaces, factories, strategies, or plugin hooks with one
   concrete implementation (canonical rule: §4 YAGNI). One implementation = inline it.
4. **Comment pollution.** Restating-code comments, "Step N" scaffolding, doc comments that
   paraphrase signatures. The canonical bar is §2 `Comment Policy` — stricter than the
   generic "explains why" standard.
5. **Duplication instead of reuse.** Inline copies of logic a repo helper already provides
   (canonical rule: §4 DRY). A ≥5-line block matching existing code should call it.
6. **Hallucinated APIs and packages.** Imports, methods, or signatures that do not exist
   in the installed version. Verify against the actual dependency, not what "should exist".
7. **Generic, intent-less naming.** `data`, `result`, `item`, `temp`, `value`, `obj`,
   `info`, `helper`, `manager`, and unqualified `handle_*`/`process_*`/`do_*`
   (canonical rule: §2).
8. **Long multi-concern functions.** One function does one thing (canonical rule: §2);
   watch for functions assembled from several generated fragments mixing I/O, logic,
   formatting, and side effects.
9. **Parameter explosion.** Five or more parameters that should be a typed request/config
   object.
10. **Inconsistency with surrounding code.** New casing/import/error/logging styles, or a
    second HTTP/database/logging utility where the project already has one. Read the file
    and a neighbor before judging an addition consistent.
11. **Dead code.** Unused imports and symbols, unreachable branches, half-implemented
    "just in case" exports.
12. **Mock fallbacks declared as success.** Hardcoded success values or fixture data on a
    production code path that should do real work; tests disabled, skipped, or weakened to
    pass. Failing explicitly is correct; fictional success is critical.
13. **Plausible-but-wrong code.** Compiles and reads correctly but encodes a slightly
    wrong formula, range, or null semantic — often copied from a similar-but-different
    function. Enumerate the boundary cases (empty / one / even / odd / null) and check the
    logic against the spec, not against the neighboring function.
14. **Speculative configurability.** Flags, env vars, and optional parameters with no
    present-day caller (canonical rule: §4 YAGNI).

The common root cause is a bias toward emitting more code than the spec requires — more
guards, parameters, abstractions, comments. The review question for each hunk: *does the
spec require this, today?*
