import { Box, Button, Card, CardActionArea, CardContent, Chip, Container, Grid, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api, Clinic } from '../services/api';
import { ErrorState, Loading } from '../components/PageState';

export default function ClinicSearch() {
  const { t } = useTranslation();
  const [specialty, setSpecialty] = useState('');
  const [city, setCity] = useState('');
  const [country, setCountry] = useState('');
  const [tag, setTag] = useState('');
  const [filters, setFilters] = useState('');
  const query = useQuery({ queryKey: ['clinics', filters], queryFn: () => api<Clinic[]>(`/clinics?${filters}`) });
  const directory = useQuery({ queryKey: ['clinic-location-options'], queryFn: () => api<Clinic[]>('/clinics') });
  const cities = [...new Set((directory.data ?? []).map((clinic) => clinic.city.trim()).filter(Boolean))].sort((a, b) => a.localeCompare(b));
  const countries = [...new Set((directory.data ?? []).map((clinic) => clinic.country.trim()).filter(Boolean))].sort((a, b) => a.localeCompare(b));
  const search = () => setFilters(new URLSearchParams(Object.entries({ specialty, city, country, tag }).filter(([, value]) => value.trim())).toString());
  const clear = () => { setSpecialty(''); setCity(''); setCountry(''); setTag(''); setFilters(''); };

  return <Container><Stack spacing={4} sx={{ py: { xs: 4, md: 7 } }}>
    <Box><Typography sx={{ color: '#b06f42', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '.12em', fontSize: '.75rem' }}>{t('clinics.eyebrow')}</Typography><Typography variant="h2" sx={{ fontWeight: 800, letterSpacing: '-.055em', mt: 1 }}>{t('clinics.title')}</Typography><Typography color="text.secondary" sx={{ mt: 2, maxWidth: 650 }}>{t('clinics.subtitle')}</Typography></Box>
    <Paper elevation={0} sx={{ p: { xs: 2, md: 3 }, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}><Stack spacing={2}><Typography variant="h6" sx={{ fontWeight: 800 }}>{t('clinics.searchTitle')}</Typography><Grid container spacing={2}><Grid item xs={12} sm={6} md={3}><TextField fullWidth label={t('clinics.specialty')} placeholder={t('clinics.specialtyPlaceholder')} value={specialty} onChange={(e) => setSpecialty(e.target.value)} /></Grid><Grid item xs={12} sm={6} md={3}><TextField select fullWidth label={t('common.city')} value={city} onChange={(e) => setCity(e.target.value)}><MenuItem value="">{t('clinics.allCities')}</MenuItem>{cities.map((option) => <MenuItem key={option} value={option}>{option}</MenuItem>)}</TextField></Grid><Grid item xs={12} sm={6} md={3}><TextField select fullWidth label={t('common.country')} value={country} onChange={(e) => setCountry(e.target.value)}><MenuItem value="">{t('clinics.allCountries')}</MenuItem>{countries.map((option) => <MenuItem key={option} value={option}>{option}</MenuItem>)}</TextField></Grid><Grid item xs={12} sm={6} md={3}><TextField fullWidth label={t('clinics.treatment')} placeholder={t('clinics.treatmentPlaceholder')} value={tag} onChange={(e) => setTag(e.target.value)} /></Grid></Grid><Stack direction="row" spacing={1} justifyContent="flex-end"><Button onClick={clear} color="inherit">{t('common.clear')}</Button><Button variant="contained" onClick={search}>{t('clinics.search')}</Button></Stack></Stack></Paper>
    {query.isLoading && <Loading />}{query.error && <ErrorState error={query.error} />}{query.data && query.data.length > 0 && <Typography color="text.secondary">{t('clinics.found', { count: query.data.length })}</Typography>}<Grid container spacing={2}>{query.data?.map((clinic) => <Grid item xs={12} md={6} key={clinic.id}><Card elevation={0} sx={{ height: '100%', border: '1px solid', borderColor: 'divider', borderRadius: 1 }}><CardActionArea component={Link} to={`/clinics/${clinic.id}`} sx={{ height: '100%' }}><CardContent sx={{ p: 3 }}><Stack direction="row" justifyContent="space-between" gap={2}><Box><Typography variant="h6" sx={{ fontWeight: 800 }}>{clinic.name}</Typography><Typography variant="body2" color="text.secondary">{clinic.type.replace(/([A-Z])/g, ' $1').trim()}</Typography></Box>{clinic.isVerified && <Chip size="small" label={t('clinics.verified')} color="success" />}</Stack><Typography color="text.secondary" sx={{ mt: 1 }}>{clinic.specialty} · {clinic.city}, {clinic.country}</Typography><Stack direction="row" spacing={1} sx={{ mt: 2 }} flexWrap="wrap" useFlexGap>{clinic.treatmentsOffered.map((treatment) => <Chip size="small" key={treatment} label={treatment} variant="outlined" />)}</Stack></CardContent></CardActionArea></Card></Grid>)}</Grid>{query.data?.length === 0 && <Typography>{t('clinics.empty')}</Typography>}
  </Stack></Container>;
}
