# RouteBasedClaim

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.1.

## Claims portal architecture

The app uses an Angular frontend, an Nginx API gateway, and three independently deployable ASP.NET Core services; the browser never connects directly to SQL Server:

1. **UI and gateway:** Angular's `/intake` and `/status` pages use relative `/api` URLs, which Nginx routes to the backend services.
2. **AuthService:** owns accounts, hashes passwords, and issues JWTs accepted by ClaimsService.
3. **ClaimsService:** owns claims and prescription details in `ClaimsDb`; its transactional outbox sends client-profile updates to ClientsService.
4. **ClientsService:** owns client profiles in `ClientsDb`, validates the shared JWT for Admin-only profile reads, and accepts service-key-protected writes from ClaimsService.
5. **SQL Server:** hosts separate `AuthDb`, `ClaimsDb`, and `ClientsDb` databases. Services do not access each other's databases.

`InsuranceClaimApi` remains in the repository as the original reference implementation and is not part of the default MSA gateway.

For the original `InsuranceClaimApi` reference implementation only, configure its `DefaultConnection` connection string and ensure its database schema exists before using the claims portal. Start that API with its `https` launch profile (`https://localhost:7197`), then start Angular with `ng serve` (`http://localhost:4200`). Local Angular development uses the local HTTPS API; production builds use the deployed Azure API URL. For local HTTPS, trust the .NET development certificate with `dotnet dev-certs https --trust`.

### Containerized local development

For the Docker Compose setup, including the Nginx routes, independent database migrations, and the preserved SQL Server volume, see [DOCKER.md](./DOCKER.md). The default stack exposes the frontend at `http://localhost:8082`; microservice ports remain internal to Docker.

### Sign-in, accounts, and claim review

The app opens on the sign-in page, before displaying the navigation or claim pages. Users can sign in or create a claimant account. New accounts require a 12-character minimum password; passwords are stored as salted, one-way password hashes. Claimant accounts can use claim intake and status pages, but do not have administrator access. The **Claim Review** navigation link prompts for a separate administrator username and password at `/admin-login`. AuthService verifies the account's password hash and `IsAdmin` database flag before returning an Admin-role JWT. Successful admin sign-in creates a separate admin session and token without replacing the claimant session. Only an Admin role can access reviewer operations. ClaimsService requires a valid JWT for claim operations and the Admin role for reviewer operations. Use **Sign out** to end both sessions, or **Exit admin review** to return to the claimant app without ending the claimant session.

After admin sign-in, **Manage Accounts** opens the protected account-creation page, where an administrator can create claimant accounts or grant administrator access to trusted reviewers. This endpoint requires an Admin JWT; public registration always creates claimant accounts and never accepts an admin flag. The first administrator is provisioned on AuthService startup from `AdminBootstrap:Username` and `AdminBootstrap:Password`. The same `Jwt:SigningKey` (at least 32 bytes) must be configured for AuthService and ClaimsService. Never commit these values or put production secrets in application source.

For the original API's local development only, set secrets from the `InsuranceClaimApi` directory (replace the database connection string and admin password with your own values):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>"
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object 'Byte[]' 32
$rng.GetBytes($bytes)
$rng.Dispose()
$signingKey = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:SigningKey" $signingKey
dotnet user-secrets set "AdminBootstrap:Username" "claim-admin"
dotnet user-secrets set "AdminBootstrap:Password" "<a unique password of at least 12 characters>"
dotnet ef database update
dotnet run --launch-profile https
```

For a deployment of the original API, configure these values as App Service settings/Key Vault references. Run `dotnet ef database update` as a deployment step before starting it so the `Account` table exists. The administrator is created once if the configured username is not already in the table; changing its password later requires a deliberate credential rotation.

## Start the application on Windows

After installing the .NET SDK and Node.js, run `start.bat` from File Explorer or a terminal in the project folder. It opens separate terminals for the HTTPS API and Angular frontend. Open `http://localhost:4200` in your browser. Stop each server with `Ctrl+C` in its terminal. If the frontend dependencies have not been installed yet, run `npm install` from the project folder first.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
