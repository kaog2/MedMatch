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
import { useTranslation } from 'react-i18next';

const empty: Profile = { displayMode: 'Anonymous', diagnoses: [], interventions: [], symptoms: '', languages: [] };

export default function PatientProfile() {
  const { t } = useTranslation();
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

  const displayTag = (name: string) =>
    suggestions.data?.find((t) => t.name.toLowerCase() === name.toLowerCase())?.localizedName ?? name;

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
      <Typography variant="h4" gutterBottom>{t('profile.title')}</Typography>
      <Stack spacing={2}>
        <TextField select label={t('profile.appearAs')} value={form.displayMode} onChange={(e) => setForm({ ...form, displayMode: e.target.value as Profile['displayMode'] })}>
          <MenuItem value="Anonymous">{t('profile.anonymous')}</MenuItem>
          <MenuItem value="Pseudonym">{t('profile.pseudonym')}</MenuItem>
          <MenuItem value="RealName">{t('profile.realName')}</MenuItem>
        </TextField>
        {form.displayMode === 'Pseudonym' && <TextField label={t('profile.pseudonym')} value={form.pseudonym ?? ''} onChange={(e) => update('pseudonym', e.target.value)} />}
        {form.displayMode === 'RealName' && <TextField label={t('profile.realName')} value={form.realName ?? ''} onChange={(e) => update('realName', e.target.value)} />}
        <TextField label={t('common.city')} value={form.city ?? ''} onChange={(e) => update('city', e.target.value)} />
        <TextField label={t('common.country')} value={form.country ?? ''} onChange={(e) => update('country', e.target.value)} />

        <Autocomplete
          multiple
          freeSolo
          options={suggestions.data ?? []}
          value={form.diagnoses}
          inputValue={diagnosisInput}
          onInputChange={(_, value) => setDiagnosisInput(value)}
          getOptionLabel={(option) => typeof option === 'string' ? displayTag(option) : (option.localizedName ?? option.name)}
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
                  label={typeof option === 'string' ? displayTag(option) : (option.localizedName ?? option.name)}
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
          renderOption={(props, option) => (
            <li {...props} key={typeof option === 'string' ? option : option.id}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
                <Typography variant="body2" sx={{ fontWeight: 500 }}>
                  {typeof option === 'string' ? displayTag(option) : (option.localizedName ?? option.name)}
                </Typography>
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
                label={t('profile.diagnosisTags')}
                placeholder={t('profile.diagnosisPlaceholder')}
                helperText={t('profile.diagnosisHelper')}
              />
            );
          }}
        />

        <TextField label={t('profile.symptoms')} multiline minRows={3} value={form.symptoms} onChange={(e) => setForm({ ...form, symptoms: e.target.value })} helperText={t('profile.symptomsHelper')} />

        {privacy && (
          <>
            <Typography variant="h6" sx={{ mt: 1 }}>{t('profile.consentTitle')}</Typography>
            <FormControlLabel control={<Switch checked={privacy.showProfilePublicly} onChange={(e) => setPrivacy({ ...privacy, showProfilePublicly: e.target.checked })} />} label={t('profile.showPublic')} />
            <FormControlLabel control={<Switch checked={privacy.patientsContactMe} onChange={(e) => setPrivacy({ ...privacy, patientsContactMe: e.target.checked })} />} label={t('profile.patientsContact')} />
            <FormControlLabel control={<Switch checked={privacy.clinicsContactMe} onChange={(e) => setPrivacy({ ...privacy, clinicsContactMe: e.target.checked })} />} label={t('profile.clinicsContact')} />
            <FormControlLabel control={<Switch checked={privacy.dataForSearch} onChange={(e) => setPrivacy({ ...privacy, dataForSearch: e.target.checked })} />} label={t('profile.dataSearch')} />
          </>
        )}

        {mutation.error && <Alert severity="error">{mutation.error.message}</Alert>}
        {mutation.isSuccess && (
          <Alert
            severity="success"
            action={
              <Button color="inherit" size="small" component={Link} to="/matches">
                {t('profile.viewMatches')}
              </Button>
            }
          >
            {t('profile.saved')}
          </Alert>
        )}
        <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? t('common.saving') : t('common.save')}
        </Button>
      </Stack>
    </Paper>
  );
}

export { PatientProfile };

