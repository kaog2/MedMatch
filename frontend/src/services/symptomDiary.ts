import { api } from './api';

export type SymptomCategory =
  | 'Pain'
  | 'Fatigue'
  | 'Neurological'
  | 'Digestive'
  | 'Respiratory'
  | 'Musculoskeletal'
  | 'MoodPsychological'
  | 'Sleep'
  | 'Skin'
  | 'Other';

export type SymptomDiaryEntry = {
  id: string;
  sheetId: string;
  date: string;
  recordedAt: string;
  category: SymptomCategory | string;
  symptomName: string;
  painType?: string | null;
  bodyLocation?: string | null;
  severity: number;
  durationMinutes?: number | null;
  triggers?: string | null;
  relievers?: string | null;
  medicationsTaken?: string | null;
  notes?: string | null;
  source: string;
  createdAt: string;
  updatedAt: string;
};

export type SymptomDiarySheet = {
  id: string;
  date: string;
  overallWellbeing?: number | null;
  sleepQuality?: number | null;
  sleepHours?: number | null;
  dailyNotes?: string | null;
  entryCount: number;
  averageSeverity?: number | null;
  maxSeverity?: number | null;
  entries: SymptomDiaryEntry[];
  createdAt: string;
  updatedAt: string;
};

export type SymptomDiarySheetSummary = {
  id: string;
  date: string;
  overallWellbeing?: number | null;
  sleepQuality?: number | null;
  sleepHours?: number | null;
  entryCount: number;
  averageSeverity?: number | null;
  maxSeverity?: number | null;
  mainSymptoms: string[];
};

export type UpsertSymptomEntryRequest = {
  date?: string;
  recordedAt?: string;
  category?: string;
  symptomName: string;
  painType?: string | null;
  bodyLocation?: string | null;
  severity: number;
  durationMinutes?: number | null;
  triggers?: string | null;
  relievers?: string | null;
  medicationsTaken?: string | null;
  notes?: string | null;
  source?: string;
};

export type UpdateDailySheetRequest = {
  overallWellbeing?: number | null;
  sleepQuality?: number | null;
  sleepHours?: number | null;
  dailyNotes?: string | null;
};

export type BotApiKeyDto = {
  id: string;
  keyPrefix: string;
  label: string;
  isActive: boolean;
  createdAt: string;
  lastUsedAt?: string | null;
};

export type CreateBotKeyResponse = {
  id: string;
  apiKey: string;
  keyPrefix: string;
  label: string;
  createdAt: string;
};

export type DailySeverityPoint = {
  date: string;
  averageSeverity: number;
  maxSeverity: number;
  entryCount: number;
};

export type SymptomFrequency = {
  name: string;
  category: string;
  count: number;
  averageSeverity: number;
};

export type LocationFrequency = {
  location: string;
  count: number;
};

export type PainTypeFrequency = {
  painType: string;
  count: number;
};

export type SymptomDiaryAnalytics = {
  totalEntries: number;
  daysTracked: number;
  overallAverageSeverity: number;
  severityTrend: DailySeverityPoint[];
  topSymptoms: SymptomFrequency[];
  topLocations: LocationFrequency[];
  topPainTypes: PainTypeFrequency[];
};

export async function getDiarySheets(startDate?: string, endDate?: string): Promise<SymptomDiarySheetSummary[]> {
  const params = new URLSearchParams();
  if (startDate) params.set('startDate', startDate);
  if (endDate) params.set('endDate', endDate);
  const q = params.toString();
  return api<SymptomDiarySheetSummary[]>(`/symptom-diary/sheets${q ? `?${q}` : ''}`);
}

export async function getDiarySheet(date: string): Promise<SymptomDiarySheet> {
  return api<SymptomDiarySheet>(`/symptom-diary/sheets/${date}`);
}

export async function updateDiarySheet(date: string, request: UpdateDailySheetRequest): Promise<SymptomDiarySheet> {
  return api<SymptomDiarySheet>(`/symptom-diary/sheets/${date}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function addDiaryEntry(date: string, request: UpsertSymptomEntryRequest): Promise<SymptomDiaryEntry> {
  return api<SymptomDiaryEntry>(`/symptom-diary/sheets/${date}/entries`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateDiaryEntry(id: string, request: UpsertSymptomEntryRequest): Promise<SymptomDiaryEntry> {
  return api<SymptomDiaryEntry>(`/symptom-diary/entries/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function deleteDiaryEntry(id: string): Promise<void> {
  return api<void>(`/symptom-diary/entries/${id}`, {
    method: 'DELETE',
  });
}

export async function getDiaryAnalytics(days = 30): Promise<SymptomDiaryAnalytics> {
  return api<SymptomDiaryAnalytics>(`/symptom-diary/summary?days=${days}`);
}

export async function getBotApiKey(): Promise<BotApiKeyDto | null> {
  return api<BotApiKeyDto | null>('/symptom-diary/bot-key');
}

export async function createBotApiKey(label?: string): Promise<CreateBotKeyResponse> {
  return api<CreateBotKeyResponse>('/symptom-diary/bot-key', {
    method: 'POST',
    body: JSON.stringify({ label: label || 'n8n Chatbot' }),
  });
}

export async function deleteBotApiKey(id: string): Promise<void> {
  return api<void>(`/symptom-diary/bot-key/${id}`, {
    method: 'DELETE',
  });
}

