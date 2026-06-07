# Contract: UI Navigation & Routes

The single navigation source of truth lives in `core/navigation/nav-items.ts` and drives both
the top navbar and `app.routes.ts`. Route paths are the stable contract; Arabic labels may change
later without changing routes/keys.

## NavItem list (canonical)

| key | labelAr | route | group | page this phase |
|-----|---------|-------|-------|-----------------|
| `dashboard` | لوحة التحكم | `/dashboard` | primary | **real home page** |
| `mushaf` | المصحف والآيات | `/mushaf` | primary | shared placeholder |
| `words` | الكلمات والجذور | `/words` | primary | shared placeholder |
| `tafsirs` | التفاسير | `/tafsirs` | primary | shared placeholder |
| `gates` | الأبواب | `/gates` | primary | shared placeholder |
| `resources` | المصادر | `/resources` | primary | shared placeholder |
| `i3rab` | الإعراب | `/i3rab` | more | shared placeholder |
| `translations` | الترجمات | `/translations` | more | shared placeholder |
| `audio` | الصوتيات | `/audio` | more | shared placeholder |
| `mutashabihat` | المتشابهات | `/mutashabihat` | more | shared placeholder |
| `settings` | الإعدادات | `/settings` | actions | shared placeholder |

## Routing rules (`app.routes.ts`)

- `''` → redirect to `dashboard` (`pathMatch: 'full'`).
- `dashboard` → the real home page component (`features/dashboard`).
- every other route above → the shared `PlaceholderPageComponent` with `data: { titleAr: <labelAr> }`.
- `**` (wildcard) → redirect to `dashboard` (Clarification Q1: unknown routes go home).

## Navbar rules

- `primary` items render directly in the bar.
- `more` items render inside a «المزيد» dropdown menu.
- `actions` items (Settings) render in the user/actions area, not the main nav list.
- The active route's item shows an active state (via `routerLinkActive`).
- On small screens the bar collapses into an accessible menu containing all items.
- Labels come from `labelAr`; navigation links target `route` (never component classes).

## Placeholder page

- Title = the route's `titleAr`.
- Body (fixed Arabic): «سيتم ربط هذا القسم ضمن خطة الميزات التالية.»
- Calm styling (no "coming soon", no error styling).
