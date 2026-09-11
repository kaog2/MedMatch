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

export default function PeopleSearch() {
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
      setNotice('Connection request sent.');
    },
    onError: () => setNotice('This person is no longer accepting connection requests.'),
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
        <Alert severity="info">People search is available to patient accounts who want to connect with other patients.</Alert>
      </Container>
    );
  }

  return (
    <Container>
      <Stack spacing={4} sx={{ py: { xs: 4, md: 7 } }}>
        <Box>
          <Typography sx={{ color: '#b06f42', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '.12em', fontSize: '.75rem' }}>
            Patient connections
          </Typography>
          <Typography variant="h2" sx={{ fontWeight: 800, letterSpacing: '-.055em', mt: 1 }}>
            Find people who understand.
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 2, maxWidth: 650 }}>
            Search for shared experiences by diagnosis tags and reach out only when both sides have opted in.
          </Typography>
        </Box>

        <Alert severity="info">
          Only members who enabled both patient contact and profile search appear here. We never show private email addresses.
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
                      <Typography color="text.secondary">{[person.city, person.country].filter(Boolean).join(', ') || 'Location not shared'}</Typography>
                    </Box>

                    <Typography variant="body2">{person.bio || 'This member has chosen to share experiences with the community.'}</Typography>

                    {person.diagnoses.length > 0 && (
                      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                        {person.diagnoses.map((diag) => (
                          <Chip
                            key={diag}
                            label={diag}
                            size="small"
                            sx={{ bgcolor: 'rgba(0,105,92,.12)', color: '#004d40', fontWeight: 600 }}
                          />
                        ))}
                      </Stack>
                    )}

                    {person.symptoms.length > 0 && (
                      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                        {person.symptoms.map((s) => (
                          <Chip key={s} label={s} size="small" variant="outlined" />
                        ))}
                      </Stack>
                    )}

                    <Box sx={{ flexGrow: 1 }} />
                    <Button
                      variant="contained"
                      onClick={() => setSelected(person)}
                      sx={{ alignSelf: 'flex-start', bgcolor: '#102a2b', '&:hover': { bgcolor: '#1d4647' } }}
                    >
                      Request connection
                    </Button>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {query.data?.length === 0 && <Typography>No matching members found. Try a broader search.</Typography>}

        <Dialog open={!!selected} onClose={() => setSelected(null)} maxWidth="sm" fullWidth>
          <DialogTitle>Request a connection?</DialogTitle>
          <DialogContent>
            <Typography color="text.secondary" sx={{ mb: 2 }}>
              Send an invitation to {selected?.displayName}. They can decide whether to accept and message back.
            </Typography>
            <TextField
              fullWidth
              multiline
              rows={3}
              label="Introduction note (optional)"
              placeholder="Mention shared health conditions or what you hope to exchange..."
              value={message}
              onChange={(e) => setMessage(e.target.value)}
            />
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setSelected(null)}>Cancel</Button>
            <Button
              variant="contained"
              onClick={() => selected && request.mutate({ person: selected, msg: message })}
              disabled={request.isPending}
            >
              Send request
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
        <Typography variant="h6" sx={{ fontWeight: 800 }}>Search shared experiences</Typography>
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
                        label={`${option.usageCount} ${option.usageCount === 1 ? 'member' : 'members'}`}
                        size="small"
                        variant="outlined"
                        sx={{ fontSize: '.68rem', height: 20 }}
                      />
                    )}
                  </Box>
                </li>
              )}
              renderInput={(params) => (
                <TextField {...params} fullWidth label="Diagnosis tag" placeholder="Type or pick a diagnosis..." />
              )}
            />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField fullWidth label="Symptom" value={symptom} onChange={(e) => setSymptom(e.target.value)} />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField fullWidth label="City" value={city} onChange={(e) => setCity(e.target.value)} />
          </Grid>
        </Grid>
        <Button
          variant="contained"
          onClick={search}
          sx={{ alignSelf: 'flex-end', bgcolor: '#102a2b', '&:hover': { bgcolor: '#1d4647' } }}
        >
          Search people
        </Button>
      </Stack>
    </Paper>
  );
}
