# Quickstart: Words Hub + Unique Words Explorer

This quickstart verifies Feature 014 after implementation. It assumes the existing HTTPS local development setup is already working and Feature 013 deterministic unique-word IDs are present.

## Preconditions

- Backend database contains Quran word rows and Feature 013 unique-word display tables.
- Backend runs over HTTPS at `https://localhost:5015`.
- Frontend runs over HTTPS at `https://localhost:4200`.
- Browser trusts the local development certificates used by the existing dashboard.

## Start The Apps

From the backend repo:

```bash
dotnet run --project api/QuranDashboard.Api/QuranDashboard.Api.csproj --launch-profile https
```

From the frontend repo:

```bash
npm run start:https
```

## Browser Smoke Test

1. Open `https://localhost:4200/dashboard/words`.
2. Confirm the Words hub shows `الكلمات الفريدة` as the active card.
3. Confirm `الجذور`, `الصيغة المعجمية`, `الأصل الصرفي`, and `أنواع الكلمة` show `قريبًا` and do not navigate.
4. Open `الكلمات الفريدة` and confirm the explorer defaults to `بالتشكيل`.
5. Switch to `إملائي (بدون تشكيل)` and confirm the URL state changes to the simple mode.
6. Search with Arabic input with and without tashkeel; confirm contains-style normalized results appear when matches exist.
7. Change sort and page; refresh the browser; confirm mode, search, sort, and page restore.
8. Open `السور` for a word; confirm a modal lists mentioned surahs and per-surah counts.
9. Open `لم يذكر في`; confirm a modal lists missing surahs or a clear empty state.
10. Open `الآيات`; confirm ayahs paginate and exact matched word occurrences are highlighted.
11. Copy a URL with a modal open and reopen it; confirm the selected word restores by stable ID.
12. Close the modal; confirm the list mode, search, sort, and page remain unchanged.

## Backend API Smoke Tests

Use a known stable unique-word ID from seeded data, for example `1` if present.

List unique words:

```bash
curl -k "https://localhost:5015/api/words/unique/tashkeel?page=1&pageSize=50"
```

Search unique words:

```bash
curl -k "https://localhost:5015/api/words/unique/simple?search=اسم&sort=mushaf-order&page=1&pageSize=50"
```

Load a selected word summary:

```bash
curl -k "https://localhost:5015/api/words/unique/tashkeel/1"
```

Load mentioned surahs:

```bash
curl -k "https://localhost:5015/api/words/unique/tashkeel/1/surahs"
```

Load missing surahs:

```bash
curl -k "https://localhost:5015/api/words/unique/tashkeel/1/missing-surahs"
```

Load ayah matches:

```bash
curl -k "https://localhost:5015/api/words/unique/tashkeel/1/ayahs?page=1&pageSize=20"
```

Malformed kind should produce a controlled validation response:

```bash
curl -k "https://localhost:5015/api/words/unique/not-a-kind?page=1&pageSize=50"
```

## Verification Expectations

- Hub and explorer are Arabic-first and RTL-first.
- Unique list shows Uthmani display text and all four counts.
- `missingSurahsCount = 114 - surahsCount`.
- Search uses normalized contains behavior.
- Drill-downs open as modals and preserve list context on close.
- Selected word restores by stable unique-word ID.
- Ayah highlighting uses exact matched word IDs, not string matching.
- Ayah markers never appear as highlighted matches.
- No database writes, migrations, imports, or new indexes are required.

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
