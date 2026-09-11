import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  Paper,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { api, DiagnosisTag, Person } from '../services/api';
import { useAuthStore, hasRole } from '../store';
import { ErrorState, Loading } from '../components/PageState';
import { useTranslation } from 'react-i18next';

export default function PeopleSearch() {
  const { t } = useTranslation();
  const roles = useAuthStore((state) => state.roles);
  const [diagnosis, setDiagnosis] = useState('');
  const [symptom, setSymptom] = useState('');
  const [city, setCity] = useState('');
  const [filters, setFilters] = useState('');
  const [selected, setSelected] = useState<Person | null>(null);
  const [message, setMessage] = useState('');
  const [notice, setNotice] = useState('');

  const query = useQuery({
    queryKey: ['people', filters],
    queryFn: () => api<Person[]>(`/people?${filters}`),
    enabled: hasRole(roles, 'Patient'),
  });

  const request = useMutation({
    mutationFn: ({ person, msg }: { person: Person; msg: string }) =>
      api(`/people/${person.userId}/connection-requests`, {
        method: 'POST',
        body: JSON.stringify({ message: msg }),
      }),
    onSuccess: () => {
      setSelected(null);
      setMessage('');
      setNotice(t('people.sent'));
    },
    onError: () => setNotice(t('people.notAccepting')),
  });

  const search = () => {
    const params = new URLSearchParams();
    if (diagnosis) params.set('diagnosis', diagnosis);
    if (symptom) params.set('symptom', symptom);
    if (city) params.set('city', city);
    setFilters(params.toString());
  };

  if (!hasRole(roles, 'Patient')) {
    return (
      <Container sx={{ py: 6 }}>
        <Alert severity="info">{t('people.notPatient')}</Alert>
      </Container>
    );
  }

  return (
    <Container>
      <Stack spacing={4} sx={{ py: { xs: 4, md: 7 } }}>
        <Box>
          <Typography sx={{ color: '#b06f42', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '.12em', fontSize: '.75rem' }}>
            {t('people.eyebrow')}
          </Typography>
          <Typography variant="h2" sx={{ fontWeight: 800, letterSpacing: '-.055em', mt: 1 }}>
            {t('people.title')}
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 2, maxWidth: 650 }}>
            {t('people.subtitle')}
          </Typography>
        </Box>

        <Alert severity="info">
          {t('people.privacy')}
        </Alert>

        <PaperSearch
          diagnosis={diagnosis}
          symptom={symptom}
          city={city}
          setDiagnosis={setDiagnosis}
          setSymptom={setSymptom}
          setCity={setCity}
          search={search}
        />

        {query.isLoading && <Loading />}
        {query.error && <ErrorState error={query.error} />}

        <Grid container spacing={2}>
          {query.data?.map((person) => (
            <Grid item xs={12} md={6} key={person.userId}>
              <Card elevation={0} sx={{ height: '100%', border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
                <CardContent sx={{ p: 3, height: '100%' }}>
                  <Stack spacing={2} height="100%">
                    <Box>
                      <Typography variant="h6" sx={{ fontWeight: 800 }}>{person.displayName}</Typography>
                      <Typography color="text.secondary">{[person.city, person.country].filter(Boolean).join(', ') || t('people.noLocation')}</Typography>
                    </Box>

                    <Typography variant="body2">{person.bio || t('people.noBio')}</Typography>

                    {person.diagnoses.length > 0 && (
                      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                        {person.diagnoses.map((diag) => (
                          <Chip
                            key={diag}
                            label={diag}
                            size="small"
                            sx={(theme) => ({
                              bgcolor: theme.palette.mode === 'dark' ? 'rgba(77,182,172,.18)' : 'rgba(0,105,92,.12)',
                              color: theme.palette.mode === 'dark' ? '#9ce8dd' : '#004d40',
                              fontWeight: 600,
                            })}
                          />
                        ))}
                      </Stack>
                    )}

                    {person.symptoms && (
                      <Typography variant="body2" color="text.secondary">{t('people.symptoms', { symptoms: person.symptoms })}</Typography>
                    )}

                    <Box sx={{ flexGrow: 1 }} />
                    <Button
                      variant="contained"
                      onClick={() => setSelected(person)}
                      sx={{ alignSelf: 'flex-start' }}
                    >
                      {t('people.request')}
                    </Button>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {query.data?.length === 0 && <Typography>{t('people.empty')}</Typography>}

        <Dialog open={!!selected} onClose={() => setSelected(null)} maxWidth="sm" fullWidth>
          <DialogTitle>{t('people.requestTitle')}</DialogTitle>
          <DialogContent>
            <Typography color="text.secondary" sx={{ mb: 2 }}>
              {t('people.requestBody', { name: selected?.displayName })}
            </Typography>
            <TextField
              fullWidth
              multiline
              rows={3}
              label={t('people.note')}
              placeholder={t('people.notePlaceholder')}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
            />
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setSelected(null)}>{t('common.cancel')}</Button>
            <Button
              variant="contained"
              onClick={() => selected && request.mutate({ person: selected, msg: message })}
              disabled={request.isPending}
            >
              {t('people.send')}
            </Button>
          </DialogActions>
        </Dialog>

        <Snackbar open={!!notice} autoHideDuration={4000} onClose={() => setNotice('')} message={notice} />
      </Stack>
    </Container>
  );
}

function PaperSearch({
  diagnosis,
  symptom,
  city,
  setDiagnosis,
  setSymptom,
  setCity,
  search,
}: {
  diagnosis: string;
  symptom: string;
  city: string;
  setDiagnosis: (value: string) => void;
  setSymptom: (value: string) => void;
  setCity: (value: string) => void;
  search: () => void;
}) {
  const { t } = useTranslation();
  const [inputVal, setInputVal] = useState(diagnosis);

  const tagSuggestions = useQuery({
    queryKey: ['diagnosis-tags-search', inputVal],
    queryFn: () =>
      api<DiagnosisTag[]>(
        `/diagnosis-tags/suggest${inputVal.trim() ? `?q=${encodeURIComponent(inputVal.trim())}` : ''}`
      ),
  });

  return (
    <Paper elevation={0} sx={{ p: { xs: 2, md: 3 }, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
      <Stack spacing={2}>
        <Typography variant="h6" sx={{ fontWeight: 800 }}>{t('people.searchTitle')}</Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} md={4}>
            <Autocomplete
              freeSolo
              options={tagSuggestions.data ?? []}
              inputValue={inputVal}
              onInputChange={(_, val) => {
                setInputVal(val);
                setDiagnosis(val);
              }}
              value={diagnosis}
              onChange={(_, val) => {
                const text = typeof val === 'string' ? val : val?.name ?? '';
                setDiagnosis(text);
                setInputVal(text);
              }}
              getOptionLabel={(option) => (typeof option === 'string' ? option : option.name)}
              renderOption={(props, option) => (
                <li {...props} key={typeof option === 'string' ? option : option.id}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
                    <Typography variant="body2">{typeof option === 'string' ? option : option.name}</Typography>
                    {typeof option !== 'string' && option.usageCount > 0 && (
                      <Chip
                        label={`${option.usageCount} ${t('common.members', { count: option.usageCount })}`}
                        size="small"
                        variant="outlined"
                        sx={{ fontSize: '.68rem', height: 20 }}
                      />
                    )}
                  </Box>
                </li>
              )}
              renderInput={(params) => (
                <TextField {...params} fullWidth label={t('people.diagnosisTag')} placeholder={t('people.diagnosisPlaceholder')} />
              )}
            />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField fullWidth label={t('common.symptom')} value={symptom} onChange={(e) => setSymptom(e.target.value)} />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField fullWidth label={t('common.city')} value={city} onChange={(e) => setCity(e.target.value)} />
          </Grid>
        </Grid>
        <Button
          variant="contained"
          onClick={search}
          sx={{ alignSelf: 'flex-end' }}
        >
          {t('people.searchPeople')}
        </Button>
      </Stack>
    </Paper>
  );
}
