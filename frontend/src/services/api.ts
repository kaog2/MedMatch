import { useAuthStore } from '../store';
import i18n from '../i18n';
import { logger } from './telemetry';

const baseUrl = (import.meta as unknown as { env?: Record<string, string> }).env?.VITE_API_URL ?? 'http://localhost:5000';

const resolvedLanguage = () => (i18n.resolvedLanguage ?? i18n.language ?? 'en').split('-')[0];

type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  roles: ('Patient' | 'Clinic' | 'Doctor' | 'Admin')[];
};

let refreshInFlight: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = useAuthStore.getState().refreshToken;
  if (!refreshToken) return null;

  try {
    const response = await fetch(`${baseUrl}/api/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      useAuthStore.getState().signOut();
      return null;
    }

    const session = await response.json() as AuthResponse;
    useAuthStore.getState().setSession(session.accessToken, session.refreshToken, session.roles);
    return session.accessToken;
  } catch (error) {
    logger.warn('API access-token refresh request failed', {
      errorMessage: error instanceof Error ? error.message : 'Unknown refresh error',
    });
    return null;
  }
}

function getRefreshedAccessToken(): Promise<string | null> {
  if (!refreshInFlight) {
    refreshInFlight = refreshAccessToken().finally(() => {
      refreshInFlight = null;
    });
  }
  return refreshInFlight;
}

async function sendApiRequest(path: string, options: RequestInit, token: string | null): Promise<Response> {
  const headers = new Headers(options.headers);
  headers.set('Content-Type', 'application/json');
  headers.set('Accept-Language', resolvedLanguage());
  if (token) headers.set('Authorization', `Bearer ${token}`);

  return fetch(`${baseUrl}/api${path}`, { ...options, headers });
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const sentToken = useAuthStore.getState().accessToken;
  let response = await sendApiRequest(path, options, sentToken);

  if (response.status === 401 && !path.startsWith('/auth/')) {
    const currentToken = useAuthStore.getState().accessToken;
    const retryToken = currentToken && currentToken !== sentToken
      ? currentToken
      : await getRefreshedAccessToken();

    if (retryToken) response = await sendApiRequest(path, options, retryToken);
  }

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({ error: response.statusText }));
    const errorMsg = errorBody.error ?? 'Request failed';
    logger.warn(`API Error [${options.method || 'GET'}] ${path} returned ${response.status}`, {
      path,
      method: options.method || 'GET',
      status: response.status,
      statusText: response.statusText,
      errorMessage: errorMsg,
    });
    throw new Error(errorMsg);
  }
  return response.status === 204 ? (undefined as T) : response.json();
}

export type CareProviderType = 'Clinic' | 'MedicalPractice' | 'Doctor' | 'Therapist' | 'Hospital' | 'Other';
export type Clinic = { id: string; name: string; type: CareProviderType; specialty: string; treatmentsOffered: string[]; address?: string; city: string; country: string; contactInfo?: string; publicWebsiteUrl?: string; publicationConsentGranted: boolean; isVerified: boolean };
export type Review = { id: string; clinicId?: string; doctorId?: string; rating: number; title: string; body: string; tags: string[]; isAnonymous: boolean; authorDisplayName: string; authorUserId?: string; createdAt: string };
export type Profile = { displayMode: 'Anonymous' | 'Pseudonym' | 'RealName'; pseudonym?: string; realName?: string; city?: string; country?: string; diagnoses: string[]; interventions: string[]; symptoms: string; ageRange?: string; bio?: string; languages: string[] };
export type Consent = { showProfilePublicly: boolean; clinicsContactMe: boolean; patientsContactMe: boolean; dataForSearch: boolean; version: number; updatedAt: string };
export type Person = { userId: string; displayName: string; city?: string; country?: string; diagnoses: string[]; interventions: string[]; symptoms: string; bio?: string; languages: string[] };
export type DiagnosisTag = { id: string; name: string; localizedName?: string | null; usageCount: number };
export type Match = {
  userId: string;
  displayName: string;
  city?: string;
  country?: string;
  sharedDiagnoses: string[];
  sharedSymptoms: string[];
  sameLocation: boolean;
  matchPercentage: number;
  bio?: string;
  languages: string[];
};
export type MatchNotification = {
  id: string;
  matchedUserId: string;
  displayName: string;
  sharedDiagnoses: string[];
  score: number;
  isRead: boolean;
  createdAt: string;
};
export type MatchSummary = { unreadCount: number };
export type AdminUser = {
  id: string;
  email: string;
  roles: string[];
  displayName?: string;
  city?: string;
  country?: string;
  diagnoses: string[];
  symptoms: string;
  emailConfirmed: boolean;
  isActive: boolean;
  patientsContactMe: boolean;
  dataForSearch: boolean;
  createdAt: string;
};
export type AdminUsersResponse = { total: number; page: number; pageSize: number; items: AdminUser[] };
export type RecommendationStatus = 'Pending' | 'Approved' | 'Rejected';
export type RecommendationClinic = { id: string; name: string; type: CareProviderType; city: string; country: string };
export type Recommendation = {
  id: string;
  authorUserId: string;
  authorDisplayName: string;
  clinics: RecommendationClinic[];
  diagnoses: string[];
  details: string;
  status: RecommendationStatus;
  moderationNote?: string;
  createdAt: string;
};
export type CreateRecommendation = { clinicIds: string[]; diagnoses: string[]; details: string };

export async function translateText(text: string, targetLanguage: string): Promise<string> {
  const result = await api<{ translatedText: string }>('/translate', {
    method: 'POST',
    body: JSON.stringify({ text, targetLanguage }),
  });
  return result.translatedText;
}
