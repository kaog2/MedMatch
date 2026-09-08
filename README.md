# MedMatch – Social Network for Clinical Recommendations

> A platform where patients share experiences and recommendations about clinics, doctors, and therapies, and can choose whether to remain anonymous or be contactable by other patients and/or clinics.

**Note:** This is a work-in-progress project. It does not provide medical advice and does not replace professional care.

---

## Product Vision

MedMatch helps patients find suitable clinics and doctors for their specific condition (e.g., chronic lower back pain, post-prosthesis, Morbus Perthes, Schmerztherapie), based on real experiences from other users.

- **Patients** can:
  - Search for clinics and doctors by specialty, symptom, city, and therapy type.
  - Read experiences and recommendations from other patients.
  - Publish their own experience, choosing to appear anonymously, with a pseudonym, or with their real name.
  - Decide whether they want to be contacted by other patients and/or clinics.

- **Clinics and doctors** can:
  - Have a verified profile.
  - (Optional) Search for patients who have explicitly marked that they are open to being contacted by clinics, with certain diagnoses or symptoms.

The main focus is **patient → clinic**, with the "clinics searching for patients" feature being optional and highly controlled.

---

## Main Features (MVP)

### For Patients

- Secure registration and login (optional MFA).
- Patient profile with:
  - Diagnoses (e.g., Morbus Perthes, osteoarthritis).
  - Interventions (e.g., hip prosthesis).
  - Symptoms (e.g., lower back pain).
  - Location (city/country).
  - Privacy and contact preferences.
- Publish experiences/recommendations about clinics and doctors:
  - Rating (1–5 stars).
  - Experience text.
  - Tags (Schmerztherapie, post-prosthesis, lower back pain, etc.).
  - Anonymity option (anonymous / pseudonym / real name).
  - Option to allow contact by other patients and/or clinics.
- Search for clinics and doctors:
  - By specialty, city, therapy type.
  - Filter by experiences from patients with similar diagnoses or symptoms.
- Contact other patients (if they allow it) to ask more about their experience.

### For Clinics and Doctors

- Verified clinic/doctor profile:
  - Name, specialty, treatments offered.
  - Address, city, country.
  - Public contact information.
- (Phase 2, optional) Search for patients who:
  - Have explicitly marked "clinics can contact me".
  - Have certain diagnoses, interventions, or symptoms.
- Send secure messages to contactable patients from the platform.

### Administration

- Moderation of reviews and profiles.
- Management of reports and blocks.
- Metrics dashboard (without sensitive data in plain text).
- Tools to handle GDPR rights (export/delete user data).

---

## Technology Stack

### Frontend

- **React** + **TypeScript**
- UI: **Material UI** or **Chakra UI**
- State and data:
  - **React Query** for caching and API synchronization.
  - **Zustand** or **Redux Toolkit** for global state.
- Authentication: **NextAuth** or **Auth0** (with optional MFA).

### Backend

- **.NET Core** Web API
- Database: **PostgreSQL** (recommended) or **MongoDB** (if NoSQL is preferred)
- ORM: **Entity Framework Core**
- Authentication and authorization:
  - JWT + refresh tokens
  - Roles: `patient`, `clinic`, `doctor`, `admin`

### Infrastructure and DevOps

- **Docker** + **Docker Compose** for local development.
- **Nginx** as reverse proxy.
- Deployment on **Azure** (App Service or AKS).
- TLS, WAF, backups, and monitoring (Grafana, Prometheus, Loki, etc.).

---

## Privacy and Compliance (GDPR)

The project handles **health data** (special category under GDPR Art. 9). The design follows **privacy by design** and **privacy by default** principles. [web:5][web:7][web:10][web:12][web:14]

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

> **Important:** Before launching, it is recommended to review the implementation with a lawyer specialized in data protection and digital health in Germany/Europe. [web:5][web:10][web:12][web:14]

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

## Repository Structure (Suggested)

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

  docs/
    architecture.md
    api-spec.md
    privacy-policy.md
    terms.md

  README.md
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
git clone https://github.com/your-username/medmatch.git
cd medmatch
```

2. Configure environment variables:

Create a `.env` file in the root (or in `infra/`) with:

```env
# Backend
ASPNETCORE_ENVIRONMENT=Development
DATABASE_HOST=postgres
DATABASE_NAME=medmatch
DATABASE_USER=medmatch
DATABASE_PASSWORD=ChangeMe!
JWT_SECRET=ChangeMeToo-Development-Jwt-Key-32Chars!

# Frontend
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=ChangeMeAsWell!
API_URL=http://localhost:5000
```

3. Start the infrastructure with Docker Compose:

```bash
docker compose -f infra/docker-compose.yml up -d
```

This should start:

- PostgreSQL
- .NET Core Backend
- React Frontend
- Nginx (reverse proxy)

4. Access the application:

- Frontend: `http://localhost:3000`
- API: `http://localhost:5000`
- (Optional) Adminer/pgAdmin to view the DB: `http://localhost:8080`

---

## Roadmap

### Phase 1 – MVP (4–6 weeks)

- User authentication and registration.
- Patient profiles with anonymity and contact options.
- CRUD for clinics and reviews.
- Clinic search and filtering by tags/diagnoses/symptoms.
- Privacy policy and consent texts (to be reviewed by a lawyer). [web:12][web:14]

### Phase 2 – Messaging and Clinics (4–6 weeks)

- Patient ↔ patient messaging (when the author allows it).
- Verification of clinics and doctors.
- Optional feature: clinics search for contactable patients.
- Administration and moderation panel. [web:6][web:14]

### Phase 3 – GDPR and Production

- User data export and deletion.
- Security audits, logs, and documented DPIA. [web:12][web:14][web:17]
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