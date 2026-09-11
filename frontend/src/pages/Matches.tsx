import {
  Alert,
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
  Divider,
  Grid,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api, Match, MatchNotification } from '../services/api';
import { ErrorState, Loading } from '../components/PageState';
import { useTranslation } from 'react-i18next';

export default function Matches() {
  const { t } = useTranslation();
  const client = useQueryClient();
  const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);
  const [connectMessage, setConnectMessage] = useState('');
  const [notice, setNotice] = useState('');
  const [country, setCountry] = useState('');
  const [city, setCity] = useState('');
  const [filters, setFilters] = useState('');

  const matches = useQuery({
    queryKey: ['matches', filters],
    queryFn: () => api<Match[]>(`/matches?${filters}`),
  });
  const notifications = useQuery({
    queryKey: ['match-notifications'],
    queryFn: () => api<MatchNotification[]>('/matches/notifications'),
  });

  const markAllRead = useMutation({
    mutationFn: () => api('/matches/notifications/read', { method: 'POST' }),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['match-notifications'] });
      client.invalidateQueries({ queryKey: ['match-summary'] });
    },
  });

  const sendRequest = useMutation({
    mutationFn: ({ userId, message }: { userId: string; message: string }) =>
      api(`/people/${userId}/connection-requests`, {
        method: 'POST',
        body: JSON.stringify({ message }),
      }),
    onSuccess: () => {
      setSelectedMatch(null);
      setConnectMessage('');
      setNotice(t('matches.sent'));
    },
    onError: () => {
      setNotice(t('matches.notAccepting'));
    },
  });

  if (matches.isLoading || notifications.isLoading) return <Loading />;
  if (matches.error || notifications.error) return <ErrorState error={matches.error ?? notifications.error} />;

  const unreadCount = notifications.data?.filter((item) => !item.isRead).length ?? 0;
  const matchList = matches.data ?? [];

  const applyFilters = () => {
    const params = new URLSearchParams();
    if (country.trim()) params.set('country', country.trim());
    if (city.trim()) params.set('city', city.trim());
    setFilters(params.toString());
  };

  const clearFilters = () => {
    setCountry('');
    setCity('');
    setFilters('');
  };

  return (
    <Container sx={{ py: { xs: 4, md: 6 } }}>
      <Stack spacing={4}>
        <Box>
          <Typography
            sx={{
              color: '#b06f42',
              fontWeight: 800,
              textTransform: 'uppercase',
              letterSpacing: '.12em',
              fontSize: '.75rem',
            }}
          >
            {t('matches.eyebrow')}
          </Typography>
          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-.04em', mt: 1 }}>
            {t('matches.title')}
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 1.5, maxWidth: 700 }}>
            {t('matches.subtitle')}
          </Typography>
        </Box>

        <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, p: 2 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
            <TextField size="small" label={t('common.country')} value={country} onChange={(e) => setCountry(e.target.value)} sx={{ minWidth: 180 }} />
            <TextField size="small" label={t('common.city')} value={city} onChange={(e) => setCity(e.target.value)} sx={{ minWidth: 180 }} />
            <Button variant="contained" onClick={applyFilters}>
              {t('matches.filter')}
            </Button>
            <Button variant="text" onClick={clearFilters} color="inherit">{t('common.clear')}</Button>
            <Box sx={{ flexGrow: 1 }} />
            {!matches.isLoading && !matches.error && (
              <Typography color="text.secondary" sx={{ fontWeight: 600, whiteSpace: 'nowrap' }}>
                {t('matches.count', { count: matchList.length })}
              </Typography>
            )}
          </Stack>
        </Card>

        {unreadCount > 0 && (
          <Alert
            severity="info"
            action={
              <Button color="inherit" size="small" onClick={() => markAllRead.mutate()} disabled={markAllRead.isPending}>
                {t('matches.markRead')}
              </Button>
            }
          >
            {t('matches.unread', { count: unreadCount })}
          </Alert>
        )}

        {matchList.length === 0 ? (
          <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, p: 4, textAlign: 'center' }}>
            <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
              {t('matches.emptyTitle')}
            </Typography>
            <Typography color="text.secondary" sx={{ maxWidth: 500, mx: 'auto', mb: 3 }}>
              {t('matches.emptyBody')}
            </Typography>
            <Button variant="contained" component={Link} to="/profile">
              {t('matches.updateProfile')}
            </Button>
          </Card>
        ) : (
          <Grid container spacing={3}>
            {matchList.map((match) => (
              <Grid item xs={12} md={6} key={match.userId}>
                <Card
                  elevation={0}
                  sx={{
                    height: '100%',
                    border: '1px solid',
                    borderColor: 'divider',
                    borderRadius: 2,
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'box-shadow .2s ease',
                    '&:hover': {
                      boxShadow: '0 4px 20px rgba(0,0,0,.08)',
                    },
                  }}
                >
                  <CardContent sx={{ p: 3, flexGrow: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                      <Box>
                        <Typography variant="h6" sx={{ fontWeight: 800, lineHeight: 1.2 }}>
                          {match.displayName}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                          {[match.city, match.country].filter(Boolean).join(', ') || t('matches.noLocation')}
                        </Typography>
                      </Box>
                      <Chip
                        label={`${match.matchPercentage}${t('matches.percentMatch')}`}
                        sx={{
                          bgcolor: match.matchPercentage >= 75 ? '#00695c' : '#b06f42',
                          color: '#fff',
                          fontWeight: 700,
                          fontSize: '.75rem',
                          height: 24,
                        }}
                      />
                    </Box>

                    {match.sameLocation && (
                      <Box>
                        <Chip
                          label="{t('matches.localMatch')}"
                          size="small"
                          sx={(theme) => ({
                            bgcolor: theme.palette.mode === 'dark' ? 'rgba(77,182,172,.18)' : 'rgba(0,105,92,.08)',
                            color: theme.palette.mode === 'dark' ? '#4db6ac' : '#00695c',
                            fontWeight: 600,
                            fontSize: '.7rem',
                          })}
                        />
                      </Box>
                    )}

                    <Box>
                      <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase', letterSpacing: '.05em' }}>
                        {t('matches.sharedDiagnoses')}
                      </Typography>
                      <Stack direction="row" spacing={1} mt={0.5} flexWrap="wrap" useFlexGap>
                        {match.sharedDiagnoses.map((diagnosis) => (
                          <Chip
                            key={diagnosis}
                            label={diagnosis}
                            size="small"
                            sx={(theme) => ({
                              bgcolor: theme.palette.mode === 'dark' ? 'rgba(77,182,172,.18)' : 'rgba(0,105,92,.12)',
                              color: theme.palette.mode === 'dark' ? '#9ce8dd' : '#004d40',
                              fontWeight: 600,
                            })}
                          />
                        ))}
                      </Stack>
                    </Box>

                    {match.sharedSymptoms && match.sharedSymptoms.length > 0 && (
                      <Box>
                        <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase', letterSpacing: '.05em' }}>
                          {t('matches.sharedSymptoms')}
                        </Typography>
                        <Stack direction="row" spacing={0.8} mt={0.5} flexWrap="wrap" useFlexGap>
                          {match.sharedSymptoms.map((symptom) => (
                            <Chip key={symptom} label={symptom} size="small" variant="outlined" sx={{ fontSize: '.75rem' }} />
                          ))}
                        </Stack>
                      </Box>
                    )}

                    {match.bio && (
                      <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic', mt: 0.5 }}>
                        "{match.bio}"
                      </Typography>
                    )}

                    <Box sx={{ flexGrow: 1 }} />
                    <Divider sx={{ my: 1 }} />

                    <Button
                      variant="contained"
                      onClick={() => setSelectedMatch(match)}
                      sx={{
                        borderRadius: '9999px',
                        textTransform: 'none',
                        fontWeight: 600,
                      }}
                    >
                      {t('matches.request')}
                    </Button>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}

        {/* Connection Request Modal */}
        <Dialog open={!!selectedMatch} onClose={() => setSelectedMatch(null)} maxWidth="sm" fullWidth>
          <DialogTitle sx={{ fontWeight: 800 }}>{t('matches.connectTitle', { name: selectedMatch?.displayName })}</DialogTitle>
          <DialogContent>
            <Typography color="text.secondary" sx={{ mb: 2 }}>
              {t('matches.connectBody', { diagnoses: selectedMatch?.sharedDiagnoses.join(', '), name: selectedMatch?.displayName })}
            </Typography>
            <TextField
              fullWidth
              multiline
              rows={3}
              label={t('matches.message')}
              placeholder={t('matches.messagePlaceholder')}
              value={connectMessage}
              onChange={(e) => setConnectMessage(e.target.value)}
            />
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setSelectedMatch(null)}>{t('common.cancel')}</Button>
            <Button
              variant="contained"
              onClick={() =>
                selectedMatch &&
                sendRequest.mutate({
                  userId: selectedMatch.userId,
                  message: connectMessage,
                })
              }
              disabled={sendRequest.isPending}
            >
              {sendRequest.isPending ? t('matches.sending') : t('matches.send')}
            </Button>
          </DialogActions>
        </Dialog>

        <Snackbar open={!!notice} autoHideDuration={4000} onClose={() => setNotice('')} message={notice} />
      </Stack>
    </Container>
  );
}
