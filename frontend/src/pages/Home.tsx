import { Box, Button, Container, Grid, Paper, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

const highlights = [
  { number: '01', titleKey: 'home.h1', textKey: 'home.h1Text' },
  { number: '02', titleKey: 'home.h2', textKey: 'home.h2Text' },
  { number: '03', titleKey: 'home.h3', textKey: 'home.h3Text' },
];

export default function Home() {
  const { t } = useTranslation();
  return <Box sx={{ bgcolor: 'background.default', color: 'text.primary', minHeight: 'calc(100vh - 64px)' }}>
    <Box sx={{ bgcolor: '#102a2b', color: '#f8f6f0', overflow: 'hidden', position: 'relative' }}>
      <Box sx={{ position: 'absolute', width: 420, height: 420, borderRadius: '50%', bgcolor: '#d2e7d9', opacity: .14, right: '-100px', top: '-160px' }} />
      <Container maxWidth="lg" sx={{ py: { xs: 8, md: 13 }, position: 'relative' }}>
        <Grid container spacing={{ xs: 6, md: 10 }} alignItems="center">
          <Grid item xs={12} md={7}>
            <Typography sx={{ color: '#f2b880', fontWeight: 800, letterSpacing: '.14em', textTransform: 'uppercase', fontSize: '.78rem', mb: 3 }}>{t('home.eyebrow')}</Typography>
            <Typography component="h1" sx={{ fontSize: { xs: '3.4rem', md: '6.4rem' }, lineHeight: .94, letterSpacing: '-.065em', fontWeight: 800, maxWidth: 760 }}>{t('home.title')}</Typography>
            <Typography sx={{ color: 'rgba(248,246,240,.72)', fontSize: { xs: '1.08rem', md: '1.28rem' }, lineHeight: 1.6, maxWidth: 570, mt: 4 }}>{t('home.subtitle')}</Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mt: 5 }}>
              <Button component={Link} to="/clinics" variant="contained" size="large" sx={{ bgcolor: '#f2b880', color: '#102a2b', px: 3, py: 1.5, '&:hover': { bgcolor: '#f7c99d' } }}>{t('home.explore')}</Button>
              <Button component={Link} to="/register" variant="outlined" size="large" sx={{ color: '#f8f6f0', borderColor: 'rgba(248,246,240,.45)', px: 3, py: 1.5, '&:hover': { borderColor: '#f8f6f0' } }}>{t('home.share')}</Button>
            </Stack>
          </Grid>
          <Grid item xs={12} md={5}>
            <Paper elevation={0} sx={{ bgcolor: '#d2e7d9', color: '#102a2b', p: { xs: 3, md: 4 }, transform: { md: 'rotate(2deg)' }, borderRadius: 1 }}>
              <Typography sx={{ fontSize: '.75rem', fontWeight: 800, letterSpacing: '.12em', textTransform: 'uppercase', opacity: .65 }}>{t('home.cardEyebrow')}</Typography>
              <Typography sx={{ fontSize: { xs: '2rem', md: '2.7rem' }, lineHeight: 1.05, fontWeight: 800, letterSpacing: '-.04em', mt: 7 }}>{t('home.cardTitle')}</Typography>
              <Box sx={{ borderTop: '1px solid rgba(16,42,43,.22)', mt: 8, pt: 2 }}><Typography sx={{ fontSize: '.9rem' }}>{t('home.cardFooter')}</Typography></Box>
            </Paper>
          </Grid>
        </Grid>
      </Container>
    </Box>
    <Container maxWidth="lg" sx={{ py: { xs: 8, md: 12 } }}>
      <Grid container spacing={3}>
        {highlights.map((highlight) => <Grid item xs={12} md={4} key={highlight.number}><Box sx={{ borderTop: '2px solid', borderColor: 'divider', pt: 2.5, height: '100%' }}><Typography sx={{ color: '#b06f42', fontWeight: 800 }}>{highlight.number}</Typography><Typography variant="h5" sx={{ fontWeight: 800, mt: 5, letterSpacing: '-.03em' }}>{t(highlight.titleKey)}</Typography><Typography sx={{ color: 'text.secondary', lineHeight: 1.65, mt: 2 }}>{t(highlight.textKey)}</Typography></Box></Grid>)}
      </Grid>
      <Box sx={{ mt: { xs: 9, md: 13 }, display: 'flex', justifyContent: 'space-between', alignItems: { xs: 'flex-start', md: 'center' }, gap: 3, flexDirection: { xs: 'column', md: 'row' } }}>
        <Box><Typography sx={{ color: '#b06f42', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '.12em', fontSize: '.75rem' }}>{t('home.ctaEyebrow')}</Typography><Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-.05em', mt: 1 }}>{t('home.ctaTitle')}</Typography></Box>
        <Button component={Link} to="/clinics" variant="contained" size="large" sx={{ px: 3, py: 1.5 }}>{t('home.ctaButton')}</Button>
      </Box>
    </Container>
  </Box>;
}