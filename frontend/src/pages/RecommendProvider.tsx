import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { api, Clinic, CreateRecommendation, DiagnosisTag, Recommendation } from '../services/api';
import { useAuthStore, hasRole } from '../store';
import { Loading } from '../components/PageState';

export default function RecommendProvider() {
  const roles = useAuthStore((s) => s.roles);
  const providers = useQuery({ queryKey: ['clinics'], queryFn: () => api<Clinic[]>('/clinics') });
  const [searchParams] = useSearchParams();
  const prefillClinicId = searchParams.get('clinicId');

  const [diagnosisInput, setDiagnosisInput] = useState('');
  const [selectedDiagnoses, setSelectedDiagnoses] = useState<string[]>([]);
  const [selectedProviders, setSelectedProviders] = useState<Clinic[]>([]);
  const [details, setDetails] = useState('');
  const [error, setError] = useState('');
  const [result, setResult] = useState<Recommendation | null>(null);

  useEffect(() => {
    if (prefillClinicId && providers.data) {
      const match = providers.data.find((p) => p.id === prefillClinicId);
      if (match) {
        setSelectedProviders((current) => (current.some((p) => p.id === match.id) ? current : [...current, match]));
      }
    }
  }, [prefillClinicId, providers.data]);

  const suggestions = useQuery({
    queryKey: ['diagnosis-tags', diagnosisInput],
    queryFn: () =>
      api<DiagnosisTag[]>(
        `/diagnosis-tags/suggest${diagnosisInput.trim() ? `?q=${encodeURIComponent(diagnosisInput.trim())}` : ''}`
      ),
  });

  const submit = useMutation({
    mutationFn: (payload: CreateRecommendation) => api<Recommendation>('/recommendations', { method: 'POST', body: JSON.stringify(payload) }),
    onSuccess: (created) => {
      setResult(created);
      setSelectedDiagnoses([]);
      setSelectedProviders([]);
      setDetails('');
    },
    onError: (e) => setError(e instanceof Error ? e.message : 'Unable to publish your recommendation.'),
  });

  const addDiagnosis = (raw: string) => {
    const name = raw.trim();
    if (!name) return;
    setSelectedDiagnoses((prev) =>
      prev.some((d) => d.toLowerCase() === name.toLowerCase()) ? prev : [...prev, name]
    );
    setDiagnosisInput('');
  };

  if (!hasRole(roles, 'Patient')) {
    return (
      <Paper sx={{ p: 3 }}>
        <Alert severity="warning">Please sign in with a patient account to recommend a care provider.</Alert>
      </Paper>
    );
  }

  if (providers.isLoading) return <Loading />;

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (selectedProviders.length === 0) return setError('Select at least one care provider.');
    if (selectedDiagnoses.length === 0) return setError('Select at least one diagnosis or symptom.');
    if (details.trim().length < 10) return setError('Please describe how the provider helped you (at least 10 characters).');
    submit.mutate({
      clinicIds: selectedProviders.map((p) => p.id),
      diagnoses: selectedDiagnoses,
      details: details.trim(),
    });
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>Recommend a care provider</Typography>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        Tell others which care providers helped with your diagnosis or symptoms. Only positive, constructive
        experiences are published.
      </Typography>

      <Box component="form" onSubmit={handleSubmit}>
        <Stack spacing={2}>
          {error && <Alert severity="error">{error}</Alert>}
          {result && (
            <Alert severity={result.status === 'Approved' ? 'success' : result.status === 'Rejected' ? 'error' : 'info'}>
              {result.status === 'Approved' && 'Thank you! Your recommendation has been published.'}
              {result.status === 'Rejected' && `Your recommendation was not published${result.moderationNote ? `: ${result.moderationNote}` : '.'}`}
              {result.status === 'Pending' && 'Your recommendation is awaiting review before it is published.'}
            </Alert>
          )}

          <Autocomplete
            multiple
            freeSolo
            options={suggestions.data ?? []}
            value={selectedDiagnoses}
            inputValue={diagnosisInput}
            onInputChange={(_, value) => setDiagnosisInput(value)}
            getOptionLabel={(option) => (typeof option === 'string' ? option : option.name)}
            isOptionEqualToValue={(option, value) => {
              const optName = typeof option === 'string' ? option : option.name;
              const valName = typeof value === 'string' ? value : value.name;
              return optName.trim().toLowerCase() === valName.trim().toLowerCase();
            }}
            onChange={(_, values) => {
              const unique = new Map<string, string>();
              values.forEach((v) => {
                const name = typeof v === 'string' ? v.trim() : v.name.trim();
                if (name && !unique.has(name.toLowerCase())) unique.set(name.toLowerCase(), name);
              });
              setSelectedDiagnoses(Array.from(unique.values()));
            }}
            renderTags={(value, getTagProps) =>
              value.map((option, index) => {
                const { key, ...tagProps } = getTagProps({ index });
                return (
                  <Chip
                    key={key}
                    label={typeof option === 'string' ? option : option.name}
                    size="small"
                    sx={(theme) => ({
                      bgcolor: theme.palette.mode === 'dark' ? 'rgba(77,182,172,.18)' : 'rgba(0,105,92,.12)',
                      color: theme.palette.mode === 'dark' ? '#9ce8dd' : '#004d40',
                      fontWeight: 600,
                    })}
                    {...tagProps}
                  />
                );
              })
            }
            renderInput={(params) => {
              const original = params.inputProps.onKeyDown as any;
              return (
                <TextField
                  {...params}
                  inputProps={{
                    ...params.inputProps,
                    onKeyDown: (e) => {
                      if (e.key === 'Enter' && diagnosisInput.trim()) {
                        e.preventDefault();
                        addDiagnosis(diagnosisInput);
                        return;
                      }
                      original?.(e);
                    },
                  }}
                  label="Diagnosis or symptom"
                  placeholder="e.g. Morbus Perthes, Lower Back Pain"
                  helperText="What this care provider helped you with."
                />
              );
            }}
          />

          <Autocomplete
            multiple
            options={providers.data ?? []}
            value={selectedProviders}
            getOptionLabel={(option) => option.name}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            onChange={(_, values) => setSelectedProviders(values)}
            renderOption={(props, option) => (
              <li {...props} key={option.id}>
                <Box sx={{ display: 'flex', flexDirection: 'column' }}>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>{option.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {option.type.replace(/([A-Z])/g, ' $1').trim()} · {option.city}, {option.country}
                  </Typography>
                </Box>
              </li>
            )}
            renderInput={(params) => <TextField {...params} label="Care providers that helped" />}
          />

          <TextField
            label="What helped you"
            multiline
            minRows={5}
            required
            value={details}
            onChange={(e) => setDetails(e.target.value)}
            helperText={`Positive details only (${details.trim().length}/2000).`}
            inputProps={{ maxLength: 2000 }}
          />

          <Stack direction="row" spacing={2}>
            <Button type="submit" variant="contained" disabled={submit.isPending}>
              {submit.isPending ? 'Submitting…' : 'Publish recommendation'}
            </Button>
            <Button component={Link} to="/clinics">Browse care providers</Button>
          </Stack>
        </Stack>
      </Box>
    </Paper>
  );
}
