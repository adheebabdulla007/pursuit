# Pursuit client

The Pursuit client is a React and TypeScript application for job seekers, employers, and administrators. Vite builds the static application, and Nginx serves the production container.

## Recommended setup

The client depends on the Pursuit API and its supporting services. For the complete application, start the Docker Compose stack from the repository root as described in the [main README](../README.md#try-it-locally).

## Run the client during development

Install the Node.js version recorded in [`.nvmrc`](../.nvmrc), then install dependencies:

```text
npm ci
```

Create the client environment file.

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

macOS or Linux:

```bash
cp .env.example .env
```

The example points `VITE_API_BASE_URL` to the API's HTTP development launch profile at `http://localhost:5146`. The Docker build sets the container client to `http://localhost:5000` instead.

Start Vite:

```text
npm run dev
```

## Verification commands

```text
npm run lint
npm test
npm run build
```

The Playwright journey also needs the API and its test dependencies:

```text
npm run e2e:infra:up
npx playwright test
npm run e2e:infra:down
```

The browser test registers an employer, publishes a job, registers a job seeker, submits an application, and verifies logout token revocation.

## Client boundaries

- API calls live in `src/api`.
- Authentication state lives in `src/context`.
- Route-level pages live in `src/pages`.
- Shared interface components live in `src/components`.
- Component tests stay beside the source file; the complete browser journey lives in `tests`.

The client uses HttpOnly authentication cookies. Unsafe requests first obtain an antiforgery token and send it through the `X-CSRF-TOKEN` header.
