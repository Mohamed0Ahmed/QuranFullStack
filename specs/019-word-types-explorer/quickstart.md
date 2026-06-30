# Quickstart: Word Types Explorer

**Feature**: 019 — Word Types Explorer  
**Scope**: Planning artifact for implementation and verification. Commands assume the workspace root
is `/projects/Dashboard/App`.

## 1. Pre-Implementation Data Gate

Verify the live POS catalogue before implementing the reader:

```bash
sudo -u postgres psql -d quran_dashboard -X -A -F $'\t' -c "SELECT code, arabic_label, category FROM quran_pos_tags WHERE code = 'PRO';"
```

Expected:

```text
PRO    حرف نهي    particle
```

If the row is stale, stop implementation and apply the existing morphology reseed/data-correction
workflow first. Do not validate Feature 019 against stale `PRO` data.

## 2. Backend Build and Tests

```bash
cd /projects/Dashboard/App/Backend
dotnet build QuranDashboard.sln --disable-build-servers -m:1 -p:BuildInParallel=false -p:RestoreDisableParallel=true -v minimal
```

Targeted tests expected after implementation:

```bash
cd /projects/Dashboard/App/Backend
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter FullyQualifiedName~WordsWordTypes --logger "console;verbosity=minimal"
```

Required backend acceptance coverage:

- Tree has four main types and particle excludes `INL`.
- Tree node count equals paged table `TotalCount` for the same active type/child only when no secondary filter is applied.
- Secondary filters narrow table `TotalCount` and active UI count chips only; they do not require scoped tree counts.
- Out-of-bucket POS rows are excluded from noun/verb/particle/INL buckets.
- Multi-context displayed words produce separate rows with separate context-scoped counts.
- E3/E4/E5 never widen a selected row to all usages of the displayed word.
- Nominal/verb secondary filters validate by type and reject cross-type filters.
- Marker words never contribute to rows or counts.
- `ApiResponse<T>` status mapping uses `200`, `400`, and `404` consistently.

## 3. Frontend Build and Tests

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm run build
```

Targeted tests expected after implementation:

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test -- --include "src/app/features/words/**/*word-types*.spec.ts"
```

If the test runner does not support the include flag in the current Angular builder, run the full
frontend test suite:

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test
```

Required frontend acceptance coverage:

- `/dashboard/words/types` route loads and defaults to `type=noun`.
- URL state restores exact `word + contextCode`.
- Secondary filters appear only for their valid main type.
- Changing filter/sort clears selection and resets page.
- Secondary filter changes do not expect or render scoped tree counts.
- The table displays Uthmani-with-tashkeel words only.
- Null or deferred lemma/stem values render `—` and do not remove rows.
- Details ayah highlights are context-scoped and do not use text replacement.
- Loading, empty, error, and not-found states are explicit and calm.

## 4. Manual Smoke Flow

1. Start backend and frontend using the existing project dev commands.
2. Open `/dashboard/words/types`.
3. Confirm اسم is selected by default and rows load.
4. Switch to فعل, then choose ماض / مضارع / أمر and voice filters.
5. Switch to حرف وأداة and confirm no secondary filter appears.
6. Switch to حروف مقطعة and confirm disconnected letters are isolated.
7. Select a row, open الآيات, السور, and التحليل.
8. Confirm main-type selection loads the first page within the 2-second target in the local dev environment after initial app bootstrap.
9. Confirm the path from page open to a selected row's الآيات or التحليل view takes at most 4 interactions.
10. Copy the URL, reload, and confirm the same filters and exact selected row restore.

## 5. Non-Regression Checks

After Feature 019 implementation, existing Words explorers must keep their contracts and results:

```bash
cd /projects/Dashboard/App/Backend
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~Words" --logger "console;verbosity=minimal"
```

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test
```

At minimum, re-check Roots, Lemmas, Stems, and Unique Words routes manually if the full suites are not
practical in the current environment.
