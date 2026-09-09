# MedMatch Technical Documentation

This document describes the current implementation of MedMatch. For the product overview, user value, and quick start, see [README.md](README.md).

## Architecture

MedMatch is a Docker Compose application with four services:

```text
Browser
  |
  v
Nginx :80
  |---------------------> Frontend / Vite :5173
  |
  `---------------------> Backend / ASP.NET Core :8080
                                  |
                                  v
                            PostgreSQL :5432
```

### Components

| Component | Location | Responsibility |
| --- | --- | --- |
| Frontend | `frontend/` | React, TypeScript, Material UI, routing, forms, client state, API calls |
| API | `backend/src/MedMatch.Api/` | Minimal API endpoints, authentication middleware, CORS, migrations on startup |
| Domain | `backend/src/MedMatch.Domain/` | Users, profiles, care providers, doctors, reviews, consent, messages |
| Application | `backend/src/MedMatch.Application/` | Request/response contracts and application interfaces |
| Infrastructure | `backend/src/MedMatch.Infrastructure/` | EF Core DbContext, PostgreSQL mappings, migrations, token service |
| Reverse proxy | `infra/nginx/nginx.conf` | Routes `/` to the frontend and `/api` plus `/health` to the API |

## Local Development

### Prerequisites

- Docker Desktop with Docker Compose
- Node.js 20+ and npm for frontend work outside Docker
- .NET SDK 10 for backend work outside Docker

### Start the stack

From the repository root:

```bash
docker compose up -d --build
```

Useful commands:

```bash
docker compose ps
docker compose logs -f backend
docker compose logs -f frontend
docker compose down
```

URLs:

- Application through Nginx: `http://localhost`
- Frontend Vite server: `http://localhost:3000`
- API: `http://localhost:5000`
- API health: `http://localhost:5000/health`
- PostgreSQL: `localhost:5432`

The Compose file currently uses development values for database credentials and JWT configuration. Do not use these values outside local development. In a deployed environment, provide secrets through the platform's secret store or environment configuration.

## Frontend

The frontend is a Vite React application.

```bash
cd frontend
npm ci
npm run dev
npm run build
npm run preview
```

Important frontend paths:

- `src/App.tsx`: application shell, navigation, and routes
- `src/store.ts`: persisted authentication session state
- `src/services/api.ts`: typed fetch wrapper and API types
- `src/pages/Home.tsx`: public home page
- `src/pages/ClinicSearch.tsx`: clinic directory search
- `src/pages/PeopleSearch.tsx`: consent-filtered patient search and connection requests
- `src/pages/PatientProfile.tsx`: profile and privacy settings

## Backend Code Organization

The backend follows a lightweight Clean Architecture layout:

```text
MedMatch.Domain/
  Entities.cs                         # Domain entities and enums

MedMatch.Application/
  Contracts/
    AuthContracts.cs                  # Authentication request/response records
    ProfileContracts.cs               # Profile and consent records
    ClinicContracts.cs                # Clinic and doctor records
    ReviewContracts.cs                # Review records
    PeopleContracts.cs                # Patient directory records
    IAuthService.cs                    # Authentication service contract
    ITokenService.cs                   # Token service contract
    IPatientProfileService.cs          # Profile service contract
    IClinicSearchService.cs            # Clinic search contract
    IReviewService.cs                  # Review service contract
    IConsentService.cs                 # Consent service contract

MedMatch.Infrastructure/
  Services/
    AuthService.cs                    # IAuthService implementation
    TokenService.cs                   # ITokenService implementation
    PasswordHasher.cs                 # PBKDF2 password hashing
  Persistence/                        # EF Core context, mappings, migrations

MedMatch.Api/
  Program.cs                          # Composition root and route registration
  Mapping/DtoMapper.cs                 # Entity/DTO conversions
  Queries/ReviewQueries.cs            # Reusable review query construction
  Security/UserIdentity.cs             # Claims-to-user identity handling
  Configuration/ConfigurationExtensions.cs
```

Dependencies point inward: the Application project defines service contracts, Infrastructure implements them, and the API composes them through dependency injection. Authentication no longer creates users, hashes passwords, or issues tokens inside `Program.cs`.

The frontend uses `VITE_API_URL` for direct API calls. Through the normal Docker setup it points to `http://localhost:5000`.

## Backend

The backend targets .NET 10 and uses ASP.NET Core Minimal APIs with Entity Framework Core and PostgreSQL.

Build and test the solution from the backend directory:

```bash
cd backend
dotnet restore MedMatch.sln
dotnet build MedMatch.sln
dotnet test MedMatch.sln
```

The API applies pending EF Core migrations during startup. The API container waits for PostgreSQL to become healthy before starting. Public care-provider records require explicit publication consent and may include a provider-supplied website URL.

### Configuration

The backend requires these settings:

| Variable | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment, normally `Development` locally |
| `ASPNETCORE_URLS` | Listen address, configured as `http://+:8080` in Compose |
| `DATABASE_HOST` | PostgreSQL host, `postgres` inside Compose |
| `DATABASE_PORT` | PostgreSQL port |
| `DATABASE_NAME` | Database name |
| `DATABASE_USER` | Database user |
| `DATABASE_PASSWORD` | Database password |
| `JWT_SECRET` | HS256 signing key; use a long random secret in production |
| `JWT_ISSUER` | JWT issuer claim |
| `JWT_AUDIENCE` | JWT audience claim |
| `FRONTEND_URL` | Exact browser origin allowed by CORS, `http://localhost` through Nginx |

The JWT secret must be at least 16 bytes for HS256. Use a substantially longer randomly generated key in production.

## API Overview

All application endpoints are under `/api`.

### Authentication

| Method | Route | Access |
| --- | --- | --- |
| `POST` | `/api/auth/register` | Public |
| `POST` | `/api/auth/login` | Public |
| `POST` | `/api/auth/refresh` | Public |

Registration creates a patient profile and consent record for patient accounts. Successful registration and login return access and refresh tokens.

### Profiles and consent

| Method | Route | Access |
| --- | --- | --- |
| `GET` | `/api/profile` | Authenticated patient |
| `PUT` | `/api/profile` | Patient |
| `GET` | `/api/consent` | Authenticated user |
| `PUT` | `/api/consent` | Authenticated user |

Patient discovery requires both `PatientsContactMe` and `DataForSearch` to be true. Clinic discovery uses the equivalent `ClinicsContactMe` and `DataForSearch` flags.

### Care providers, doctors, and reviews

| Method | Route | Access |
| --- | --- | --- |
| `GET` | `/api/clinics` | Public consented care providers; supports `specialty`, `city`, `tag` |
| `GET` | `/api/clinics/{id}` | Public only when publication consent is granted |
| `POST` | `/api/clinics` | Clinic or admin |
| `PUT` | `/api/clinics/{id}` | Clinic or admin |
| `DELETE` | `/api/clinics/{id}` | Clinic or admin |
| `GET` | `/api/doctors` | Public; supports `specialty`, `city`, `tag` |
| `POST` | `/api/doctors` | Clinic, doctor, or admin |
| `PUT` | `/api/doctors/{id}` | Clinic, doctor, or admin |
| `DELETE` | `/api/doctors/{id}` | Clinic, doctor, or admin |
| `GET` | `/api/reviews` | Public; supports `clinicId`, `doctorId` |
| `POST` | `/api/reviews` | Patient |
| `PUT` | `/api/reviews/{id}` | Review author |
| `DELETE` | `/api/reviews/{id}` | Review author or admin |

### Patient discovery and connections

| Method | Route | Access |
| --- | --- | --- |
| `GET` | `/api/people` | Patient; supports `diagnosis`, `symptom`, `city` |
| `POST` | `/api/people/{id}/connection-requests` | Patient |
| `GET` | `/api/clinic/patients` | Clinic; supports `diagnosis`, `symptom` |

The people endpoint excludes the requesting user and returns only consented profiles. It does not return email addresses. A connection request is stored as a `Message` with a consent snapshot. An inbox and threaded messaging UI are planned follow-up work.

Although the current compatibility route is `/api/clinics`, its records represent care providers. `Clinic.Type` supports `Clinic`, `MedicalPractice`, `Doctor`, `Therapist`, `Hospital`, and `Other`. The existing table and route names are retained to avoid breaking current reviews and links; a future version can rename them to `care_providers` after a planned data migration.

## Data Model

The main PostgreSQL tables are:

- `users`: credentials, role, timestamps
- `patient_profiles`: display mode, location, health-related profile fields
- `consent_settings`: contact/search consent and audit metadata
- `clinics`: clinic directory records
- `doctors`: doctor records associated with clinics
- `reviews`: patient experiences and contact preferences
- `messages`: connection requests and future conversations
- `refresh_tokens`: hashed refresh tokens and revocation state

PostgreSQL array columns are used for diagnoses, interventions, symptoms, languages, treatments, and review tags. Entity configuration is in `backend/src/MedMatch.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`.

## Authentication And Authorization

- Passwords are hashed with PBKDF2-HMAC-SHA512 and a per-password random salt.
- Access tokens are signed with HS256 and include user ID, email, and role claims.
- Refresh tokens are generated from cryptographically secure random bytes and stored as SHA-256 hashes.
- Endpoint role requirements are enforced with ASP.NET Core authorization policies.
- CORS allows the configured frontend origin only.

For production, add secret rotation, rate limiting, account lockout or abuse detection, HTTPS, centralized logs, persisted data-protection keys, and a formal threat model.

## Database Migrations

Migrations live in:

```text
backend/src/MedMatch.Infrastructure/Persistence/Migrations/
```

Create a migration from the backend directory when the EF tooling is available:

```bash
dotnet ef migrations add DescribeTheChange \
  --project src/MedMatch.Infrastructure \
  --startup-project src/MedMatch.Api \
  --output-dir Persistence/Migrations
```

Review migrations before applying them. The application applies migrations automatically at startup for local development; production deployments should use a controlled migration step.

## Testing And Verification

Recommended local checks:

```bash
# Frontend typecheck and production bundle
cd frontend
npm run build

# Backend compilation
cd ../backend
dotnet build MedMatch.sln

# Compose validation
cd ..
docker compose config --quiet
```

There is currently no dedicated automated test project in the repository. New authentication, consent, search, and connection behavior should be covered with API integration tests before production use.

## Deployment Notes

The current Compose setup is intended for development. A production deployment should at minimum:

1. Build a static frontend bundle and serve it from a hardened Nginx image.
2. Run the API behind HTTPS with a managed PostgreSQL instance.
3. Replace all development credentials and JWT values with managed secrets.
4. Restrict database and API network exposure.
5. Add backups, health monitoring, structured logs, alerting, and migration controls.
6. Complete GDPR documentation, retention rules, export/deletion workflows, moderation, and legal review for health data.

## Known Gaps

- No inbox or threaded messaging UI yet; connection requests are persisted for the next messaging increment.
- No automated test suite yet.
- Clinic and doctor verification is represented by a field but does not yet include a verification workflow.
- Production hardening and GDPR operational processes remain to be implemented.
