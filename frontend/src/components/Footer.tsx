import { Box, Container, Divider, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

export default function Footer() {
  const { t, i18n } = useTranslation();
  const isDe = Boolean(i18n.language && i18n.language.startsWith('de'));

  const legalLinks = [
    {
      label: isDe ? 'Datenschutzerklärung' : t('footer.privacy', { defaultValue: 'Política de Privacidad' }),
      to: isDe ? '/datenschutz' : '/privacy',
    },
    {
      label: isDe ? 'Nutzungsbedingungen (AGB)' : t('footer.terms', { defaultValue: 'Términos y Condiciones' }),
      to: isDe ? '/agb' : '/terms',
    },
    {
      label: isDe ? 'Impressum' : t('footer.legal', { defaultValue: 'Aviso Legal' }),
      to: isDe ? '/impressum' : '/legal',
    },
  ];

  return (
    <Box
      component="footer"
      sx={{
        bgcolor: '#102a2b',
        color: '#f8f6f0',
        mt: 'auto',
        pt: { xs: 4, md: 5 },
        pb: { xs: 3, md: 4 },
      }}
    >
      <Container maxWidth="lg">
        {/* ── Brand + legal links ── */}
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'center', sm: 'flex-start' }}
          spacing={2}
        >
          <Typography
            sx={{
              fontWeight: 800,
              fontSize: '1.15rem',
              letterSpacing: '-.02em',
              color: '#b06f42',
            }}
          >
            MedMatch
          </Typography>

          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={{ xs: 1, sm: 3 }}
            alignItems="center"
          >
            {legalLinks.map((link) => (
              <Typography
                key={link.to}
                component={Link}
                to={link.to}
                sx={{
                  color: 'rgba(248,246,240,.7)',
                  textDecoration: 'none',
                  fontSize: '.85rem',
                  transition: 'color .2s',
                  '&:hover': { color: '#b06f42' },
                }}
              >
                {link.label}
              </Typography>
            ))}
          </Stack>
        </Stack>

        <Divider sx={{ borderColor: 'rgba(248,246,240,.12)', my: 3 }} />

        {/* ── Medical disclaimer ── */}
        <Typography
          sx={{
            color: 'rgba(248,246,240,.45)',
            fontSize: '.78rem',
            lineHeight: 1.6,
            textAlign: 'center',
            maxWidth: 750,
            mx: 'auto',
            mb: 2,
          }}
        >
          {isDe
            ? 'MedMatch erbringt keine medizinischen Leistungen, Diagnosen oder Behandlungen. Der Austausch zwischen Nutzern und die Empfehlungen von Fachzentren ersetzen keinesfalls die professionelle ärztliche Beratung oder Behandlung. In Notfällen wenden Sie sich bitte sofort an den Notruf (112).'
            : 'MedMatch no provee servicios médicos, diagnósticos ni tratamientos. Las conexiones entre usuarios y las recomendaciones de centros no sustituyen la opinión de un profesional de la salud.'}
        </Typography>

        {/* ── Copyright ── */}
        <Typography
          sx={{
            color: 'rgba(248,246,240,.35)',
            fontSize: '.75rem',
            textAlign: 'center',
          }}
        >
          © {new Date().getFullYear()} MedMatch. {isDe ? 'Alle Rechte vorbehalten.' : 'Todos los derechos reservados.'}
        </Typography>
      </Container>
    </Box>
  );
}
