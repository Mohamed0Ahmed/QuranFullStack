# Quickstart: Mushaf Reader Ayah Similarities

This quickstart verifies Feature 012 after implementation. It assumes Feature 011's HTTPS local development setup is already working.

## Preconditions

- Backend database contains the existing Quran foundation, Mushaf Reader data, and Feature 006 similarity/mutashabihat tables.
- Backend runs over HTTPS at `https://localhost:5015`.
- Frontend runs over HTTPS at `https://localhost:4200`.
- Browser trusts the local development certificates used by Feature 011.

## Start The Apps

From the backend repo:

```bash
dotnet run --project api/QuranDashboard.Api/QuranDashboard.Api.csproj --launch-profile https
```

From the frontend repo:

```bash
npm run start:https
```

## Smoke Test In The Browser

1. Open `https://localhost:4200/dashboard/mushaf`.
2. Load any Mushaf page.
3. Confirm the page renders without similarity counters on page ayahs.
4. Select an ayah.
5. Confirm the selected ayah study area still shows `التفسير`, `الترجمة`, and `الإعراب الكامل`.
6. Confirm the selected ayah study area also shows `آيات قريبة في المعنى` and `المتشابهات اللفظية للحفظ`.
7. Confirm the action labels can show or imply counts from the selected ayah similarity summary.
8. Open `آيات قريبة في المعنى` and confirm a flat list or the Arabic empty state appears.
9. Open `المتشابهات اللفظية للحفظ` and confirm grouped cards/sections or the Arabic empty state appears.
10. Copy the URL while each new action is active and reopen it in a fresh tab; confirm the selected ayah action restores.

## Backend API Smoke Tests

Use a selected ayah known to exist, for example `2:25` if present in the seeded fixture.

Selected ayah study should include `similaritySummary`:

```bash
curl -k "https://localhost:5015/api/mushaf/ayahs/2%3A25/study"
```

Similar ayahs should return a flat payload:

```bash
curl -k "https://localhost:5015/api/mushaf/ayahs/2%3A25/similar-ayahs"
```

Mutashabihat should return grouped payload:

```bash
curl -k "https://localhost:5015/api/mushaf/ayahs/2%3A25/mutashabihat"
```

Malformed verse key should produce a controlled validation response:

```bash
curl -k "https://localhost:5015/api/mushaf/ayahs/not-a-key/similar-ayahs"
```

## Verification Expectations

- Mushaf page response has no similarity counters or detail payloads.
- Selected ayah study response includes `similaritySummary`.
- Similar ayahs are flat and deduplicated.
- Mutashabihat are grouped and never flattened.
- Ayah text appears from canonical ayah data.
- Phrase text, when present, matches the canonical word-span range.
- No database writes or migrations are required.

## Suggested Test Commands

Backend:

```bash
dotnet test
```

Frontend:

```bash
npm test
```

If a project uses a more specific test target by the time tasks are generated, prefer that target in `tasks.md`.
