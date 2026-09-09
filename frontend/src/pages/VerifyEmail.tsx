import { Alert, Button, Paper, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../services/api';

export default function VerifyEmail() {
  const [params] = useSearchParams();
  const token = params.get('token');
  const email = params.get('email');
  const [state, setState] = useState<'checking' | 'pending' | 'verified' | 'error'>(token ? 'checking' : 'pending');

  useEffect(() => {
    if (!token) return;
    api('/auth/verify-email', { method: 'POST', body: JSON.stringify({ token }) })
      .then(() => setState('verified'))
      .catch(() => setState('error'));
  }, [token]);

  return <Paper sx={{ p: 4, maxWidth: 560, mx: 'auto', mt: 6 }}><Stack spacing={2}>
    {state === 'pending' && <><Typography variant="h4">Check your email</Typography><Typography color="text.secondary">We sent a verification link{email ? ` to ${email}` : ''}. Open it to activate your MedMatch account.</Typography><Alert severity="info">The link expires after 24 hours.</Alert></>}
    {state === 'checking' && <><Typography variant="h4">Verifying your email</Typography><Typography color="text.secondary">Please wait while we confirm your address.</Typography></>}
    {state === 'verified' && <><Typography variant="h4">Email verified</Typography><Alert severity="success">Your account is ready. You can sign in now.</Alert><Button component={Link} to="/login" variant="contained">Go to sign in</Button></>}
    {state === 'error' && <><Typography variant="h4">Verification link unavailable</Typography><Alert severity="error">This link is invalid, expired, or has already been used.</Alert><Button component={Link} to="/login" variant="outlined">Back to sign in</Button></>}
  </Stack></Paper>;
}
