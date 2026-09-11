import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Container,
  Drawer,
  Divider,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, AdminUser, AdminUsersResponse, Match, Recommendation } from '../services/api';
import { ErrorState, Loading } from '../components/PageState';

export default function AdminPortal() {
  const client = useQueryClient();
  const [search, setSearch] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [page, setPage] = useState(1);
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null);
  const [notice, setNotice] = useState('');
  const [errorNotice, setErrorNotice] = useState('');

  const users = useQuery({
    queryKey: ['admin-users', appliedSearch, page],
    queryFn: () => api<AdminUsersResponse>(`/admin/users?page=${page}&pageSize=25&search=${encodeURIComponent(appliedSearch)}`),
  });

  const matches = useQuery({
    queryKey: ['admin-user-matches', selectedUser?.id],
    queryFn: () => api<Match[]>(`/admin/users/${selectedUser!.id}/matches`),
    enabled: !!selectedUser,
  });

  const toggleActive = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      api(`/admin/users/${id}/active`, { method: 'POST', body: JSON.stringify({ isActive }) }),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['admin-users'] });
      setNotice('User status updated.');
    },
  });

  const recommendations = useQuery({
    queryKey: ['admin-recommendations'],
    queryFn: () => api<Recommendation[]>('/admin/recommendations'),
  });

  const moderate = useMutation({
    mutationFn: ({ id, approve }: { id: string; approve: boolean }) =>
      api(`/admin/recommendations/${id}/moderate`, { method: 'POST', body: JSON.stringify({ approve }) }),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['admin-recommendations'] });
      setNotice('Recommendation updated.');
    },
  });

  const changeRole = useMutation({
    mutationFn: ({ id, role, enabled }: { id: string; role: string; enabled: boolean }) =>
      api(`/admin/users/${id}/role`, { method: 'POST', body: JSON.stringify({ role, enabled }) }),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ['admin-users'] });
      setNotice('User roles updated.');
    },
    onError: (e) => setErrorNotice(e instanceof Error ? e.message : 'Unable to update roles.'),
  });

  const applySearch = () => {
    setAppliedSearch(search);
    setPage(1);
  };

  const totalPages = users.data ? Math.max(1, Math.ceil(users.data.total / users.data.pageSize)) : 1;

  return (
    <Container sx={{ py: { xs: 4, md: 6 } }}>
      <Stack spacing={3}>
        <Box>
          <Typography sx={{ color: '#b06f42', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '.12em', fontSize: '.75rem' }}>
            Administration
          </Typography>
          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-.04em', mt: 1 }}>
            User Management
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            Review users, inspect their diagnosis matches, and activate or deactivate accounts.
          </Typography>
        </Box>

        {notice && <Alert severity="success" onClose={() => setNotice('')}>{notice}</Alert>}
        {errorNotice && <Alert severity="error" onClose={() => setErrorNotice('')}>{errorNotice}</Alert>}

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <TextField
            size="small"
            label="Search email or name"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && applySearch()}
            sx={{ minWidth: 260 }}
          />
          <Button variant="contained" onClick={applySearch} sx={{ bgcolor: '#102a2b', '&:hover': { bgcolor: '#1d4647' } }}>
            Search
          </Button>
          {users.data && (
            <Typography color="text.secondary" sx={{ alignSelf: 'center' }}>
              {users.data.total} users
            </Typography>
          )}
        </Stack>

        {users.isLoading && <Loading />}
        {users.error && <ErrorState error={users.error} />}

        {users.data && (
          <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 800 }}>User</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>Role</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>Location</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>Diagnoses</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>Discoverable</TableCell>
                  <TableCell sx={{ fontWeight: 800 }}>Active</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {users.data.items.map((user) => (
                  <TableRow key={user.id} hover>
                    <TableCell>
                      <Typography sx={{ fontWeight: 600 }}>{user.displayName || user.email}</Typography>
                      <Typography variant="caption" color="text.secondary">{user.email}</Typography>
                    </TableCell>
                    <TableCell>
                      <TextField
                        select
                        size="small"
                        SelectProps={{
                          multiple: true,
                          renderValue: (selected) => (selected as string[]).join(', '),
                        }}
                        value={user.roles}
                        onChange={(e) => {
                          const next = e.target.value as unknown as string[];
                          const prev = user.roles;
                          const added = next.find((r) => !prev.includes(r));
                          const removed = prev.find((r) => !next.includes(r));
                          const role = added ?? removed;
                          if (role) changeRole.mutate({ id: user.id, role, enabled: !!added });
                        }}
                        sx={{ minWidth: 140 }}
                      >
                        <MenuItem value="Patient">Patient</MenuItem>
                        <MenuItem value="Clinic">Clinic</MenuItem>
                        <MenuItem value="Doctor">Doctor</MenuItem>
                        <MenuItem value="Admin">Admin</MenuItem>
                      </TextField>
                    </TableCell>
                    <TableCell>{[user.city, user.country].filter(Boolean).join(', ') || '—'}</TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                        {user.diagnoses.slice(0, 3).map((d) => <Chip key={d} label={d} size="small" />)}
                        {user.diagnoses.length > 3 && <Chip label={`+${user.diagnoses.length - 3}`} size="small" variant="outlined" />}
                      </Stack>
                    </TableCell>
                    <TableCell>{user.patientsContactMe && user.dataForSearch ? 'Yes' : 'No'}</TableCell>
                    <TableCell>
                      <Switch
                        size="small"
                        checked={user.isActive}
                        disabled={user.roles.includes('Admin')}
                        onChange={(e) => toggleActive.mutate({ id: user.id, isActive: e.target.checked })}
                      />
                    </TableCell>
                    <TableCell>
                      <Button size="small" onClick={() => setSelectedUser(user)}>Matches</Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        <Stack direction="row" spacing={2} justifyContent="center">
          <Button disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
          <Typography alignSelf="center">Page {page} of {totalPages}</Typography>
          <Button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next</Button>
        </Stack>

        <Divider />

        <Box>
          <Typography variant="h5" sx={{ fontWeight: 800 }}>Provider recommendations</Typography>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            Approve or reject positive provider recommendations submitted by patients.
          </Typography>
        </Box>

        {recommendations.isLoading && <Loading />}
        {recommendations.error && <ErrorState error={recommendations.error} />}

        {recommendations.data && recommendations.data.length === 0 && (
          <Typography color="text.secondary">No recommendations yet.</Typography>
        )}

        {recommendations.data?.map((rec) => (
          <Card key={rec.id} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
            <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap>
                <Typography sx={{ fontWeight: 700 }}>{rec.authorDisplayName}</Typography>
                <Chip
                  label={rec.status}
                  size="small"
                  sx={{
                    fontWeight: 700,
                    bgcolor: rec.status === 'Approved' ? '#00695c' : rec.status === 'Rejected' ? '#b3261e' : '#b06f42',
                    color: '#fff',
                  }}
                />
              </Stack>
              <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 1 }}>
                {rec.clinics.map((c) => <Chip key={c.id} label={c.name} size="small" variant="outlined" />)}
              </Stack>
              <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 1 }}>
                {rec.diagnoses.map((d) => <Chip key={d} label={d} size="small" color="primary" variant="outlined" />)}
              </Stack>
              <Typography sx={{ mt: 1 }}>{rec.details}</Typography>
              {rec.moderationNote && (
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                  Note: {rec.moderationNote}
                </Typography>
              )}
              {rec.status === 'Pending' && (
                <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                  <Button size="small" variant="contained" color="success" onClick={() => moderate.mutate({ id: rec.id, approve: true })}>
                    Approve
                  </Button>
                  <Button size="small" variant="outlined" color="error" onClick={() => moderate.mutate({ id: rec.id, approve: false })}>
                    Reject
                  </Button>
                </Stack>
              )}
            </CardContent>
          </Card>
        ))}
      </Stack>

      <Drawer anchor="right" open={!!selectedUser} onClose={() => setSelectedUser(null)}>
        <Box sx={{ width: { xs: '100vw', sm: 480 }, p: 3 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6" sx={{ fontWeight: 800 }}>Matches for {selectedUser?.displayName || selectedUser?.email}</Typography>
            <IconButton onClick={() => setSelectedUser(null)}>✕</IconButton>
          </Stack>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2 }}>
            {selectedUser?.email} · {[selectedUser?.city, selectedUser?.country].filter(Boolean).join(', ') || 'No location'}
          </Typography>
          {selectedUser && selectedUser.diagnoses.length > 0 && (
            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mb: 2 }}>
              {selectedUser.diagnoses.map((d) => <Chip key={d} label={d} size="small" />)}
            </Stack>
          )}
          <Divider sx={{ mb: 2 }} />
          {matches.isLoading && <Loading />}
          {matches.error && <ErrorState error={matches.error} />}
          {matches.data?.length === 0 && <Typography color="text.secondary">No matches for this user.</Typography>}
          <Stack spacing={1.5}>
            {matches.data?.map((match) => (
              <Card key={match.userId} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <Stack direction="row" justifyContent="space-between">
                    <Typography sx={{ fontWeight: 700 }}>{match.displayName}</Typography>
                    <Chip label={`${match.matchPercentage}%`} size="small" sx={{ bgcolor: '#00695c', color: '#fff', fontWeight: 700 }} />
                  </Stack>
                  <Typography variant="caption" color="text.secondary">{[match.city, match.country].filter(Boolean).join(', ') || 'Location not shared'}</Typography>
                  <Stack direction="row" spacing={0.5} mt={1} flexWrap="wrap" useFlexGap>
                    {match.sharedDiagnoses.map((d) => <Chip key={d} label={d} size="small" variant="outlined" />)}
                  </Stack>
                </CardContent>
              </Card>
            ))}
          </Stack>
        </Box>
      </Drawer>
    </Container>
  );
}
