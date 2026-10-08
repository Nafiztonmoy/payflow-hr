# Frontend redesign

## Delivered

A warm white, evergreen and emerald design system based on three generated UI references. Shared navigation, cards, tables, forms, dialogs and typography now use one visual language. The references are concepts; implementation follows the actual APIs and role permissions.

Updated screens: login; role dashboards; employees and compensation history/revisions; organization; salary structures; payroll workspace and exceptions; attendance/corrections; leave balances, submission, approval and rejection; personal payslip history; printable statements; reports; audit trail.

## Behavior improvements

- Removed fabricated dashboard metrics and report fallback charts. Missing data has explicit loading/error/empty states.
- `/compensation-plans` opens salary structures directly.
- Report requests and sections follow backend role permissions.
- Each authenticated identity gets a separate query cache; switching users unmounts/cancels the old workspace. Failed persona login preserves the current session. Expired API sessions return to login.
- Employee pagination is functional. Compensation forms start from existing compensation rather than invented amounts. History displays the actual salary structure name.
- Attendance exposes all months and a selectable year. Employee views omit correction controls.
- Leave rejection requires a reviewer-entered note; submission validates required fields and date ordering.
- Payroll has a run list, progress steps, totals, item/exception tabs and preserved backend lifecycle calls. Approved and paid runs cannot be recalculated. Blocking exceptions disable approval.
- Bank detail correction is shown only where the existing update API authorizes it. No sample account/routing numbers are prefilled.
- Payment recording explicitly records an external payment; it does not initiate a bank transfer. Payment references and resolution notes require user input.
- CSV export uses the authenticated API client. Printable payslips use returned currency and omit invented address/tax identifiers and transfer claims.
- Dialogs gain accessible titles, Escape handling, focus management, keyboard focus containment and body scroll locking. Existing form labels are associated with fields. Motion honors reduced-motion preferences.
- Routes load lazily. Financial values align for scanning; narrow tables scroll within their panels. Mobile navigation and layouts are implemented in CSS, but their rendered appearance has not been verified here.
- Frontend CI now uses Node 24 and runs lint plus DOM checks.

## Verification

- Production frontend build and TypeScript check: PASS.
- Oxlint: PASS with no warnings.
- React DOM regression suite: PASS with mocked API fixtures.
- Covered flows: login/logout; all main routes; five role views; salary route; employee pagination and compensation revision; dialogs; time off submission/rejection; payroll exception resolution and lifecycle; approval conflict recovery; immutable approved cycle; payslip values; report query boundaries; failed persona login; API retry; empty states.
- API/domain/infrastructure C# sources and backend test sources are byte-for-byte unchanged from the supplied archive.

## Limits

The .NET SDK and Docker are unavailable in the creation environment, so backend tests, live PostgreSQL/API integration and Docker build were not run. Local Chromium failed to start, and the cloud browser could not reach the local server. Browser layout, screenshots, responsive behavior, actual download/print rendering and browser keyboard interactions remain unverified. `tests/ui-smoke.mjs` is provided for local browser follow-up; it has not completed here.

DOM tests use JSDOM and synthetic chart dimensions. They test React behavior and request contracts, not browser layout or real backend authorization. Production source contains no test-data substitution. Original backend authorization/data scope remains unchanged; this redesign does not certify production security or jurisdictional payroll rules.

## Reference mapping

- `design-references/01-dashboard-reference.png`: overall palette, sidebar, metric cards and dashboard hierarchy.
- `design-references/02-people-reference.png`: directory, filter toolbar and readable row density.
- `design-references/03-payroll-reference.png`: run list, cycle steps, exception emphasis and payroll table.

Generated references contain fictional example text/metrics and should not be interpreted as shipped features or actual application screenshots.
