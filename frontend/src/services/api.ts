import { useAuthStore } from '../store';

const baseUrl = (import.meta as unknown as { env?: Record<string, string> }).env?.VITE_API_URL ?? 'http://localhost:5000';

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = useAuthStore.getState().accessToken;
  const response = await fetch(`${baseUrl}/api${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers },
  });
  if (!response.ok) throw new Error((await response.json().catch(() => ({ error: response.statusText }))).error ?? 'Request failed');
  return response.status === 204 ? (undefined as T) : response.json();
}

export type CareProviderType = 'Clinic' | 'MedicalPractice' | 'Doctor' | 'Therapist' | 'Hospital' | 'Other';
export type Clinic = { id: string; name: string; type: CareProviderType; specialty: string; treatmentsOffered: string[]; address?: string; city: string; country: string; contactInfo?: string; publicWebsiteUrl?: string; publicationConsentGranted: boolean; isVerified: boolean };
export type Review = { id: string; clinicId?: string; doctorId?: string; rating: number; title: string; body: string; tags: string[]; isAnonymous: boolean; authorDisplayName: string; authorUserId?: string; createdAt: string };
export type Profile = { displayMode: 'Anonymous' | 'Pseudonym' | 'RealName'; pseudonym?: string; realName?: string; city?: string; country?: string; diagnoses: string[]; interventions: string[]; symptoms: string[]; ageRange?: string; bio?: string; languages: string[] };
export type Consent = { showProfilePublicly: boolean; clinicsContactMe: boolean; patientsContactMe: boolean; dataForSearch: boolean; version: number; updatedAt: string };
export type Person = { userId: string; displayName: string; city?: string; country?: string; diagnoses: string[]; interventions: string[]; symptoms: string[]; bio?: string; languages: string[] };
export type DiagnosisTag = { id: string; name: string; usageCount: number };
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
