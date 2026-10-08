# PayFlow HR — redesigned project

The complete source project is included. The redesigned frontend uses the original React/Vite architecture and existing API contracts. Three generated design references are in `docs/design-references/`; their fictional numbers and extra menus are concept content, not production data or screenshots of the running app.

## Run the whole stack

From this project folder:

```powershell
docker compose up -d --build
```

Open http://localhost:5173. The API uses port 5005 and PostgreSQL uses host port 5434. Docker builds the frontend with Node 24. The backend and database configuration are the original project configuration.

## Local development

Use Node 24.15 or newer and the .NET 10 SDK.

Terminal 1, from the project root:

```powershell
docker compose up -d db
dotnet run --project src/PayFlow.Api
```

Terminal 2, from the project root:

```powershell
cd src/payflow-web
npm ci
npm run dev
```

Open http://localhost:5173. The Vite proxy routes `/api` to the API on port 5005. Avoid running the Docker web container and Vite on the same port at the same time.

## Checks

From `src/payflow-web`:

```powershell
npm run build
npm run lint
npm run test:dom
```

The DOM tests mount the actual React application with API fixtures. They do not contact your database or submit real payroll actions.

Optional browser verification, with the frontend running in another terminal:

```powershell
npx playwright install chromium
npm run test:ui
```

Browser checks also use API fixtures and save screenshots under the frontend's `docs/screenshots` directory. This suite is included for local follow-up and was not executed successfully in the creation environment.

Full validation details: `docs/FRONTEND_REDESIGN.md`.

The archive omits installed dependencies, build outputs and Git history. `npm ci` and the normal .NET restore/build recreate dependencies. No database dump is included.
