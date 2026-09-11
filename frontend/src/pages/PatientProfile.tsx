import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  FormControlLabel,
  MenuItem,
  Paper,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, Consent, DiagnosisTag, Profile } from '../services/api';
import { ErrorState, Loading } from '../components/PageState';

const empty: Profile = { displayMode: 'Anonymous', diagnoses: [], interventions: [], symptoms: '', languages: [] };

export default function PatientProfile() {
  const client = useQueryClient();
  const profile = useQuery({ queryKey: ['profile'], queryFn: () => api<Profile>('/profile') });
  const consent = useQuery({ queryKey: ['consent'], queryFn: () => api<Consent>('/consent') });
  const [form, setForm] = useState<Profile>(empty);
  const [privacy, setPrivacy] = useState<Consent | null>(null);
  const [diagnosisInput, setDiagnosisInput] = useState('');

  const suggestions = useQuery({
    queryKey: ['diagnosis-tags', diagnosisInput],
    queryFn: () =>
      api<DiagnosisTag[]>(
        `/diagnosis-tags/suggest${diagnosisInput.trim() ? `?q=${encodeURIComponent(diagnosisInput.trim())}` : ''}`
      ),
  });

  useEffect(() => { if (profile.data) setForm(profile.data); }, [profile.data]);
  useEffect(() => { if (consent.data) setPrivacy(consent.data); }, [consent.data]);

  const mutation = useMutation({
    mutationFn: async () => {
      await api('/profile', { method: 'PUT', body: JSON.stringify(form) });
      return api<Consent>('/consent', { method: 'PUT', body: JSON.stringify(privacy) });
    },
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['profile'] });
      client.invalidateQueries({ queryKey: ['matches'] });
      client.invalidateQueries({ queryKey: ['match-notifications'] });
      client.invalidateQueries({ queryKey: ['match-summary'] });
      client.invalidateQueries({ queryKey: ['diagnosis-tags'] });
    },
  });

  if (profile.isLoading || consent.isLoading) return <Loading />;
  if (profile.error || consent.error) return <ErrorState error={profile.error ?? consent.error} />;

  const update = (field: keyof Profile, value: string) => setForm({ ...form, [field]: value });
  const addDiagnosis = (raw: string) => {
    const name = raw.trim();
    if (!name) return;
    setForm((prev) => ({
      ...prev,
      diagnoses: prev.diagnoses.some((d) => d.toLowerCase() === name.toLowerCase())
        ? prev.diagnoses
        : [...prev.diagnoses, name],
    }));
    setDiagnosisInput('');
  };

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>Your profile and privacy</Typography>
      <Stack spacing={2}>
        <TextField select label="How you appear" value={form.displayMode} onChange={(e) => setForm({ ...form, displayMode: e.target.value as Profile['displayMode'] })}>
          <MenuItem value="Anonymous">Anonymous</MenuItem>
          <MenuItem value="Pseudonym">Pseudonym</MenuItem>
          <MenuItem value="RealName">Real name</MenuItem>
        </TextField>
        {form.displayMode === 'Pseudonym' && <TextField label="Pseudonym" value={form.pseudonym ?? ''} onChange={(e) => update('pseudonym', e.target.value)} />}
        {form.displayMode === 'RealName' && <TextField label="Real name" value={form.realName ?? ''} onChange={(e) => update('realName', e.target.value)} />}
        <TextField label="City" value={form.city ?? ''} onChange={(e) => update('city', e.target.value)} />
        <TextField label="Country" value={form.country ?? ''} onChange={(e) => update('country', e.target.value)} />

        <Autocomplete
          multiple
          freeSolo
          options={suggestions.data ?? []}
          value={form.diagnoses}
          inputValue={diagnosisInput}
          onInputChange={(_, value) => setDiagnosisInput(value)}
          getOptionLabel={(option) => typeof option === 'string' ? option : option.name}
          isOptionEqualToValue={(option, value) => {
            const optName = typeof option === 'string' ? option : option.name;
            const valName = typeof value === 'string' ? value : value.name;
            return optName.trim().toLowerCase() === valName.trim().toLowerCase();
          }}
          onChange={(_, values) => {
            const unique = new Map<string, string>();
            values.forEach((v) => {
              const name = typeof v === 'string' ? v.trim() : v.name.trim();
              if (name && !unique.has(name.toLowerCase())) {
                unique.set(name.toLowerCase(), name);
              }
            });
            setForm({ ...form, diagnoses: Array.from(unique.values()) });
          }}
          renderTags={(value, getTagProps) =>
            value.map((option, index) => {
              const { key, ...tagProps } = getTagProps({ index });
              return (
                <Chip
                  key={key}
                  label={typeof option === 'string' ? option : option.name}
                  size="small"
                  sx={{ bgcolor: 'rgba(0,105,92,.12)', color: '#004d40', fontWeight: 600 }}
                  {...tagProps}
                />
              );
            })
          }
          renderOption={(props, option) => (
            <li {...props} key={typeof option === 'string' ? option : option.id}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
                <Typography variant="body2" sx={{ fontWeight: 500 }}>
                  {typeof option === 'string' ? option : option.name}
                </Typography>
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
                label="Diagnoses tags"
                placeholder="Search or add diagnoses..."
                helperText="Add standard tags or type custom conditions. Press Enter or comma to add a tag."
              />
            );
          }}
        />

        <TextField label="Symptoms" multiline minRows={3} value={form.symptoms} onChange={(e) => setForm({ ...form, symptoms: e.target.value })} helperText="Describe your symptoms in your own words — spaces and full sentences are fine." />

        {privacy && (
          <>
            <Typography variant="h6" sx={{ mt: 1 }}>Consent preferences</Typography>
            <FormControlLabel control={<Switch checked={privacy.showProfilePublicly} onChange={(e) => setPrivacy({ ...privacy, showProfilePublicly: e.target.checked })} />} label="Show my profile publicly" />
            <FormControlLabel control={<Switch checked={privacy.patientsContactMe} onChange={(e) => setPrivacy({ ...privacy, patientsContactMe: e.target.checked })} />} label="Other patients can contact me" />
            <FormControlLabel control={<Switch checked={privacy.clinicsContactMe} onChange={(e) => setPrivacy({ ...privacy, clinicsContactMe: e.target.checked })} />} label="Clinics can contact me" />
            <FormControlLabel control={<Switch checked={privacy.dataForSearch} onChange={(e) => setPrivacy({ ...privacy, dataForSearch: e.target.checked })} />} label="Allow my health profile in contact search and peer matching" />
          </>
        )}

        {mutation.error && <Alert severity="error">{mutation.error.message}</Alert>}
        {mutation.isSuccess && (
          <Alert
            severity="success"
            action={
              <Button color="inherit" size="small" component={Link} to="/matches">
                View Matches
              </Button>
            }
          >
            Profile saved! Your diagnosis tags are synchronized and peer matches have been updated.
          </Alert>
        )}
        <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? 'Saving...' : 'Save changes'}
        </Button>
      </Stack>
    </Paper>
  );
}

export { PatientProfile };

