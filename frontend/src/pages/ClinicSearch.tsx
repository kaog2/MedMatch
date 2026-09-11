import { Box, Button, Card, CardActionArea, CardContent, Chip, Container, Grid, Paper, Stack, TextField, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api, Clinic } from '../services/api';
import { ErrorState, Loading } from '../components/PageState';

export default function ClinicSearch() {
  const [specialty, setSpecialty] = useState('');
  const [city, setCity] = useState('');
  const [tag, setTag] = useState('');
  const [filters, setFilters] = useState('');
  const query = useQuery({ queryKey: ['clinics', filters], queryFn: () => api<Clinic[]>(`/clinics?${filters}`) });
  const search = () => setFilters(new URLSearchParams(Object.entries({ specialty, city, tag }).filter(([, value]) => value.trim())).toString());
  const clear = () => { setSpecialty(''); setCity(''); setTag(''); setFilters(''); };

  return <Container><Stack spacing={4} sx={{ py: { xs: 4, md: 7 } }}>
    <Box><Typography sx={{ color: '#b06f42', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '.12em', fontSize: '.75rem' }}>The care directory</Typography><Typography variant="h2" sx={{ fontWeight: 800, letterSpacing: '-.055em', mt: 1 }}>Find care that fits your journey.</Typography><Typography color="text.secondary" sx={{ mt: 2, maxWidth: 650 }}>Explore care providers and therapies through the experiences people choose to share.</Typography></Box>
    <Paper elevation={0} sx={{ p: { xs: 2, md: 3 }, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}><Stack spacing={2}><Typography variant="h6" sx={{ fontWeight: 800 }}>What kind of care are you looking for?</Typography><Grid container spacing={2}><Grid item xs={12} md={4}><TextField fullWidth label="Specialty or therapy" placeholder="e.g. orthopedics" value={specialty} onChange={(e) => setSpecialty(e.target.value)} /></Grid><Grid item xs={12} md={4}><TextField fullWidth label="City" placeholder="e.g. Berlin" value={city} onChange={(e) => setCity(e.target.value)} /></Grid><Grid item xs={12} md={4}><TextField fullWidth label="Treatment or experience" placeholder="e.g. pain therapy" value={tag} onChange={(e) => setTag(e.target.value)} /></Grid></Grid><Stack direction="row" spacing={1} justifyContent="flex-end"><Button onClick={clear} color="inherit">Clear</Button><Button variant="contained" onClick={search}>Search care</Button></Stack></Stack></Paper>
    {query.isLoading && <Loading />}{query.error && <ErrorState error={query.error} />}{query.data && query.data.length > 0 && <Typography color="text.secondary">{query.data.length} {query.data.length === 1 ? 'care provider' : 'care providers'} found</Typography>}<Grid container spacing={2}>{query.data?.map((clinic) => <Grid item xs={12} md={6} key={clinic.id}><Card elevation={0} sx={{ height: '100%', border: '1px solid', borderColor: 'divider', borderRadius: 1 }}><CardActionArea component={Link} to={`/clinics/${clinic.id}`} sx={{ height: '100%' }}><CardContent sx={{ p: 3 }}><Stack direction="row" justifyContent="space-between" gap={2}><Box><Typography variant="h6" sx={{ fontWeight: 800 }}>{clinic.name}</Typography><Typography variant="body2" color="text.secondary">{clinic.type.replace(/([A-Z])/g, ' $1').trim()}</Typography></Box>{clinic.isVerified && <Chip size="small" label="Verified" color="success" />}</Stack><Typography color="text.secondary" sx={{ mt: 1 }}>{clinic.specialty} · {clinic.city}, {clinic.country}</Typography><Stack direction="row" spacing={1} sx={{ mt: 2 }} flexWrap="wrap" useFlexGap>{clinic.treatmentsOffered.map((treatment) => <Chip size="small" key={treatment} label={treatment} variant="outlined" />)}</Stack></CardContent></CardActionArea></Card></Grid>)}</Grid>{query.data?.length === 0 && <Typography>No care providers match those filters yet. Try a broader search.</Typography>}
  </Stack></Container>;
}
