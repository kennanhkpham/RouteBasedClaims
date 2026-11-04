# Local Docker Compose environment

The default Compose stack runs the Angular frontend, Nginx API gateway, and three independently built ASP.NET Core services:

```text
Browser ── localhost:8082 ──> Angular + Nginx
                                  ├── /api/auth/*    ──> AuthService ──> AuthDb
                                  ├── /api/claims/*  ──> ClaimsService ──> ClaimsDb
                                  └── /api/clients/* ──> ClientsService ──> ClientsDb

ClaimsService ── transactional outbox + service key ──> ClientsService
All three databases are hosted by the same SQL Server container, but remain separate.
```

`InsuranceClaimApi` remains available as the original reference implementation under the `legacy` Compose profile. Its migration job is opt-in under `legacy-migrations`; the default MSA startup does not migrate or otherwise initialize `RouteBasedClaimsDev`.

The SQL Server Developer edition image is for development and testing only. It is not licensed for production use.

## 1. Prepare Docker Desktop and local settings

1. Install and start Docker Desktop. Use Linux containers and confirm Docker Desktop has enough memory for SQL Server.
2. Keep any existing `.env` file and its values. Only create it from the example if it does not already exist:

   ```powershell
   if (-not (Test-Path .env)) { Copy-Item .env.example .env }
   ```

3. Ensure the private `.env` contains the required values. Preserve the configured ports (`SQL_PORT=1433`, `API_PORT=5000`, `FRONTEND_PORT=8082`):
   - `MSSQL_SA_PASSWORD`: use a private SQL Server password with uppercase, lowercase, a number, and a symbol; avoid semicolons and dollar signs.
   - `JWT_SIGNING_KEY`: use the same random key for AuthService token issuance and ClaimsService token validation.
   - `CLIENTS_SERVICE_API_KEY`: use a different random key for ClaimsService-to-ClientsService calls.
   - `ADMIN_BOOTSTRAP_USERNAME` and `ADMIN_BOOTSTRAP_PASSWORD`: local bootstrap administrator credentials.

   Generate a random secret in PowerShell without printing it:

     ```powershell
     $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
     $bytes = New-Object 'Byte[]' 32
     $rng.GetBytes($bytes)
     $rng.Dispose()
     $secret = [Convert]::ToBase64String($bytes)
     ```

   Assign separate generated values to the JWT and service-key settings. Keep any trailing `=` in Base64 values.

   `.env` is ignored by Git and must stay private. The example file contains placeholders only.

## 2. Build and start the services

From the repository root:

```powershell
docker compose config --quiet
docker compose up --build -d
```

The first build downloads the .NET, Node, Nginx, and SQL Server images and may take a few minutes. Compose:

1. Reuses the existing `routebasedclaim_sql_data` volume at `/var/opt/mssql` and waits for SQL Server health.
2. Builds separate ClaimsService, ClientsService, and AuthService images, each with its own migration bundle.
3. Runs the three independent migrations against `ClaimsDb`, `ClientsDb`, and `AuthDb`.
4. Starts each API only after its own migration succeeds. AuthService bootstraps the configured administrator.
5. Builds Angular and serves it through Nginx. Only the frontend and SQL Server ports are published for the MSA stack; backend service ports stay internal.

The volume is declared external with the exact existing name `routebasedclaim_sql_data`; Compose will fail rather than silently create an empty replacement if it is missing. Do not run `docker compose down -v`.

## 3. Open and test the application

- Frontend: <http://localhost:8082>
- AuthService Swagger: <http://localhost:8082/swagger/auth/index.html>
- ClaimsService Swagger: <http://localhost:8082/swagger/claims/index.html>
- ClientsService Swagger: <http://localhost:8082/swagger/clients/index.html>
- Legacy API Swagger, when explicitly started: <http://localhost:5000/swagger>
- SQL Server for SSMS: server `localhost,1433`, SQL Server Authentication, login `sa`, and the `MSSQL_SA_PASSWORD` from `.env`. For the local self-signed SQL certificate, enable **Trust server certificate** in SSMS.

Inside Docker, services use the Compose DNS name `sql`. From Windows/SSMS, use `localhost,1433`; `sql` is not a Windows hostname. The microservices have no published host ports.

In AuthService Swagger, verify registration and login:

1. Call `POST /api/auth/register` with a unique test username and a password of at least 12 characters. For example:

   ```json
   {
     "username": "docker-test-user",
     "password": "Local_Test_Password_123!"
   }
   ```

2. Copy the returned `token`. In ClaimsService Swagger, click **Authorize**, paste the token without adding `Bearer`, and authorize.
3. Call `POST /api/claims` with test data:

   ```json
   {
     "policyNumber": "DOCKER-TEST-001",
     "fullName": "Docker Test User",
     "email": "docker-test@example.com",
     "amountCost": 25.5,
     "prescriptionDetail": {
       "prescriptionNumber": "RX-DOCKER-001",
       "doctorName": "Dr. Example",
       "datePrescribed": "2026-10-29T00:00:00Z",
       "medicationName": "Test medication"
     }
   }
   ```

   A successful request returns **201 Created** and a generated claim number.

4. Call `GET /api/claims/{claimNumber}` with that number. Expect **200 OK** and the saved record.
5. In the frontend at `http://localhost:8082`, sign in with the test account and verify the UI can submit and look up a claim.
6. `/api/clients/{policyNumber}` is an Admin-JWT-protected profile lookup. ClaimsService writes profiles through a separate `/internal/clients/*` endpoint protected by the service key; Nginx never forwards or injects that key.
7. In SSMS, inspect each database separately. The default stack does not migrate or copy data from `RouteBasedClaimsDev`.

## 4. Useful Compose commands

```powershell
docker compose ps
docker compose logs -f sql
docker compose logs -f auth-migrate claims-migrate clients-migrate
docker compose logs -f auth-service claims-service clients-service
docker compose logs -f frontend
docker compose down
```

`docker compose down` stops and removes Compose containers but retains the external SQL volume. For rollback, stop the new services and restore the saved Compose/Nginx configuration; do not remove or recreate `routebasedclaim_sql_data`. The databases are local development data; use synthetic claims and do not load real insurance or health information into this stack.

## How the files fit together

- `Dockerfile.frontend`: multi-stage build; Node compiles Angular and Nginx serves the static output.
- `Services/*Service/Dockerfile`: builds each microservice and its independent EF Core migration bundle.
- `docker-compose.yml`: defines the three microservices, migration jobs, local ports, health/start ordering, and the external SQL volume.
- `docker/nginx/default.conf`: supports Angular route fallback and forwards the Auth, Claims, and Clients API paths through the gateway.
- `src/environments/environment.docker.ts` and the Angular `docker` build configuration: use the same-origin `/api` path for the Nginx proxy.
- `.dockerignore`: keeps dependencies, build output, local settings, and appsettings files out of the Docker build context.
- `.env.example`: documents required local settings; copy it to the ignored `.env` and replace placeholders.
