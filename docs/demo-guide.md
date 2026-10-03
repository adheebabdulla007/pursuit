# Local demo guide

## Start the full stack

From the repository root:

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

macOS or Linux:

```bash
cp .env.example .env
```

Start the stack:

```text
docker compose up --build -d --wait
```

Open [http://localhost:5173](http://localhost:5173).

The first build downloads the .NET, Node, SQL Server, Redis, RabbitMQ, Azurite, and Nginx images. Later starts reuse those layers.

If Docker reports a daemon connection error, start Docker Desktop or the Docker service and confirm `docker info` succeeds.

## Five-minute product flow

### 1. Create an employer

1. Open **Register**.
2. Choose **Employer**.
3. Enter a company name and account details.
4. Submit the form.

The browser receives HttpOnly access and refresh cookies. Public registration cannot create an administrator.

### 2. Post a job

1. Choose **Post a Job**.
2. Enter a title, description, location, salary range, and job type.
3. Submit and open the new job page.

Copy the job title for the job-seeker search.

### 3. Create a job seeker

1. Log out.
2. Register a second account with the default **Job Seeker** role.
3. Search for the job by its title.
4. Open the result.

### 4. Apply with a resume

Upload a PDF or DOCX file smaller than 5 MB and select **Apply**. The API stores the file in Azurite, inserts the application in SQL Server, and publishes an event to RabbitMQ.

The RabbitMQ management page is available at [http://localhost:15672](http://localhost:15672). The local broker uses the standard development `guest` account.

### 5. Inspect the administrator view

Log out and sign in with the local development account:

```text
Email: admin@pursuit.local
Password: PursuitDevAdmin1!
```

Open **Admin** to view counts, page through users, and activate or deactivate an account. These credentials live only in `appsettings.Development.json`; production administrator creation is disabled by default.

## Automated browser journey

The Playwright test performs the employer and job-seeker flow without shared test data:

```powershell
Set-Location client
npm ci
npm run e2e:infra:up
npx playwright test
npm run e2e:infra:down
```

The test creates unique users and a unique job on each run. It also checks logout cookie removal and rejection of the old refresh token.

## Reset local data

```powershell
docker compose down -v
docker compose up --build -d --wait
```

Removing the volume deletes all local users, jobs, applications, and refresh tokens.
