# RouteBasedClaim

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.1.

## Claims portal architecture

The app follows a three-tier flow; the browser never connects directly to SQL Server:

1. **UI:** Angular's `/intake` view submits a claim, and `/status` looks one up by claim number.
2. **Web API:** `InsuranceClaimApi` exposes `POST /api/claims` and `GET /api/claims/{claimNumber}`. Its `ClaimsController` validates and maps the request/response.
3. **SQL:** The API uses Entity Framework Core and `AppDbContext` to persist claims and prescription details in SQL Server.

Configure the API's `DefaultConnection` connection string and ensure its database schema exists before submitting claims. Start the API with its `https` launch profile (`https://localhost:7197`), then start Angular with `ng serve` (`http://localhost:4200`). The Angular API service targets that HTTPS API URL, and the API's CORS policy allows the local Angular origin.

### Admin claim review sign-in

Selecting **Claim Review** opens the admin sign-in page. For this demo, use username `admin` and password `ClaimReview123!`. The API issues a one-hour JWT and requires the Admin role for claim-list, pending-claim, claim-update, and claim-delete endpoints. In Claim Review, an admin can mark a claim **Inactive** or **Rejected** and use its Delete action to permanently remove it after confirming. The browser stores the token in session storage and sends it as a bearer token; leaving Claim Review clears the session. These credentials and the signing key are hardcoded for local testing only; replace them with secure configuration and proper credential storage before deployment.

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
