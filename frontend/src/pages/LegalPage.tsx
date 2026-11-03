import { Box, Container, Paper, Stack, Typography, Button } from '@mui/material';
import { Link } from 'react-router-dom';

interface LegalPageProps {
  title: string;
  lastUpdated: string;
  children: React.ReactNode;
}

export default function LegalPage({ title, lastUpdated, children }: LegalPageProps) {
  return (
    <Box sx={{ bgcolor: 'background.default', minHeight: 'calc(100vh - 64px)' }}>
      <Box sx={{ bgcolor: '#102a2b', color: '#f8f6f0', py: { xs: 5, md: 8 } }}>
        <Container maxWidth="md">
          <Typography
            sx={{
              color: '#b06f42',
              fontWeight: 800,
              textTransform: 'uppercase',
              letterSpacing: '.12em',
              fontSize: '.75rem',
              mb: 1,
            }}
          >
            MedMatch
          </Typography>
          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-.03em' }}>
            {title}
          </Typography>
          <Typography sx={{ color: 'rgba(248,246,240,.6)', mt: 1, fontSize: '.9rem' }}>
            Última actualización: {lastUpdated}
          </Typography>
        </Container>
      </Box>

      <Container maxWidth="md" sx={{ py: { xs: 4, md: 6 } }}>
        <Paper
          sx={{
            p: { xs: 3, md: 5 },
            borderRadius: 3,
            '& h5': { fontWeight: 800, mt: 4, mb: 1.5, letterSpacing: '-.02em', color: '#00695c' },
            '& h6': { fontWeight: 700, mt: 3, mb: 1, color: '#102a2b' },
            '& p, & li': { color: 'text.secondary', lineHeight: 1.75, fontSize: '.95rem' },
            '& ul': { pl: 3, mb: 2 },
            '& li': { mb: 0.75 },
          }}
        >
          {children}
        </Paper>

        <Stack direction="row" justifyContent="center" sx={{ mt: 4 }}>
          <Button
            component={Link}
            to="/"
            variant="outlined"
            sx={{
              borderRadius: '9999px',
              textTransform: 'none',
              fontWeight: 600,
              px: 3,
            }}
          >
            ← Volver al inicio
          </Button>
        </Stack>
      </Container>
    </Box>
  );
}

