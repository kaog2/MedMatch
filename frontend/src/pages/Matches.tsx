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

export default function Matches() {
  const client = useQueryClient();
  const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);
  const [connectMessage, setConnectMessage] = useState('');
  const [notice, setNotice] = useState('');

  const matches = useQuery({ queryKey: ['matches'], queryFn: () => api<Match[]>('/matches') });
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
      setNotice('Connection request sent.');
    },
    onError: () => {
      setNotice('This member is no longer accepting connection requests.');
    },
  });

  if (matches.isLoading || notifications.isLoading) return <Loading />;
  if (matches.error || notifications.error) return <ErrorState error={matches.error ?? notifications.error} />;

  const unreadCount = notifications.data?.filter((item) => !item.isRead).length ?? 0;
  const matchList = matches.data ?? [];

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
            Peer Matching
          </Typography>
          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-.04em', mt: 1 }}>
            Your Diagnosis Matches
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 1.5, maxWidth: 700 }}>
            Connect with peers who share your conditions, lived experiences, and symptoms. Matches are computed based
            on shared diagnosis tags, symptom overlap, and location proximity.
          </Typography>
        </Box>

        {unreadCount > 0 && (
          <Alert
            severity="info"
            action={
              <Button color="inherit" size="small" onClick={() => markAllRead.mutate()} disabled={markAllRead.isPending}>
                Mark all read
              </Button>
            }
          >
            You have {unreadCount} new diagnosis match notification{unreadCount === 1 ? '' : 's'}.
          </Alert>
        )}

        {matchList.length === 0 ? (
          <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, p: 4, textAlign: 'center' }}>
            <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
              No matches found yet
            </Typography>
            <Typography color="text.secondary" sx={{ maxWidth: 500, mx: 'auto', mb: 3 }}>
              Make sure you have saved diagnosis tags on your profile and verified that both peer contact and profile search are enabled in your consent settings.
            </Typography>
            <Button variant="contained" component={Link} to="/profile" sx={{ bgcolor: '#102a2b', '&:hover': { bgcolor: '#1d4647' } }}>
              Update Profile & Tags
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
                          {[match.city, match.country].filter(Boolean).join(', ') || 'Location not shared'}
                        </Typography>
                      </Box>
                      <Chip
                        label={`${match.matchPercentage}% Match`}
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
                          label="Local match (same city)"
                          size="small"
                          sx={{
                            bgcolor: 'rgba(0,105,92,.08)',
                            color: '#00695c',
                            fontWeight: 600,
                            fontSize: '.7rem',
                          }}
                        />
                      </Box>
                    )}

                    <Box>
                      <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase', letterSpacing: '.05em' }}>
                        Shared Diagnoses
                      </Typography>
                      <Stack direction="row" spacing={1} mt={0.5} flexWrap="wrap" useFlexGap>
                        {match.sharedDiagnoses.map((diagnosis) => (
                          <Chip
                            key={diagnosis}
                            label={diagnosis}
                            size="small"
                            sx={{
                              bgcolor: 'rgba(0,105,92,.12)',
                              color: '#004d40',
                              fontWeight: 600,
                            }}
                          />
                        ))}
                      </Stack>
                    </Box>

                    {match.sharedSymptoms && match.sharedSymptoms.length > 0 && (
                      <Box>
                        <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.secondary', textTransform: 'uppercase', letterSpacing: '.05em' }}>
                          Shared Symptoms
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
                        bgcolor: '#102a2b',
                        borderRadius: '9999px',
                        textTransform: 'none',
                        fontWeight: 600,
                        '&:hover': { bgcolor: '#1d4647' },
                      }}
                    >
                      Request connection
                    </Button>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}

        {/* Connection Request Modal */}
        <Dialog open={!!selectedMatch} onClose={() => setSelectedMatch(null)} maxWidth="sm" fullWidth>
          <DialogTitle sx={{ fontWeight: 800 }}>Connect with {selectedMatch?.displayName}</DialogTitle>
          <DialogContent>
            <Typography color="text.secondary" sx={{ mb: 2 }}>
              You share <strong>{selectedMatch?.sharedDiagnoses.join(', ')}</strong> with {selectedMatch?.displayName}.
              Send an introduction note to exchange treatments, recommendations, and lived experiences.
            </Typography>
            <TextField
              fullWidth
              multiline
              rows={3}
              label="Personal message (optional)"
              placeholder="Hi, I noticed we share similar diagnosis experiences and would love to exchange insights..."
              value={connectMessage}
              onChange={(e) => setConnectMessage(e.target.value)}
            />
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setSelectedMatch(null)}>Cancel</Button>
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
              sx={{ bgcolor: '#102a2b', '&:hover': { bgcolor: '#1d4647' } }}
            >
              {sendRequest.isPending ? 'Sending...' : 'Send request'}
            </Button>
          </DialogActions>
        </Dialog>

        <Snackbar open={!!notice} autoHideDuration={4000} onClose={() => setNotice('')} message={notice} />
      </Stack>
    </Container>
  );
}
