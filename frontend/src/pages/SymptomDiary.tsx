import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Grid,
  IconButton,
  InputAdornment,
  MenuItem,
  Paper,
  Slider,
  Snackbar,
  Stack,
  SvgIcon,
  Tab,
  Tabs,
  TextField,
  Tooltip,
  Typography,
  useTheme,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ErrorState, Loading } from '../components/PageState';
import {
  addDiaryEntry,
  createBotApiKey,
  deleteBotApiKey,
  deleteDiaryEntry,
  getBotApiKey,
  getDiaryAnalytics,
  getDiarySheet,
  SymptomCategory,
  SymptomDiaryEntry,
  updateDiaryEntry,
  updateDiarySheet,
  UpsertSymptomEntryRequest,
} from '../services/symptomDiary';

const CATEGORIES: SymptomCategory[] = [
  'Pain',
  'Fatigue',
  'Neurological',
  'Digestive',
  'Respiratory',
  'Musculoskeletal',
  'MoodPsychological',
  'Sleep',
  'Skin',
  'Other',
];

const PAIN_TYPE_SUGGESTIONS = [
  'Sharp',
  'Burning',
  'Throbbing',
  'Dull',
  'Aching',
  'Stabbing',
  'Radiating',
  'Cramping',
  'Stiffness',
  'Tingling',
  'Shooting',
  'Pressure',
];

const BODY_LOCATION_SUGGESTIONS = [
  'Lower Back',
  'Upper Back',
  'Neck',
  'Head / Temples',
  'Left Shoulder',
  'Right Shoulder',
  'Chest',
  'Abdomen',
  'Left Hip',
  'Right Hip',
  'Left Knee',
  'Right Knee',
  'Hands / Wrists',
  'Feet / Ankles',
];

function getSeverityColor(sev: number): string {
  if (sev <= 3) return '#00695c';
  if (sev <= 6) return '#b06f42';
  if (sev <= 8) return '#d9534f';
  return '#900c3f';
}

function getTodayIso(): string {
  return new Date().toISOString().split('T')[0];
}

function shiftDateIso(isoDate: string, days: number): string {
  const d = new Date(isoDate + 'T00:00:00');
  d.setDate(d.getDate() + days);
  return d.toISOString().split('T')[0];
}

function formatDisplayDate(isoDate: string, lang = 'en'): string {
  try {
    const d = new Date(isoDate + 'T00:00:00');
    return new Intl.DateTimeFormat(lang, {
      weekday: 'long',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(d);
  } catch {
    return isoDate;
  }
}

function formatTime(isoDateTime: string): string {
  try {
    const d = new Date(isoDateTime);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  } catch {
    return '';
  }
}

// Custom icons
function CalendarIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2" fill="none" stroke="currentColor" strokeWidth="2" />
      <line x1="16" y1="2" x2="16" y2="6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <line x1="8" y1="2" x2="8" y2="6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <line x1="3" y1="10" x2="21" y2="10" stroke="currentColor" strokeWidth="2" />
    </SvgIcon>
  );
}

function PlusIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" d="M12 5v14M5 12h14" />
    </SvgIcon>
  );
}

function TrashIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"
      />
    </SvgIcon>
  );
}

function EditIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"
      />
    </SvgIcon>
  );
}

function KeyIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <path
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M21 2l-2 2m-1.5 1.5L14 9l-3-3 2-2 1.5 1.5L19 3l2-1zM11 13a5 5 0 1 1-7-7 5 5 0 0 1 7 7zm0 0l7 7m-3-1l2 2"
      />
    </SvgIcon>
  );
}

function BotIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <rect x="3" y="11" width="18" height="10" rx="2" fill="none" stroke="currentColor" strokeWidth="2" />
      <circle cx="12" cy="5" r="2" fill="none" stroke="currentColor" strokeWidth="2" />
      <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" d="M12 7v4M8 16h.01M16 16h.01" />
    </SvgIcon>
  );
}

export default function SymptomDiary() {
  const { t, i18n } = useTranslation();
  const theme = useTheme();
  const client = useQueryClient();
  const currentLang = i18n.language?.split('-')[0] ?? 'en';

  const [currentTab, setCurrentTab] = useState<'daily' | 'analytics' | 'bot'>('daily');
  const [selectedDate, setSelectedDate] = useState<string>(getTodayIso());
  const [notice, setNotice] = useState<string>('');

  // Daily Check-in Form state
  const [wellbeing, setWellbeing] = useState<number | null>(null);
  const [sleepQuality, setSleepQuality] = useState<number | null>(null);
  const [sleepHours, setSleepHours] = useState<string>('');
  const [dailyNotes, setDailyNotes] = useState<string>('');

  // Dialog State
  const [dialogOpen, setDialogOpen] = useState<boolean>(false);
  const [editingEntryId, setEditingEntryId] = useState<string | null>(null);
  const [entryCategory, setEntryCategory] = useState<string>('Pain');
  const [entrySymptomName, setEntrySymptomName] = useState<string>('');
  const [entryPainTypes, setEntryPainTypes] = useState<string[]>([]);
  const [entryCustomPainType, setEntryCustomPainType] = useState<string>('');
  const [entryLocations, setEntryLocations] = useState<string[]>([]);
  const [entryCustomLocation, setEntryCustomLocation] = useState<string>('');
  const [entrySeverity, setEntrySeverity] = useState<number>(5);
  const [entryDurationMinutes, setEntryDurationMinutes] = useState<string>('');
  const [entryTriggers, setEntryTriggers] = useState<string>('');
  const [entryRelievers, setEntryRelievers] = useState<string>('');
  const [entryMedications, setEntryMedications] = useState<string>('');
  const [entryNotes, setEntryNotes] = useState<string>('');
  const [entryRecordedTime, setEntryRecordedTime] = useState<string>('');

  // Bot API Key Modal State
  const [newKeyModalOpen, setNewKeyModalOpen] = useState<boolean>(false);
  const [generatedKey, setGeneratedKey] = useState<string>('');

  // Analytics Lookback State
  const [analyticsDays, setAnalyticsDays] = useState<number>(30);

  // Queries
  const sheetQuery = useQuery({
    queryKey: ['symptom-sheet', selectedDate],
    queryFn: () => getDiarySheet(selectedDate),
  });

  const analyticsQuery = useQuery({
    queryKey: ['symptom-analytics', analyticsDays],
    queryFn: () => getDiaryAnalytics(analyticsDays),
    enabled: currentTab === 'analytics',
  });

  const botKeyQuery = useQuery({
    queryKey: ['symptom-bot-key'],
    queryFn: () => getBotApiKey(),
    enabled: currentTab === 'bot',
  });

  // Sync sheet form when sheetQuery resolves
  useEffect(() => {
    if (sheetQuery.data) {
      setWellbeing(sheetQuery.data.overallWellbeing ?? null);
      setSleepQuality(sheetQuery.data.sleepQuality ?? null);
      setSleepHours(sheetQuery.data.sleepHours !== null && sheetQuery.data.sleepHours !== undefined ? String(sheetQuery.data.sleepHours) : '');
      setDailyNotes(sheetQuery.data.dailyNotes ?? '');
    }
  }, [sheetQuery.data]);

  // Mutations
  const saveSheetMutation = useMutation({
    mutationFn: () =>
      updateDiarySheet(selectedDate, {
        overallWellbeing: wellbeing,
        sleepQuality: sleepQuality,
        sleepHours: sleepHours ? parseFloat(sleepHours) : null,
        dailyNotes: dailyNotes || null,
      }),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['symptom-sheet', selectedDate] });
      client.invalidateQueries({ queryKey: ['symptom-analytics'] });
      setNotice(t('diary.checkin.saved'));
    },
  });

  const saveEntryMutation = useMutation({
    mutationFn: async () => {
      const allPainTypes = [...entryPainTypes, ...(entryCustomPainType.trim() ? [entryCustomPainType.trim()] : [])].join(', ');
      const allLocations = [...entryLocations, ...(entryCustomLocation.trim() ? [entryCustomLocation.trim()] : [])].join(', ');

      const req: UpsertSymptomEntryRequest = {
        date: selectedDate,
        recordedAt: entryRecordedTime ? `${selectedDate}T${entryRecordedTime}:00Z` : new Date().toISOString(),
        category: entryCategory,
        symptomName: entrySymptomName.trim(),
        painType: allPainTypes || null,
        bodyLocation: allLocations || null,
        severity: entrySeverity,
        durationMinutes: entryDurationMinutes ? parseInt(entryDurationMinutes, 10) : null,
        triggers: entryTriggers.trim() || null,
        relievers: entryRelievers.trim() || null,
        medicationsTaken: entryMedications.trim() || null,
        notes: entryNotes.trim() || null,
        source: 'Web',
      };

      if (editingEntryId) {
        return updateDiaryEntry(editingEntryId, req);
      } else {
        return addDiaryEntry(selectedDate, req);
      }
    },
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['symptom-sheet', selectedDate] });
      client.invalidateQueries({ queryKey: ['symptom-analytics'] });
      setDialogOpen(false);
      setNotice(t('diary.entries.saved'));
    },
  });

  const deleteEntryMutation = useMutation({
    mutationFn: (id: string) => deleteDiaryEntry(id),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['symptom-sheet', selectedDate] });
      client.invalidateQueries({ queryKey: ['symptom-analytics'] });
      setNotice(t('diary.entries.deleted'));
    },
  });

  const generateBotKeyMutation = useMutation({
    mutationFn: () => createBotApiKey('n8n Chatbot'),
    onSuccess: (data) => {
      client.invalidateQueries({ queryKey: ['symptom-bot-key'] });
      setGeneratedKey(data.apiKey);
      setNewKeyModalOpen(true);
    },
  });

  const revokeBotKeyMutation = useMutation({
    mutationFn: (id: string) => deleteBotApiKey(id),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['symptom-bot-key'] });
      setNotice('Bot API key revoked.');
    },
  });

  // Open Dialog for Add
  const handleOpenAdd = () => {
    setEditingEntryId(null);
    setEntryCategory('Pain');
    setEntrySymptomName('');
    setEntryPainTypes([]);
    setEntryCustomPainType('');
    setEntryLocations([]);
    setEntryCustomLocation('');
    setEntrySeverity(5);
    setEntryDurationMinutes('');
    setEntryTriggers('');
    setEntryRelievers('');
    setEntryMedications('');
    setEntryNotes('');
    const now = new Date();
    const hh = String(now.getHours()).padStart(2, '0');
    const mm = String(now.getMinutes()).padStart(2, '0');
    setEntryRecordedTime(`${hh}:${mm}`);
    setDialogOpen(true);
  };

  // Open Dialog for Edit
  const handleOpenEdit = (entry: SymptomDiaryEntry) => {
    setEditingEntryId(entry.id);
    setEntryCategory(entry.category);
    setEntrySymptomName(entry.symptomName);

    const savedTypes = entry.painType ? entry.painType.split(',').map((s) => s.trim()).filter(Boolean) : [];
    const knownTypes = savedTypes.filter((t) => PAIN_TYPE_SUGGESTIONS.includes(t));
    const customTypes = savedTypes.filter((t) => !PAIN_TYPE_SUGGESTIONS.includes(t));
    setEntryPainTypes(knownTypes);
    setEntryCustomPainType(customTypes.join(', '));

    const savedLocs = entry.bodyLocation ? entry.bodyLocation.split(',').map((s) => s.trim()).filter(Boolean) : [];
    const knownLocs = savedLocs.filter((l) => BODY_LOCATION_SUGGESTIONS.includes(l));
    const customLocs = savedLocs.filter((l) => !BODY_LOCATION_SUGGESTIONS.includes(l));
    setEntryLocations(knownLocs);
    setEntryCustomLocation(customLocs.join(', '));

    setEntrySeverity(entry.severity);
    setEntryDurationMinutes(entry.durationMinutes ? String(entry.durationMinutes) : '');
    setEntryTriggers(entry.triggers ?? '');
    setEntryRelievers(entry.relievers ?? '');
    setEntryMedications(entry.medicationsTaken ?? '');
    setEntryNotes(entry.notes ?? '');

    try {
      const d = new Date(entry.recordedAt);
      const hh = String(d.getHours()).padStart(2, '0');
      const mm = String(d.getMinutes()).padStart(2, '0');
      setEntryRecordedTime(`${hh}:${mm}`);
    } catch {
      setEntryRecordedTime('12:00');
    }

    setDialogOpen(true);
  };

  const togglePainType = (type: string) => {
    setEntryPainTypes((prev) =>
      prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type]
    );
  };

  const toggleLocation = (loc: string) => {
    setEntryLocations((prev) =>
      prev.includes(loc) ? prev.filter((l) => l !== loc) : [...prev, loc]
    );
  };

  const copyToClipboard = (text: string) => {
    void navigator.clipboard.writeText(text);
    setNotice(t('diary.bot.copied'));
  };

  if (sheetQuery.isLoading) return <Loading />;
  if (sheetQuery.error) return <ErrorState error={sheetQuery.error} />;

  const sheet = sheetQuery.data;
  const entries = sheet?.entries ?? [];
  const activeBotKey = botKeyQuery.data;
  const analytics = analyticsQuery.data;

  return (
    <Container sx={{ py: { xs: 3, md: 5 } }}>
      <Stack spacing={3}>
        {/* Header Title */}
        <Box>
          <Typography
            sx={{
              color: '#b06f42',
              fontWeight: 800,
              textTransform: 'uppercase',
              letterSpacing: '.12em',
              fontSize: '.75rem',
            }}
          >
            {t('diary.title')}
          </Typography>
          <Typography variant="h4" sx={{ fontWeight: 800, mt: 0.5, letterSpacing: '-.02em' }}>
            {t('diary.title')}
          </Typography>
          <Typography sx={{ color: 'text.secondary', mt: 0.5, maxWidth: 800 }}>
            {t('diary.subtitle')}
          </Typography>
        </Box>

        {/* Tab Selector */}
        <Paper sx={{ borderRadius: 3, overflow: 'hidden', p: 0.5 }}>
          <Tabs
            value={currentTab}
            onChange={(_, val) => setCurrentTab(val)}
            variant="fullWidth"
            textColor="primary"
            indicatorColor="primary"
          >
            <Tab
              value="daily"
              label={t('diary.tabs.daily')}
              icon={<CalendarIcon />}
              iconPosition="start"
              sx={{ textTransform: 'none', fontWeight: 600, fontSize: '.9rem' }}
            />
            <Tab
              value="analytics"
              label={t('diary.tabs.analytics')}
              sx={{ textTransform: 'none', fontWeight: 600, fontSize: '.9rem' }}
            />
            <Tab
              value="bot"
              label={t('diary.tabs.bot')}
              icon={<BotIcon />}
              iconPosition="start"
              sx={{ textTransform: 'none', fontWeight: 600, fontSize: '.9rem' }}
            />
          </Tabs>
        </Paper>

        {/* ========================================================================= */}
        {/* TAB 1: DAILY DIARY */}
        {/* ========================================================================= */}
        {currentTab === 'daily' && (
          <Stack spacing={3}>
            {/* Date Navigator Bar */}
            <Card sx={{ borderRadius: 3 }}>
              <CardContent sx={{ py: 2, '&:last-child': { pb: 2 } }}>
                <Grid container alignItems="center" spacing={2}>
                  <Grid item xs={12} md={4}>
                    <Stack direction="row" alignItems="center" spacing={1}>
                      <Button
                        size="small"
                        variant={selectedDate === getTodayIso() ? 'contained' : 'outlined'}
                        onClick={() => setSelectedDate(getTodayIso())}
                        sx={{ borderRadius: '9999px', textTransform: 'none', fontWeight: 600 }}
                      >
                        {t('diary.dateNav.today')}
                      </Button>
                      <IconButton
                        size="small"
                        onClick={() => setSelectedDate((prev) => shiftDateIso(prev, -1))}
                        aria-label={t('diary.dateNav.previousDay')}
                      >
                        ‹
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={() => setSelectedDate((prev) => shiftDateIso(prev, 1))}
                        aria-label={t('diary.dateNav.nextDay')}
                      >
                        ›
                      </IconButton>
                    </Stack>
                  </Grid>

                  <Grid item xs={12} md={5} textAlign={{ xs: 'left', md: 'center' }}>
                    <Typography variant="h6" sx={{ fontWeight: 700 }}>
                      {formatDisplayDate(selectedDate, currentLang)}
                    </Typography>
                  </Grid>

                  <Grid item xs={12} md={3} textAlign={{ xs: 'left', md: 'right' }}>
                    <TextField
                      type="date"
                      size="small"
                      value={selectedDate}
                      onChange={(e) => e.target.value && setSelectedDate(e.target.value)}
                      InputLabelProps={{ shrink: true }}
                      sx={{ width: { xs: '100%', sm: 180 } }}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>

            {/* Daily Check-in (Wellbeing & Sleep) */}
            <Card sx={{ borderRadius: 3, border: '1px solid rgba(0,105,92,.15)' }}>
              <CardContent sx={{ p: 3 }}>
                <Stack spacing={2.5}>
                  <Stack direction="row" alignItems="center" justifyContent="space-between">
                    <Typography variant="h6" sx={{ fontWeight: 700, color: '#00695c' }}>
                      {t('diary.checkin.title')}
                    </Typography>
                    {sheet?.updatedAt && (
                      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                        {formatTime(sheet.updatedAt)}
                      </Typography>
                    )}
                  </Stack>

                  <Grid container spacing={3}>
                    {/* Wellbeing (1 to 5) */}
                    <Grid item xs={12} sm={4}>
                      <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1 }}>
                        {t('diary.checkin.wellbeing')} (1-5)
                      </Typography>
                      <Stack direction="row" spacing={0.5}>
                        {[1, 2, 3, 4, 5].map((val) => {
                          const isSelected = wellbeing === val;
                          return (
                            <Button
                              key={val}
                              size="small"
                              variant={isSelected ? 'contained' : 'outlined'}
                              color={isSelected ? 'primary' : 'inherit'}
                              onClick={() => setWellbeing(val)}
                              sx={{
                                minWidth: 38,
                                height: 38,
                                borderRadius: '10px',
                                fontWeight: 700,
                              }}
                            >
                              {val}
                            </Button>
                          );
                        })}
                      </Stack>
                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mt: 0.5 }}>
                        {wellbeing ? t(`diary.checkin.ratingLabels.wellbeing${wellbeing}`) : '1 = Poor, 5 = Excellent'}
                      </Typography>
                    </Grid>

                    {/* Sleep Quality (1 to 5) */}
                    <Grid item xs={12} sm={4}>
                      <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1 }}>
                        {t('diary.checkin.sleepQuality')} (1-5)
                      </Typography>
                      <Stack direction="row" spacing={0.5}>
                        {[1, 2, 3, 4, 5].map((val) => {
                          const isSelected = sleepQuality === val;
                          return (
                            <Button
                              key={val}
                              size="small"
                              variant={isSelected ? 'contained' : 'outlined'}
                              color={isSelected ? 'primary' : 'inherit'}
                              onClick={() => setSleepQuality(val)}
                              sx={{
                                minWidth: 38,
                                height: 38,
                                borderRadius: '10px',
                                fontWeight: 700,
                              }}
                            >
                              {val}
                            </Button>
                          );
                        })}
                      </Stack>
                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mt: 0.5 }}>
                        {sleepQuality ? t(`diary.checkin.ratingLabels.sleep${sleepQuality}`) : '1 = Restless, 5 = Deep'}
                      </Typography>
                    </Grid>

                    {/* Sleep Hours */}
                    <Grid item xs={12} sm={4}>
                      <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1 }}>
                        {t('diary.checkin.sleepHours')}
                      </Typography>
                      <TextField
                        size="small"
                        type="number"
                        placeholder="7.5"
                        value={sleepHours}
                        onChange={(e) => setSleepHours(e.target.value)}
                        inputProps={{ step: '0.5', min: '0', max: '24' }}
                        InputProps={{
                          endAdornment: <InputAdornment position="end">hrs</InputAdornment>,
                        }}
                        fullWidth
                      />
                    </Grid>

                    {/* Daily Notes */}
                    <Grid item xs={12}>
                      <TextField
                        label={t('diary.checkin.dailyNotes')}
                        placeholder={t('diary.checkin.dailyNotesPlaceholder')}
                        value={dailyNotes}
                        onChange={(e) => setDailyNotes(e.target.value)}
                        multiline
                        rows={2}
                        fullWidth
                      />
                    </Grid>
                  </Grid>

                  <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                    <Button
                      variant="contained"
                      onClick={() => saveSheetMutation.mutate()}
                      disabled={saveSheetMutation.isPending}
                      sx={{ borderRadius: '9999px', textTransform: 'none', px: 3, fontWeight: 700 }}
                    >
                      {saveSheetMutation.isPending ? t('common.saving') : t('diary.checkin.save')}
                    </Button>
                  </Box>
                </Stack>
              </CardContent>
            </Card>

            {/* Logged Symptoms List Section */}
            <Stack spacing={2}>
              <Stack direction="row" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>
                    {t('diary.entries.title')}
                  </Typography>
                  <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                    {t('diary.entries.count', { count: entries.length })}
                  </Typography>
                </Box>
                <Button
                  variant="contained"
                  color="primary"
                  startIcon={<PlusIcon />}
                  onClick={handleOpenAdd}
                  sx={{ borderRadius: '9999px', textTransform: 'none', fontWeight: 700, px: 2.5 }}
                >
                  {t('diary.entries.add')}
                </Button>
              </Stack>

              {entries.length === 0 ? (
                <Paper
                  sx={{
                    p: 4,
                    textAlign: 'center',
                    borderRadius: 3,
                    bgcolor: 'background.paper',
                    border: '1px dashed rgba(0,0,0,.15)',
                  }}
                >
                  <Typography variant="subtitle1" sx={{ fontWeight: 700, color: 'text.secondary' }}>
                    {t('diary.entries.empty')}
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5, maxWidth: 500, mx: 'auto' }}>
                    {t('diary.entries.emptyHint')}
                  </Typography>
                  <Button
                    variant="outlined"
                    startIcon={<PlusIcon />}
                    onClick={handleOpenAdd}
                    sx={{ mt: 2, borderRadius: '9999px', textTransform: 'none', fontWeight: 600 }}
                  >
                    {t('diary.entries.add')}
                  </Button>
                </Paper>
              ) : (
                <Stack spacing={2}>
                  {entries.map((entry) => {
                    const sevColor = getSeverityColor(entry.severity);
                    return (
                      <Card
                        key={entry.id}
                        sx={{
                          borderRadius: 3,
                          borderLeft: `6px solid ${sevColor}`,
                          boxShadow: '0 2px 8px rgba(0,0,0,.04)',
                        }}
                      >
                        <CardContent sx={{ p: 2.5, '&:last-child': { pb: 2.5 } }}>
                          <Stack spacing={1.5}>
                            {/* Entry Header */}
                            <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
                              <Stack direction="row" alignItems="center" spacing={1.5} flexWrap="wrap">
                                <Typography variant="h6" sx={{ fontWeight: 800 }}>
                                  {entry.symptomName}
                                </Typography>
                                <Chip
                                  label={t(`diary.categories.${entry.category}`, entry.category)}
                                  size="small"
                                  sx={{
                                    bgcolor: 'rgba(0,105,92,.1)',
                                    color: '#00695c',
                                    fontWeight: 700,
                                    fontSize: '.75rem',
                                  }}
                                />
                                <Chip
                                  label={`Severity ${entry.severity}/10`}
                                  size="small"
                                  sx={{
                                    bgcolor: sevColor,
                                    color: '#fff',
                                    fontWeight: 800,
                                    fontSize: '.75rem',
                                  }}
                                />
                                {entry.source && entry.source !== 'Web' && (
                                  <Chip
                                    icon={<BotIcon />}
                                    label={entry.source}
                                    size="small"
                                    variant="outlined"
                                    sx={{ fontSize: '.7rem', borderColor: 'rgba(0,0,0,.2)' }}
                                  />
                                )}
                              </Stack>

                              {/* Action Buttons */}
                              <Stack direction="row" spacing={0.5}>
                                <Tooltip title={t('diary.entries.edit')}>
                                  <IconButton size="small" onClick={() => handleOpenEdit(entry)}>
                                    <EditIcon />
                                  </IconButton>
                                </Tooltip>
                                <Tooltip title={t('diary.entries.delete')}>
                                  <IconButton
                                    size="small"
                                    color="error"
                                    onClick={() => {
                                      if (window.confirm(t('diary.entries.deleteConfirm'))) {
                                        deleteEntryMutation.mutate(entry.id);
                                      }
                                    }}
                                  >
                                    <TrashIcon />
                                  </IconButton>
                                </Tooltip>
                              </Stack>
                            </Stack>

                            {/* Tags / Sub-properties */}
                            <Stack direction="row" spacing={1} flexWrap="wrap" sx={{ gap: 0.5 }}>
                              {entry.painType && (
                                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                                  <strong>{t('diary.dialog.painTypes')}:</strong> {entry.painType}
                                </Typography>
                              )}
                              {entry.bodyLocation && (
                                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                                  <strong>{t('diary.dialog.location')}:</strong> {entry.bodyLocation}
                                </Typography>
                              )}
                              {entry.durationMinutes && (
                                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                                  <strong>{t('diary.dialog.durationMinutes')}:</strong> {entry.durationMinutes} min
                                </Typography>
                              )}
                              {entry.recordedAt && (
                                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                                  <strong>{t('diary.dialog.recordedAt')}:</strong> {formatTime(entry.recordedAt)}
                                </Typography>
                              )}
                            </Stack>

                            {/* Triggers, Relievers, Meds */}
                            {(entry.triggers || entry.relievers || entry.medicationsTaken) && (
                              <Box sx={{ bgcolor: 'rgba(0,0,0,.02)', p: 1.5, borderRadius: 2 }}>
                                <Grid container spacing={1}>
                                  {entry.triggers && (
                                    <Grid item xs={12} sm={4}>
                                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block' }}>
                                        <strong>{t('diary.dialog.triggers')}:</strong> {entry.triggers}
                                      </Typography>
                                    </Grid>
                                  )}
                                  {entry.relievers && (
                                    <Grid item xs={12} sm={4}>
                                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block' }}>
                                        <strong>{t('diary.dialog.relievers')}:</strong> {entry.relievers}
                                      </Typography>
                                    </Grid>
                                  )}
                                  {entry.medicationsTaken && (
                                    <Grid item xs={12} sm={4}>
                                      <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block' }}>
                                        <strong>{t('diary.dialog.medications')}:</strong> {entry.medicationsTaken}
                                      </Typography>
                                    </Grid>
                                  )}
                                </Grid>
                              </Box>
                            )}

                            {/* Notes */}
                            {entry.notes && (
                              <Typography variant="body2" sx={{ color: 'text.primary', fontStyle: 'italic' }}>
                                "{entry.notes}"
                              </Typography>
                            )}
                          </Stack>
                        </CardContent>
                      </Card>
                    );
                  })}
                </Stack>
              )}
            </Stack>
          </Stack>
        )}

        {/* ========================================================================= */}
        {/* TAB 2: ANALYTICS & TRENDS */}
        {/* ========================================================================= */}
        {currentTab === 'analytics' && (
          <Stack spacing={3}>
            {/* Timeframe Selector */}
            <Stack direction="row" alignItems="center" justifyContent="space-between">
              <Typography variant="h6" sx={{ fontWeight: 800 }}>
                {t('diary.analytics.title')}
              </Typography>
              <Stack direction="row" spacing={1}>
                {[7, 30, 90].map((days) => (
                  <Button
                    key={days}
                    size="small"
                    variant={analyticsDays === days ? 'contained' : 'outlined'}
                    onClick={() => setAnalyticsDays(days)}
                    sx={{ borderRadius: '9999px', textTransform: 'none', fontWeight: 600 }}
                  >
                    {t(`diary.analytics.timeframe${days}`)}
                  </Button>
                ))}
              </Stack>
            </Stack>

            {analyticsQuery.isLoading ? (
              <Box sx={{ py: 6, textAlign: 'center' }}>
                <CircularProgress />
              </Box>
            ) : analytics && analytics.totalEntries > 0 ? (
              <Stack spacing={3}>
                {/* KPI Cards */}
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ borderRadius: 3, textAlign: 'center', p: 2 }}>
                      <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 700, textTransform: 'uppercase' }}>
                        {t('diary.analytics.totalEntries')}
                      </Typography>
                      <Typography variant="h3" sx={{ fontWeight: 800, color: '#00695c', mt: 0.5 }}>
                        {analytics.totalEntries}
                      </Typography>
                    </Card>
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ borderRadius: 3, textAlign: 'center', p: 2 }}>
                      <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 700, textTransform: 'uppercase' }}>
                        {t('diary.analytics.daysTracked')}
                      </Typography>
                      <Typography variant="h3" sx={{ fontWeight: 800, color: '#b06f42', mt: 0.5 }}>
                        {analytics.daysTracked}
                      </Typography>
                    </Card>
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ borderRadius: 3, textAlign: 'center', p: 2 }}>
                      <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 700, textTransform: 'uppercase' }}>
                        {t('diary.analytics.avgSeverity')}
                      </Typography>
                      <Typography
                        variant="h3"
                        sx={{ fontWeight: 800, color: getSeverityColor(analytics.overallAverageSeverity), mt: 0.5 }}
                      >
                        {analytics.overallAverageSeverity.toFixed(1)} / 10
                      </Typography>
                    </Card>
                  </Grid>
                </Grid>

                {/* Severity Trend Timeline Bar Chart */}
                <Card sx={{ borderRadius: 3, p: 3 }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 2 }}>
                    {t('diary.analytics.severityTrend')}
                  </Typography>
                  <Stack spacing={1.5}>
                    {analytics.severityTrend.map((point) => {
                      const percentage = (point.averageSeverity / 10) * 100;
                      const sevColor = getSeverityColor(point.averageSeverity);
                      return (
                        <Grid container key={point.date} alignItems="center" spacing={1.5}>
                          <Grid item xs={3} sm={2}>
                            <Typography variant="caption" sx={{ fontWeight: 700 }}>
                              {point.date}
                            </Typography>
                          </Grid>
                          <Grid item xs={7} sm={8}>
                            <Box sx={{ width: '100%', bgcolor: 'rgba(0,0,0,.06)', borderRadius: 2, height: 18, overflow: 'hidden' }}>
                              <Box
                                sx={{
                                  width: `${Math.max(percentage, 5)}%`,
                                  bgcolor: sevColor,
                                  height: '100%',
                                  borderRadius: 2,
                                  transition: 'width .3s ease',
                                }}
                              />
                            </Box>
                          </Grid>
                          <Grid item xs={2} sm={2} textAlign="right">
                            <Typography variant="caption" sx={{ fontWeight: 800, color: sevColor }}>
                              {point.averageSeverity.toFixed(1)}/10 ({point.entryCount})
                            </Typography>
                          </Grid>
                        </Grid>
                      );
                    })}
                  </Stack>
                </Card>

                {/* Top Breakdown Cards */}
                <Grid container spacing={3}>
                  {/* Top Symptoms */}
                  <Grid item xs={12} md={4}>
                    <Card sx={{ borderRadius: 3, p: 2.5, height: '100%' }}>
                      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
                        {t('diary.analytics.topSymptoms')}
                      </Typography>
                      <Stack spacing={1}>
                        {analytics.topSymptoms.map((sym, idx) => (
                          <Stack key={idx} direction="row" alignItems="center" justifyContent="space-between">
                            <Box>
                              <Typography variant="body2" sx={{ fontWeight: 700 }}>
                                {sym.name}
                              </Typography>
                              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                                Avg sev: {sym.averageSeverity.toFixed(1)}/10
                              </Typography>
                            </Box>
                            <Chip size="small" label={`${sym.count}x`} sx={{ fontWeight: 700 }} />
                          </Stack>
                        ))}
                      </Stack>
                    </Card>
                  </Grid>

                  {/* Top Locations */}
                  <Grid item xs={12} md={4}>
                    <Card sx={{ borderRadius: 3, p: 2.5, height: '100%' }}>
                      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
                        {t('diary.analytics.topLocations')}
                      </Typography>
                      {analytics.topLocations.length === 0 ? (
                        <Typography variant="caption" sx={{ color: 'text.secondary' }}>No locations recorded</Typography>
                      ) : (
                        <Stack spacing={1}>
                          {analytics.topLocations.map((loc, idx) => (
                            <Stack key={idx} direction="row" alignItems="center" justifyContent="space-between">
                              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {loc.location}
                              </Typography>
                              <Chip size="small" label={`${loc.count}x`} sx={{ fontWeight: 700 }} />
                            </Stack>
                          ))}
                        </Stack>
                      )}
                    </Card>
                  </Grid>

                  {/* Top Pain Types */}
                  <Grid item xs={12} md={4}>
                    <Card sx={{ borderRadius: 3, p: 2.5, height: '100%' }}>
                      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
                        {t('diary.analytics.topPainTypes')}
                      </Typography>
                      {analytics.topPainTypes.length === 0 ? (
                        <Typography variant="caption" sx={{ color: 'text.secondary' }}>No pain types recorded</Typography>
                      ) : (
                        <Stack spacing={1}>
                          {analytics.topPainTypes.map((pt, idx) => (
                            <Stack key={idx} direction="row" alignItems="center" justifyContent="space-between">
                              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {pt.painType}
                              </Typography>
                              <Chip size="small" label={`${pt.count}x`} sx={{ fontWeight: 700 }} />
                            </Stack>
                          ))}
                        </Stack>
                      )}
                    </Card>
                  </Grid>
                </Grid>
              </Stack>
            ) : (
              <Paper sx={{ p: 4, textAlign: 'center', borderRadius: 3 }}>
                <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                  {t('diary.analytics.noData')}
                </Typography>
              </Paper>
            )}
          </Stack>
        )}

        {/* ========================================================================= */}
        {/* TAB 3: n8n / CHATBOT INTEGRATION */}
        {/* ========================================================================= */}
        {currentTab === 'bot' && (
          <Stack spacing={3}>
            {/* Intro Card */}
            <Card sx={{ borderRadius: 3, bgcolor: 'rgba(0,105,92,.04)', border: '1px solid rgba(0,105,92,.2)' }}>
              <CardContent sx={{ p: 3 }}>
                <Stack direction="row" spacing={2} alignItems="center">
                  <Box
                    sx={{
                      width: 48,
                      height: 48,
                      borderRadius: '12px',
                      bgcolor: '#00695c',
                      color: '#fff',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <BotIcon />
                  </Box>
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 800, color: '#00695c' }}>
                      {t('diary.bot.eyebrow')}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.25 }}>
                      {t('diary.bot.description')}
                    </Typography>
                  </Box>
                </Stack>
              </CardContent>
            </Card>

            {/* API Key Management Card */}
            <Card sx={{ borderRadius: 3 }}>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" sx={{ fontWeight: 800, mb: 1 }}>
                  {t('diary.bot.keyTitle')}
                </Typography>

                {botKeyQuery.isLoading ? (
                  <CircularProgress size={24} />
                ) : activeBotKey ? (
                  <Stack spacing={2} sx={{ mt: 2 }}>
                    <Alert severity="success" icon={<KeyIcon />}>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {t('diary.bot.keyActive', {
                          prefix: activeBotKey.keyPrefix,
                          date: new Date(activeBotKey.createdAt).toLocaleDateString(),
                        })}
                      </Typography>
                      <Typography variant="caption" sx={{ display: 'block', mt: 0.5 }}>
                        {activeBotKey.lastUsedAt
                          ? t('diary.bot.keyLastUsed', { date: new Date(activeBotKey.lastUsedAt).toLocaleString() })
                          : t('diary.bot.keyNeverUsed')}
                      </Typography>
                    </Alert>

                    <Stack direction="row" spacing={1.5}>
                      <Button
                        variant="contained"
                        onClick={() => generateBotKeyMutation.mutate()}
                        disabled={generateBotKeyMutation.isPending}
                        sx={{ borderRadius: '9999px', textTransform: 'none', fontWeight: 700 }}
                      >
                        {generateBotKeyMutation.isPending ? 'Generating...' : t('diary.bot.generate')}
                      </Button>
                      <Button
                        variant="outlined"
                        color="error"
                        onClick={() => {
                          if (window.confirm('Are you sure you want to revoke this API key?')) {
                            revokeBotKeyMutation.mutate(activeBotKey.id);
                          }
                        }}
                        sx={{ borderRadius: '9999px', textTransform: 'none', fontWeight: 600 }}
                      >
                        {t('diary.bot.revoke')}
                      </Button>
                    </Stack>
                  </Stack>
                ) : (
                  <Stack spacing={2} sx={{ mt: 2 }}>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      You do not have an active bot API key yet. Generate one to link your n8n workflow.
                    </Typography>
                    <Button
                      variant="contained"
                      startIcon={<KeyIcon />}
                      onClick={() => generateBotKeyMutation.mutate()}
                      disabled={generateBotKeyMutation.isPending}
                      sx={{ borderRadius: '9999px', textTransform: 'none', fontWeight: 700, alignSelf: 'flex-start' }}
                    >
                      {generateBotKeyMutation.isPending ? 'Generating...' : t('diary.bot.generate')}
                    </Button>
                  </Stack>
                )}
              </CardContent>
            </Card>

            {/* Ingestion cURL and Payload Schema Card */}
            <Card sx={{ borderRadius: 3 }}>
              <CardContent sx={{ p: 3 }}>
                <Stack spacing={2.5}>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>
                    {t('diary.bot.quickCurl')}
                  </Typography>

                  <Box
                    sx={{
                      bgcolor: '#111b1b',
                      color: '#64d8cb',
                      p: 2,
                      borderRadius: 2,
                      fontFamily: 'monospace',
                      fontSize: '.85rem',
                      overflowX: 'auto',
                      position: 'relative',
                    }}
                  >
                    <Button
                      size="small"
                      variant="outlined"
                      sx={{
                        position: 'absolute',
                        top: 8,
                        right: 8,
                        color: '#fff',
                        borderColor: 'rgba(255,255,255,.3)',
                        textTransform: 'none',
                        fontSize: '.7rem',
                      }}
                      onClick={() =>
                        copyToClipboard(
                          `curl -X POST "${window.location.origin}/api/bot/symptom-entries" \\\n  -H "X-API-Key: ${activeBotKey?.keyPrefix || 'mm_bot_YOUR_KEY'}" \\\n  -H "Content-Type: application/json" \\\n  -d '{\n    "symptomName": "Lower back pain",\n    "category": "Pain",\n    "painType": "Sharp, Throbbing",\n    "bodyLocation": "Lower Back (LWS)",\n    "severity": 6,\n    "durationMinutes": 45,\n    "triggers": "Lifting heavy boxes",\n    "relievers": "Heat pack",\n    "medicationsTaken": "Ibuprofen 400mg",\n    "notes": "Felt a sudden pull while bending down"\n  }'`
                        )
                      }
                    >
                      Copy cURL
                    </Button>
                    <pre style={{ margin: 0 }}>
{`curl -X POST "${window.location.origin}/api/bot/symptom-entries" \\
  -H "X-API-Key: ${activeBotKey?.keyPrefix || 'mm_bot_YOUR_KEY'}" \\
  -H "Content-Type: application/json" \\
  -d '{
    "symptomName": "Lower back pain",
    "category": "Pain",
    "painType": "Sharp, Throbbing",
    "bodyLocation": "Lower Back (LWS)",
    "severity": 6,
    "durationMinutes": 45,
    "triggers": "Lifting heavy boxes",
    "relievers": "Heat pack",
    "medicationsTaken": "Ibuprofen 400mg",
    "notes": "Felt a sudden pull while bending down"
  }'`}
                    </pre>
                  </Box>

                  <Typography variant="subtitle2" sx={{ fontWeight: 700, mt: 2 }}>
                    {t('diary.bot.howItWorks')}
                  </Typography>
                  <Stack spacing={1} sx={{ color: 'text.secondary', fontSize: '.875rem' }}>
                    <Typography variant="body2">{t('diary.bot.step1')}</Typography>
                    <Typography variant="body2">{t('diary.bot.step2')}</Typography>
                    <Typography variant="body2">{t('diary.bot.step3')}</Typography>
                    <Typography variant="body2">{t('diary.bot.step4')}</Typography>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          </Stack>
        )}

        {/* ========================================================================= */}
        {/* LOG / EDIT SYMPTOM DIALOG */}
        {/* ========================================================================= */}
        <Dialog
          open={dialogOpen}
          onClose={() => setDialogOpen(false)}
          maxWidth="md"
          fullWidth
          PaperProps={{ sx: { borderRadius: 3, p: 1 } }}
        >
          <DialogTitle sx={{ fontWeight: 800 }}>
            {editingEntryId ? t('diary.dialog.editTitle') : t('diary.dialog.addTitle')}
          </DialogTitle>
          <DialogContent dividers>
            <Stack spacing={2.5} sx={{ pt: 1 }}>
              {/* Category & Recorded Time */}
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <TextField
                    select
                    label={t('diary.dialog.category')}
                    value={entryCategory}
                    onChange={(e) => setEntryCategory(e.target.value)}
                    fullWidth
                  >
                    {CATEGORIES.map((cat) => (
                      <MenuItem key={cat} value={cat}>
                        {t(`diary.categories.${cat}`, cat)}
                      </MenuItem>
                    ))}
                  </TextField>
                </Grid>

                <Grid item xs={12} sm={6}>
                  <TextField
                    type="time"
                    label={t('diary.dialog.recordedAt')}
                    value={entryRecordedTime}
                    onChange={(e) => setEntryRecordedTime(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                    fullWidth
                  />
                </Grid>
              </Grid>

              {/* Symptom Name */}
              <TextField
                label={t('diary.dialog.symptomName')}
                placeholder={t('diary.dialog.symptomNamePlaceholder')}
                value={entrySymptomName}
                onChange={(e) => setEntrySymptomName(e.target.value)}
                required
                fullWidth
              />

              {/* Pain Severity Slider */}
              <Box sx={{ px: 1, py: 0.5 }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                    {t('diary.dialog.severity')}
                  </Typography>
                  <Chip
                    label={`${entrySeverity} / 10`}
                    sx={{
                      bgcolor: getSeverityColor(entrySeverity),
                      color: '#fff',
                      fontWeight: 800,
                      fontSize: '.85rem',
                    }}
                  />
                </Stack>
                <Slider
                  value={entrySeverity}
                  min={0}
                  max={10}
                  step={1}
                  marks
                  onChange={(_, val) => setEntrySeverity(val as number)}
                  sx={{
                    mt: 1,
                    color: getSeverityColor(entrySeverity),
                    '& .MuiSlider-thumb': { height: 24, width: 24 },
                  }}
                />
                <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                  {t('diary.dialog.severityDescription')}
                </Typography>
              </Box>

              {/* Pain Types Chips */}
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.75 }}>
                  {t('diary.dialog.painTypes')}
                </Typography>
                <Stack direction="row" spacing={0.75} flexWrap="wrap" sx={{ gap: 0.75, mb: 1 }}>
                  {PAIN_TYPE_SUGGESTIONS.map((type) => {
                    const isSelected = entryPainTypes.includes(type);
                    return (
                      <Chip
                        key={type}
                        label={type}
                        clickable
                        onClick={() => togglePainType(type)}
                        color={isSelected ? 'primary' : 'default'}
                        variant={isSelected ? 'filled' : 'outlined'}
                        size="small"
                        sx={{ fontWeight: isSelected ? 700 : 500 }}
                      />
                    );
                  })}
                </Stack>
                <TextField
                  size="small"
                  label="Other / Custom Pain Qualities"
                  placeholder="e.g. Electric shocks, pulsing"
                  value={entryCustomPainType}
                  onChange={(e) => setEntryCustomPainType(e.target.value)}
                  fullWidth
                />
              </Box>

              {/* Body Location Chips */}
              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.75 }}>
                  {t('diary.dialog.location')}
                </Typography>
                <Stack direction="row" spacing={0.75} flexWrap="wrap" sx={{ gap: 0.75, mb: 1 }}>
                  {BODY_LOCATION_SUGGESTIONS.map((loc) => {
                    const isSelected = entryLocations.includes(loc);
                    return (
                      <Chip
                        key={loc}
                        label={loc}
                        clickable
                        onClick={() => toggleLocation(loc)}
                        color={isSelected ? 'primary' : 'default'}
                        variant={isSelected ? 'filled' : 'outlined'}
                        size="small"
                        sx={{ fontWeight: isSelected ? 700 : 500 }}
                      />
                    );
                  })}
                </Stack>
                <TextField
                  size="small"
                  label="Other / Specific Body Area"
                  placeholder="e.g. Left sacrum, behind right eye"
                  value={entryCustomLocation}
                  onChange={(e) => setEntryCustomLocation(e.target.value)}
                  fullWidth
                />
              </Box>

              {/* Duration in Minutes */}
              <TextField
                type="number"
                label={t('diary.dialog.durationMinutes')}
                placeholder="e.g. 30"
                value={entryDurationMinutes}
                onChange={(e) => setEntryDurationMinutes(e.target.value)}
                InputProps={{
                  endAdornment: <InputAdornment position="end">min</InputAdornment>,
                }}
                fullWidth
              />

              {/* Triggers & Relievers */}
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <TextField
                    label={t('diary.dialog.triggers')}
                    placeholder={t('diary.dialog.triggersPlaceholder')}
                    value={entryTriggers}
                    onChange={(e) => setEntryTriggers(e.target.value)}
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    label={t('diary.dialog.relievers')}
                    placeholder={t('diary.dialog.relieversPlaceholder')}
                    value={entryRelievers}
                    onChange={(e) => setEntryRelievers(e.target.value)}
                    fullWidth
                  />
                </Grid>
              </Grid>

              {/* Medications Taken */}
              <TextField
                label={t('diary.dialog.medications')}
                placeholder={t('diary.dialog.medicationsPlaceholder')}
                value={entryMedications}
                onChange={(e) => setEntryMedications(e.target.value)}
                fullWidth
              />

              {/* Notes */}
              <TextField
                label={t('diary.dialog.notes')}
                placeholder={t('diary.dialog.notesPlaceholder')}
                value={entryNotes}
                onChange={(e) => setEntryNotes(e.target.value)}
                multiline
                rows={2}
                fullWidth
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setDialogOpen(false)} sx={{ textTransform: 'none' }}>
              {t('common.cancel')}
            </Button>
            <Button
              variant="contained"
              onClick={() => saveEntryMutation.mutate()}
              disabled={!entrySymptomName.trim() || saveEntryMutation.isPending}
              sx={{ borderRadius: '9999px', px: 3, fontWeight: 700, textTransform: 'none' }}
            >
              {saveEntryMutation.isPending ? t('diary.dialog.saving') : t('diary.dialog.save')}
            </Button>
          </DialogActions>
        </Dialog>

        {/* ========================================================================= */}
        {/* NEW BOT API KEY MODAL */}
        {/* ========================================================================= */}
        <Dialog
          open={newKeyModalOpen}
          onClose={() => setNewKeyModalOpen(false)}
          maxWidth="sm"
          fullWidth
          PaperProps={{ sx: { borderRadius: 3, p: 1 } }}
        >
          <DialogTitle sx={{ fontWeight: 800, color: '#00695c' }}>
            {t('diary.bot.newKeyModalTitle')}
          </DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <Alert severity="warning" sx={{ fontWeight: 600 }}>
                {t('diary.bot.newKeyWarning')}
              </Alert>

              <Box
                sx={{
                  bgcolor: '#111b1b',
                  color: '#64d8cb',
                  p: 2,
                  borderRadius: 2,
                  fontFamily: 'monospace',
                  fontSize: '.95rem',
                  wordBreak: 'break-all',
                }}
              >
                {generatedKey}
              </Box>

              <Button
                variant="contained"
                onClick={() => copyToClipboard(generatedKey)}
                sx={{ borderRadius: '9999px', textTransform: 'none', fontWeight: 700 }}
              >
                {t('diary.bot.copyKey')}
              </Button>
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setNewKeyModalOpen(false)} sx={{ textTransform: 'none' }}>
              Close
            </Button>
          </DialogActions>
        </Dialog>

        {/* Notification Toast */}
        <Snackbar
          open={Boolean(notice)}
          autoHideDuration={4000}
          onClose={() => setNotice('')}
          message={notice}
        />
      </Stack>
    </Container>
  );
}

