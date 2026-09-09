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

## Main Features (MVP)

### For Patients

- Register and sign in with JWT-based authentication.
- Patient profile with:
  - Diagnoses (e.g., Morbus Perthes, osteoarthritis).
  - Interventions (e.g., hip prosthesis).
  - Symptoms (e.g., lower back pain).
  - Location (city/country).
  - Privacy and contact preferences.
- Publish experiences and histories about care providers and therapies:
  - Rating (1–5 stars).
  - Experience text.
  - Tags (Schmerztherapie, post-prosthesis, lower back pain, etc.).
  - Anonymity option (anonymous / pseudonym / real name).
  - Option to allow contact by other patients and/or clinics.
  - Search for care providers by specialty, city, and therapy type.
- Search for other patients by diagnosis, symptom, or city when both search and contact consent are enabled.
- Send a connection request without exposing private email addresses.

### For Care Providers

- Provider profile:
  - Name, specialty, treatments offered.
  - Address, city, country.
  - Provider-supplied public website or contact information.
  - Explicit permission before the provider record is published publicly.
- Search for patients who explicitly allow provider contact and search.

### Administration

- Moderation of reviews and profiles.
- Management of reports and blocks.
- Metrics dashboard (without sensitive data in plain text).
- Tools to handle GDPR rights (export/delete user data).

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
- `created_at`, `last_login`

### PatientProfile

- `user_id` (FK)
- `display_mode`: `anonymous` | `pseudonym` | `real_name`
- `contactable_by`: flags (`other_patients`, `clinics`)
- `location`: city, country
- `diagnoses`: array or related table
- `interventions`: prosthesis, dates, etc.
- `symptoms`: lower back pain, etc.
- `age_range` or `birth_year`
- `bio`, `languages`

### Clinic / Doctor

- `id`, `name`, `type`
- `specialty`, `treatments_offered`
- `address`, `city`, `country`, `coordinates`
- `contact_info`
- `verification_status`

### Recommendation / Review

- `id`, `author_user_id` (FK)
- `clinic_id` / `doctor_id`
- `rating` (1–5)
- `title`, `body`
- `tags`: array of strings
- `is_anonymous`: bool
- `created_at`, `updated_at`

### Consent & PrivacySettings

- `user_id`
- `consent_show_profile_publicly`
- `consent_clinics_contact_me`
- `consent_patients_contact_me`
- `consent_data_for_search`
- `version`, `updated_at`, `ip`, `user_agent` (audit)

### Message (Phase 2)

- `id`, `from_user_id`, `to_user_id`
- `thread_id`
- `content`, `created_at`
- `consent_snapshot` (what consent existed when the thread was started)

---

## Repository Structure

```bash
medmatch/
  frontend/
    # React + TypeScript
    src/
      components/
      pages/
      hooks/
      services/
      styles/
    Dockerfile
    package.json

  backend/
    # .NET Core Web API
    src/
      MedMatch.Api/
      MedMatch.Domain/
      MedMatch.Infrastructure/
      MedMatch.Application/
    Dockerfile
    MedMatch.sln

  infra/
    docker-compose.yml
    nginx/
    scripts/

  infra/nginx/       # Reverse proxy configuration
  docker-compose.yml # Local development stack
  README.md          # Product overview
  TECHNICAL_README.md
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

2. Start the development stack:

```bash
docker compose up -d --build
```

This should start:

- PostgreSQL
- .NET Core Backend
- React Frontend
- Nginx (reverse proxy)

4. Access the application:

- Application: `http://localhost`
- Frontend development server: `http://localhost:3000`
- API: `http://localhost:5000`
- API health check: `http://localhost:5000/health`

For architecture, API routes, database details, testing, and deployment notes, see [TECHNICAL_README.md](TECHNICAL_README.md).

---

## Roadmap

### Phase 1 – MVP (4–6 weeks)

- User authentication and registration.
- Patient profiles with anonymity and contact options.
- CRUD for clinics and reviews.
- Clinic search and filtering by tags/diagnoses/symptoms.
- Privacy policy and consent texts reviewed for the target deployment.

### Phase 2 – Messaging and Clinics (4–6 weeks)

- Patient ↔ patient messaging (when the author allows it).
- Verification of clinics and doctors.
- Optional feature: clinics search for contactable patients.
- Administration and moderation panel.

### Phase 3 – GDPR and Production

- User data export and deletion.
- Security audits, logs, and documented DPIA.
- Security improvements, monitoring, and scaling on Azure.

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