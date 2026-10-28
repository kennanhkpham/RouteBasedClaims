# RouteBasedClaim

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.1.

## Claims portal architecture

The app follows a three-tier flow; the browser never connects directly to SQL Server:

1. **UI:** Angular's `/intake` view submits a claim, and `/status` looks one up by claim number.
2. **Web API:** `InsuranceClaimApi` exposes `POST /api/claims` and `GET /api/claims/{claimNumber}`. Its `ClaimsController` validates and maps the request/response.
3. **SQL:** The API uses Entity Framework Core and `AppDbContext` to persist claims and prescription details in SQL Server.

Configure the API's `DefaultConnection` connection string and ensure its database schema exists before using the claims portal. Start the API with its `https` launch profile (`https://localhost:7197`), then start Angular with `ng serve` (`http://localhost:4200`). Local Angular development uses the local HTTPS API; production builds use the deployed Azure API URL. For local HTTPS, trust the .NET development certificate with `dotnet dev-certs https --trust`.

### Sign-in, accounts, and claim review

The app opens on the sign-in page, before displaying the navigation or claim pages. Users can sign in or create a claimant account. New accounts require a 12-character minimum password; passwords are stored as salted, one-way password hashes. Claimant accounts can use claim intake and status pages, but do not have administrator access. The **Claim Review** navigation link prompts for a separate administrator username and password at `/admin-login`. The backend verifies the account's password hash and `IsAdmin` database flag before returning an Admin-role JWT. Successful admin sign-in creates a separate admin session and token without replacing the claimant session. Only an Admin role can access reviewer operations. The API requires a valid JWT for all claim operations and the Admin role for reviewer operations. Use **Sign out** to end both sessions, or **Exit admin review** to return to the claimant app without ending the claimant session.

After admin sign-in, **Manage Accounts** opens the protected account-creation page, where an administrator can create claimant accounts or grant administrator access to trusted reviewers. This endpoint requires an Admin JWT; public registration always creates claimant accounts and never accepts an admin flag. The first administrator is provisioned on API startup from `AdminBootstrap:Username` and `AdminBootstrap:Password`. The JWT signing key is read from `Jwt:SigningKey` and must be at least 32 bytes. Never commit these values or put production secrets in application source.

For local development, set secrets from the `InsuranceClaimApi` directory (replace the database connection string and admin password with your own values):

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

In Azure, configure these values as App Service settings/Key Vault references. Run `dotnet ef database update` as a deployment step before starting the API so the `Account` table exists. The administrator is created once if the configured username is not already in the table; changing its password later requires a deliberate credential rotation.

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
