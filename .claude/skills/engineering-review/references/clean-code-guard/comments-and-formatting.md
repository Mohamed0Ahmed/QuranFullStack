# Comments and Formatting — Clean Code Chapters 4 and 5

Source: Robert C. Martin, *Clean Code*. Summaries: Vivek Khatri Ch. 4, Vivek Khatri Ch. 5, LinkedIn summary of Ch. 4.

## Contents

- Comments
  - C1. Acceptable comments
  - C2. Banned comments
  - C3. Docstring discipline
- Formatting
  - Fmt1. Vertical openness separates concepts
  - Fmt2. Vertical density implies association
  - Fmt3. Vertical distance
  - Fmt4. Horizontal density
  - Fmt5. Match the file you're editing
- Self-check for comments and formatting

## Comments

> **This project is stricter than the chapter.** The canonical rule is *Comments are forbidden
> by default* in the root `CLAUDE.md`. Where this file and that rule disagree, **that rule wins**
> and this section is wrong. The list below is kept as the generic Clean Code position and as the
> reviewer's vocabulary for naming a violation — it is **not** the bar a comment must clear here.

The foundational rule: **"Don't comment bad code — rewrite it."** Comments are failures to express intent in code. Every comment is a candidate for rename or extract.

### C1. Comments that earn their keep — generic Clean Code, NOT this project's bar

Clean Code accepts these. **This project accepts only the second kind, and only when omitting it
would let a competent developer make a change that is wrong** — see the root `CLAUDE.md` for the
three-part test and the tool-directive carve-out.

- **Legal headers** — license boilerplate, copyright. *(Here: only in vendored/third-party files.)*
- **Intent** — explaining *why* a decision was made when the choice is non-obvious. Example: `// Use exponential backoff to avoid hammering the rate limiter during retries.` *(Here: the only surviving category, and it must clear the harm test.)*
- **Warnings of consequences** — `// This function is called during transaction commit; do not raise.` *(Here: this is a form of intent, and qualifies only if the harm is real.)*
- **TODOs** — sparingly, with a tracking ticket reference. *(Here: a TODO with no tracked item is forbidden outright.)*
- **Public API documentation** — docstrings that document *contract*. *(Here: forbidden. No XML doc or JSDoc on controllers, endpoints, DTOs, components or services — nothing consumes it.)*
- **Amplification** — calling attention to something non-obvious. *(Here: try a name or a README line first; amplification alone is not a justification.)*

### C2. Banned comments

Delete on sight:

- **Restating-code comments.** `// increment counter by one` above `counter += 1`. The comment adds zero signal and creates two things to maintain.
- **Noise comments.** `# default constructor`, `# getter`, `# returns the day of month`.
- **Banner comments.** `# ====== USER FUNCTIONS ======`. Use a class or module split instead.
- **Closing-brace comments.** `}  // end of for loop`. If you need this to follow the flow, the function is too long.
- **Attributions and journal comments.** `# Updated by Bob on 2023-04-01 to fix bug #42`. Version control records this.
- **Commented-out code.** Delete it. If you need it back, git has it. Commented blocks are toxic — readers don't know whether to trust them.
- **`Step 1` / `Step 2` scaffolding.** Common LLM artifact. Each step should be a function call with a name; the names provide the structure.

### C3. Docstring discipline

A documentation comment that paraphrases the function signature is noise:

**Bad:**
```text
add(a, b)
  // Adds a and b and returns the result.
  return a + b
```

**Good:**
```text
add(a, b)
  return a + b
```

A documentation comment earns its keep when it documents contract: what may be passed, what may be returned, what errors are raised, and any non-obvious side effects.

**Good:**
```text
// Charge a payment source.
// Returns: charge identifier.
// Raises: CardDeclined for decline failures; PaymentProviderError otherwise.
// Side effect: writes an audit record on success.
charge(paymentSourceId, amountCents)
```

---

## Formatting

### Fmt1. Vertical openness separates concepts

Blank lines between concepts. No blank lines inside a tightly-coupled block. The eye uses blank lines as boundaries.

### Fmt2. Vertical density implies association

Code that belongs together should sit together. Variable declared 30 lines from its use is a smell.

### Fmt3. Vertical distance

- **Variables declared close to use.** Not at the top of the function "C-style."
- **Caller above callee.** Top-down reading: high-level function first, then the helpers it calls. The step-down rule ([naming-and-functions.md](naming-and-functions.md) F4).
- **Conceptually related functions adjacent.** If `parse_invoice` and `validate_invoice` are siblings, put them next to each other, not on opposite ends of the file.

### Fmt4. Horizontal density

- Spaces around assignment and comparison operators: `x = 1`, `if x == 1`.
- No space between function name and parenthesis: `f(x)` not `f (x)`.
- Line length: 80 traditional, ≤100–120 acceptable. Beyond 120 is careless.

### Fmt5. Match the file you're editing

The most common cross-cutting violation: introducing a new style in a file that already had one. If the file uses snake_case, do not introduce camelCase. If it uses double quotes, do not introduce single quotes. If it sorts imports alphabetically, do not append at the bottom. If the project already has an HTTP client, database wrapper, or logging helper, reuse it instead of introducing another one.

Team rules override personal preference. Read the file, then write.

---

## Self-check for comments and formatting

Before you ship code:

1. Walk every comment you added. For each: could a competent developer make a **wrong** change without it? If not, delete it. "It explains why" is not sufficient here — see the root `CLAUDE.md`.
2. Walk every documentation comment you added. In this project, XML doc and JSDoc on controllers, endpoints, DTOs, components and services are forbidden regardless of content. Delete them.
3. Any commented-out code? Delete it.
4. Any `Step 1`, `Step 2`, `First, ...`, or `Then, ...` scaffolding comments? Delete.
5. Could the surviving comment be a better name, a smaller function, or a line in the nearest `README.md`? Then it is not a comment. Move it.
5. Are variables declared near their use, not at the top?
6. Does the casing, quoting, and import order match the file's existing style?
