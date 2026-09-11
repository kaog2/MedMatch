# MedMatch

> Find care with a little more confidence.

MedMatch is a privacy-focused platform for discovering care providers and therapies through people's experiences and health histories. Patients can search the care directory, read experiences, manage how their profile appears, and optionally connect with other patients who have chosen to be discoverable.

> **Project status:** MedMatch is an actively developed MVP. It is not a medical advice service and does not replace professional care.

[Technical documentation](TECHNICAL_README.md) · [Report an issue](../../issues)

---

## Why MedMatch

Finding care is often a research problem before it becomes a healthcare appointment. MedMatch makes that research more useful by combining structured care-provider information with lived experience, while keeping sharing and contact decisions in the hands of each user.

- **Patients** can:
  - Search for clinics, medical practices, doctors, therapists, hospitals, and therapies by specialty, symptom, city, and treatment.
  - Read experiences and histories shared by other patients.
  - Publish their own experience, choosing to appear anonymously, with a pseudonym, or with their real name.
  - Decide whether they want to be contacted by other patients and/or clinics.

- **Care providers** can:
  - Represent a clinic, medical practice, doctor, therapist, hospital, or other provider.
  - Supply a public website or contact information and explicitly permit publication.
  - Search for patients who have explicitly allowed provider contact, with certain diagnoses or symptoms.

The main focus is **patient → care provider**, with provider-to-patient discovery optional and consent-controlled.

---

## How Peer Matching Works

MedMatch connects patients who share similar conditions. It ranks people by how much their health profiles overlap, so the most relevant peers surface first.

### 1. Normalized diagnosis tags

Diagnoses are stored as normalized, deduplicated tags (for example `Morbus Perthes`, `LWS`, `Hip TEP Surgery`, `Lower Back Pain`). A shared vocabulary powers autocomplete in the profile form, and free-text entries are normalized into the same vocabulary — so `PERTHES` and `Morbus Perthes` map to the same tag.

### 2. Similarity scoring

For two patients A and B, the system computes a score in the range 0–100:

- **Shared diagnoses** are the dominant signal, measured with the Sørensen–Dice coefficient:

  $$\text{Dice}(A,B) = \frac{2 \times |A \cap B|}{|A| + |B|}$$

- **Shared symptoms** add a smaller bonus.
- **Location** adds a bonus when both share the same city (larger) or the same country (smaller).

The final score is:

$$\text{score} = 50 + 35 \times \text{Dice} + \text{symptom bonus} + \text{location bonus}$$

clamped to 10–100. When two people share no diagnosis at all, the score is 0 and they are not shown to each other.

### 3. Consent before anything else

Only patients who have explicitly enabled both **"patients can contact me"** and **"use my data for search"** appear as candidates. Private email addresses are never exposed — contact happens through an in-app connection request.

### 4. Ranking and filtering

Matches are ordered by score descending, then by the number of shared diagnoses. The matches view also supports **country** and **city** filters so results can be narrowed by region.

---

## Main Features (MVP)

### For Patients

- Register and sign in with JWT-based authentication.
- Sign up or sign in with Google when Google OAuth is configured.
- Verify email addresses before a password account can sign in.
- Patient profile with:
  - Diagnoses (e.g., Morbus Perthes, osteoarthritis) with autocomplete suggestions.
  - Interventions (e.g., hip prosthesis).
  - Symptoms (e.g., lower back pain).
  - Location (city/country).
  - Privacy and contact preferences.
- Publish experiences and histories about care providers and therapies:
  - Rating (1–5 stars).
  - Experience text.
  - Tags.
  - Anonymity option (anonymous / pseudonym / real name).
  - Option to allow contact by other patients and/or clinics.
- Search for care providers by specialty, city, and therapy type.
- Search for other patients by diagnosis, symptom, or city when both search and contact consent are enabled.
- See ranked peer matches based on shared diagnoses, symptoms, and location.
- Send a connection request without exposing private email addresses.

### For Care Providers

- Provider profile:
  - Name, specialty, treatments offered.
  - Address, city, country.
  - Provider-supplied public website or contact information.
  - Explicit permission before the provider record is published publicly.
- Search for patients who explicitly allow provider contact and search.

### Administration

- Admin portal to review and search users.
- Inspect the ranked diagnosis matches for any user.
- Activate or deactivate accounts.
- A seed administrator account is created on startup and configured through environment variables.

### Development Data

- Optional seeders insert fictional patients for local testing:
  - 100 general sample patients.
  - 50 patients focused on the Morbus Perthes / LWS / hip replacement case study.
- Both seeders are idempotent and only run when explicitly enabled.

---

## Technology Stack

### Frontend

- **React** + **TypeScript**
- UI: **Material UI**
- State and data:
  - **React Query** for caching and API synchronization.
  - **Zustand** for authentication state.
- Authentication: JWT access tokens and refresh tokens.

### Backend

- **ASP.NET Core** Web API on .NET 10
- Database: **PostgreSQL**
- ORM: **Entity Framework Core**
- Authentication and authorization:
  - JWT + refresh tokens
  - Roles: `patient`, `clinic`, `doctor`, `admin`

### Infrastructure and DevOps

- **Docker** + **Docker Compose** for local development.
- **Nginx** as reverse proxy.

---

## Privacy And Safety

The project handles health-related information, so privacy is a core product requirement. The current implementation includes explicit contact/search consent, anonymous review display, pseudonyms, and role-based access control. A production release requires legal review, a DPIA, secure secret management, TLS, audit logging, retention policies, and a complete account export/deletion workflow.

### Key Principles

- **Explicit and granular consent**:
  - Show public profile (yes/no).
  - Be contactable by other patients (yes/no).
  - Be contactable by clinics (yes/no, optional feature).
- **Real anonymity**:
  - Users can choose to appear as anonymous in their reviews.
  - The public UI does not show name or direct data if they choose anonymity.
- **User rights implemented as features**:
  - Access to their data.
  - Correction and update.
  - Export (JSON/PDF).
  - Account and data deletion.
  - Withdrawal of consent at any time.
- **Security**:
  - Encryption in transit (TLS 1.2+) and at rest.
  - Role-based access control.
  - Access auditing and activity logs.
  - Data retention and deletion policies.

> **Important:** MedMatch does not provide medical advice. Always consult a qualified healthcare professional for health decisions.

---

## Data Model (Summary)

### User

- `id`, `email`, `password_hash`, `role`
- `email_confirmed`, `is_active`
- `created_at`, `last_login`

### PatientProfile

- `user_id` (FK)
- `display_mode`: `anonymous` | `pseudonym` | `real_name`
- `pseudonym`, `real_name`
- `city`, `country`
- `diagnoses`, `interventions`, `symptoms` (arrays)
- `bio`, `languages`

### DiagnosisTag / PatientDiagnosisTag

- `diagnosis_tags`: normalized diagnosis vocabulary (`name`, `slug`, `usage_count`).
- `patient_diagnosis_tags`: many-to-many join between a patient profile and its diagnosis tags.

### MatchNotification

- Records a ranked peer match (`user_id`, `matched_user_id`, `shared_diagnoses`, `score`, `is_read`).

### ConsentSettings

- `user_id` (FK)
- `show_profile_publicly`
- `patients_contact_me`
- `clinics_contact_me`
- `data_for_search`
- `version`, `updated_at`, `ip`, `user_agent` (audit)

### Clinic / Doctor

- `id`, `name`, `type` (clinic, practice, doctor, therapist, hospital, other)
- `specialty`, `treatments_offered`
- `address`, `city`, `country`
- `contact_info`, `public_website_url`
- `publication_consent_granted`, `publication_consent_at`
- `is_verified`

### Review

- `id`, `author_user_id` (FK)
- `clinic_id` / `doctor_id`
- `rating` (1–5), `title`, `body`, `tags`
- `is_anonymous`
- `created_at`, `updated_at`

### Message

- `id`, `from_user_id`, `to_user_id`, `thread_id`
- `content`, `created_at`
- `consent_snapshot` (the consent in effect when the thread started)

---

## Repository Structure

```bash
medmatch/
  frontend/                 # React + TypeScript (Vite, Material UI)
    src/
      components/
      pages/
      services/
  backend/                  # ASP.NET Core solution
    src/
      MedMatch.Api/         # API endpoints, mapping, services
      MedMatch.Domain/      # Entities and enums
      MedMatch.Application/ # Contracts and interfaces
      MedMatch.Infrastructure/ # EF Core, migrations, auth/email services
  infra/
    nginx/                  # Reverse proxy configuration
  scripts/                  # Build and publish helpers
  docker-compose.yml        # Local development stack
  docker-compose.harbor.yml # Deployment stack
  README.md                 # Product overview
  TECHNICAL_README.md       # Architecture and API reference
  PORTAINER_DEPLOYMENT.md   # Deployment runbook
```

---

## How to Run Locally (Development)

### Requirements

- Docker and Docker Compose
- .NET SDK (to develop the backend outside Docker)
- Node.js 20+ (to develop the frontend outside Docker)

### Steps

1. Clone the repository:

```bash
git clone https://github.com/<your-account>/medmatch.git
cd medmatch
```

2. Create a local, git-ignored `env.dev` file with your development values. The variable names are documented in `.env.example`.

3. Start the development stack:

```bash
docker compose --env-file env.dev up -d --build
```

This starts:

- PostgreSQL
- ASP.NET Core backend
- React frontend (Vite)
- Nginx reverse proxy

4. Access the application:

- Application: `http://localhost`
- Frontend development server: `http://localhost:3000`
- API: `http://localhost:5000`
- API health check: `http://localhost:5000/health`

### Optional integrations

- **Google sign-in** — create a Google OAuth web client, add `http://localhost` as an authorized JavaScript origin, and set `GOOGLE_CLIENT_ID` in `env.dev`.
- **Email verification** — provide SMTP settings (`SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM_EMAIL`) in `env.dev`. The application sends a one-time verification link for password registrations.

Never commit real credentials or secrets. Use placeholders and private environment files only.

### Seed data for local testing

Fictional data can be inserted on startup through environment flags:

```env
SEED_SAMPLE_DATA=true   # 100 general sample patients
SEED_CASE_DATA=true     # 50 Morbus Perthes / LWS / hip replacement patients
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=change-me
```

The seeders are idempotent (they skip when the sample users already exist) and create a configured administrator account for the admin portal.

For architecture, API routes, database details, testing, and deployment notes, see [TECHNICAL_README.md](TECHNICAL_README.md).

---

## Roadmap

Already implemented:

- JWT authentication, refresh tokens, Google sign-in, email verification.
- Patient profiles with anonymity, consent, and diagnosis tags.
- Care provider directory with explicit publication consent.
- Reviews and patient experiences.
- Peer matching (ranked by shared diagnoses, symptoms, and location) with connection requests.
- Admin portal for user management and match inspection.

Upcoming:

- Patient ↔ patient messaging and an inbox.
- Provider verification workflow.
- Reports, moderation, and blocking.
- GDPR tooling: data export, deletion, and documented DPIA.
- Production hardening: secrets management, rate limiting, observability, and automated tests.

---

## Contributions

If you want to contribute:

1. Open an issue describing the improvement or bug.
2. Wait for discussion and assignment.
3. Create a branch, implement, and send a PR.

Please follow code conventions and write tests for new critical features.

---

## License

[Specify the license you choose here, e.g.:]

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## Legal Disclaimer

MedMatch does not provide medical advice, diagnoses, or treatments. The information shared by users reflects their personal experiences and does not replace the care of a health professional. Always consult a doctor or specialist for decisions related to your health.