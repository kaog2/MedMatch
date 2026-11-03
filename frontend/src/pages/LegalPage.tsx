import { Box, Button, Container, Paper, Stack, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

export type LegalJurisdiction = 'de' | 'cl';

interface LegalPageProps {
  title: string;
  lastUpdated: string;
  jurisdiction?: LegalJurisdiction;
  onJurisdictionChange?: (j: LegalJurisdiction) => void;
  children: React.ReactNode;
}

export default function LegalPage({
  title,
  lastUpdated,
  jurisdiction = 'cl',
  onJurisdictionChange,
  children,
}: LegalPageProps) {
  const isGerman = jurisdiction === 'de';

  return (
    <Box sx={{ bgcolor: 'background.default', minHeight: 'calc(100vh - 64px)' }}>
      <Box sx={{ bgcolor: '#102a2b', color: '#f8f6f0', py: { xs: 5, md: 7 } }}>
        <Container maxWidth="md">
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            justifyContent="space-between"
            alignItems={{ xs: 'flex-start', sm: 'center' }}
            spacing={2}
            sx={{ mb: 2 }}
          >
            <Typography
              sx={{
                color: '#b06f42',
                fontWeight: 800,
                textTransform: 'uppercase',
                letterSpacing: '.12em',
                fontSize: '.75rem',
              }}
            >
              MedMatch • {isGerman ? 'Rechtliche Dokumentation' : 'Documentación Legal'}
            </Typography>

            {onJurisdictionChange && (
              <ToggleButtonGroup
                value={jurisdiction}
                exclusive
                onChange={(_, val) => {
                  if (val) onJurisdictionChange(val);
                }}
                size="small"
                sx={{
                  bgcolor: 'rgba(255,255,255,0.08)',
                  borderRadius: '9999px',
                  p: 0.5,
                  '& .MuiToggleButton-root': {
                    color: 'rgba(248,246,240,0.7)',
                    border: 'none',
                    borderRadius: '9999px',
                    px: 2,
                    py: 0.5,
                    fontSize: '0.78rem',
                    fontWeight: 600,
                    textTransform: 'none',
                    '&.Mui-selected': {
                      bgcolor: '#00695c',
                      color: '#ffffff',
                      '&:hover': {
                        bgcolor: '#005b4f',
                      },
                    },
                    '&:hover': {
                      bgcolor: 'rgba(255,255,255,0.12)',
                    },
                  },
                }}
              >
                <ToggleButton value="de">🇩🇪 Deutschland (DSGVO / BGB)</ToggleButton>
                <ToggleButton value="cl">🇨🇱 Chile (Ley 19.628)</ToggleButton>
              </ToggleButtonGroup>
            )}
          </Stack>

          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-.03em' }}>
            {title}
          </Typography>
          <Typography sx={{ color: 'rgba(248,246,240,.6)', mt: 1, fontSize: '.9rem' }}>
            {isGerman ? 'Zuletzt aktualisiert' : 'Última actualización'}: {lastUpdated}
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
            {isGerman ? '← Zurück zur Startseite' : '← Volver al inicio'}
          </Button>
        </Stack>
      </Container>
    </Box>
  );
}
